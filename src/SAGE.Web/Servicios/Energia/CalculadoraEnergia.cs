using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Dispositivo;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Energia;

public sealed class CalculadoraEnergia(
    IServiceScopeFactory scopeFactory, IUsuarioActual usuario, IFabricaDispositivo fabrica)
{
    public async Task<EnergiaPorFuente> CalcularAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc)
    {
        await usuario.CargarAsync();
        var empresaId = usuario.EmpresaActivaId
            ?? throw new UnauthorizedAccessException("No hay una empresa activa.");
        return await CalcularEmpresaAsync(empresaId, dispositivoId, desdeUtc, hastaUtc);
    }

    // Inicio consolida por separado las empresas vinculadas al usuario.
    internal async Task<EnergiaPorFuente> CalcularEmpresaAsync(
        int empresaId, int dispositivoId, DateTime desdeUtc, DateTime hastaUtc)
    {
        await usuario.CargarAsync();
        if (usuario.EsOperador || usuario.DebeCambiarContrasena || !usuario.PuedeVerEmpresa(empresaId))
            throw new UnauthorizedAccessException("El dispositivo no pertenece a una empresa vinculada.");

        if (hastaUtc < desdeUtc)
            throw new ArgumentException("El fin debe ser posterior al inicio.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (fabrica is FabricaDispositivoSimulado)
        {
            if (!await db.Dispositivos.AnyAsync(d => d.Id == dispositivoId && d.EmpresaId == empresaId))
                throw new UnauthorizedAccessException("El dispositivo no pertenece a una empresa vinculada.");

            var mediciones = LectorTelemetriaSimulado.Generar(dispositivoId, desdeUtc, hastaUtc, int.MaxValue);
            var previas = desdeUtc == DateTime.MinValue ? [] :
                LectorTelemetriaSimulado.Generar(dispositivoId, DateTime.MinValue, desdeUtc.AddTicks(-1), 1);
            return CalcularMuestras(mediciones, previas, 0);
        }

        // LEAD usa el orden cronológico, incluso cuando cambia el BootId o el reloj se repite.
        var resultados = await db.Database.SqlQuery<Totales>($"""
            WITH Dispositivo AS (
                SELECT Id FROM Dispositivos WHERE Id = {dispositivoId} AND EmpresaId = {empresaId}
            ), Intervalo AS (
                SELECT m.Id, m.Ts, m.BootId, m.FuenteSeleccionada, m.WhSolAcum, m.WhRedAcum
                FROM Mediciones m INNER JOIN Dispositivo d ON d.Id = m.DispositivoId
                WHERE m.Ts >= {desdeUtc} AND m.Ts <= {hastaUtc}
            ), Pares AS (
                SELECT FuenteSeleccionada, Ts, LEAD(Ts) OVER (ORDER BY Ts, Id) AS Siguiente FROM Intervalo
            ), Duraciones AS (
                SELECT FuenteSeleccionada,
                    CASE WHEN Siguiente IS NULL THEN 0
                         WHEN DATEDIFF_BIG(second, Ts, Siguiente) > 5 THEN 50000000
                         WHEN DATEDIFF_BIG(nanosecond, Ts, Siguiente) > 5000000000 THEN 50000000
                         ELSE DATEDIFF_BIG(nanosecond, Ts, Siguiente) / 100
                    END AS Ticks
                FROM Pares
            ), Ultimas AS (
                SELECT BootId, WhSolAcum, WhRedAcum,
                    ROW_NUMBER() OVER (PARTITION BY BootId ORDER BY Ts DESC, Id DESC) AS Posicion
                FROM Intervalo
            ), Energias AS (
                SELECT SUM(u.WhSolAcum - COALESCE(r.WhSolAcum, 0)) AS WhSolar,
                    SUM(u.WhRedAcum - COALESCE(r.WhRedAcum, 0)) AS WhRed
                FROM Ultimas u
                OUTER APPLY (
                    SELECT TOP (1) m.WhSolAcum, m.WhRedAcum
                    FROM Mediciones m INNER JOIN Dispositivo d ON d.Id = m.DispositivoId
                    WHERE m.BootId = u.BootId AND m.Ts < {desdeUtc}
                    ORDER BY m.Ts DESC, m.Id DESC
                ) r
                WHERE u.Posicion = 1
            )
            SELECT e.WhSolar, e.WhRed, t.TicksSolar, t.TicksRed,
                (SELECT COUNT(*) FROM Eventos ev INNER JOIN Dispositivo d ON d.Id = ev.DispositivoId
                 WHERE ev.Tipo = N'conmutacion' AND ev.Ts >= {desdeUtc} AND ev.Ts <= {hastaUtc}) AS Conmutaciones
            FROM Dispositivo CROSS JOIN Energias e CROSS JOIN (
                SELECT SUM(CASE WHEN FuenteSeleccionada = N'SOLAR' THEN Ticks ELSE 0 END) AS TicksSolar,
                       SUM(CASE WHEN FuenteSeleccionada = N'CARGADOR' THEN Ticks ELSE 0 END) AS TicksRed
                FROM Duraciones
            ) t
            """).ToListAsync();

        var total = resultados.SingleOrDefault() ??
            throw new UnauthorizedAccessException("El dispositivo no pertenece a una empresa vinculada.");
        return new(total.WhSolar ?? 0, total.WhRed ?? 0,
            TimeSpan.FromTicks(total.TicksSolar ?? 0), TimeSpan.FromTicks(total.TicksRed ?? 0), total.Conmutaciones);
    }

    internal static EnergiaPorFuente CalcularMuestras(
        IEnumerable<Medicion> descendentes, IEnumerable<Medicion> referencias, int conmutaciones)
    {
        var porArranque = referencias.ToDictionary(m => m.BootId);
        var arranques = new HashSet<long>();
        decimal whSolar = 0, whRed = 0;
        TimeSpan tiempoSolar = TimeSpan.Zero, tiempoRed = TimeSpan.Zero;
        Medicion? siguiente = null;

        foreach (var medicion in descendentes)
        {
            if (arranques.Add(medicion.BootId))
            {
                porArranque.TryGetValue(medicion.BootId, out var referencia);
                whSolar += medicion.WhSolAcum - (referencia?.WhSolAcum ?? 0);
                whRed += medicion.WhRedAcum - (referencia?.WhRedAcum ?? 0);
            }

            if (siguiente is not null)
            {
                var lapso = TimeSpan.FromTicks(Math.Min((siguiente.Ts - medicion.Ts).Ticks, 5 * TimeSpan.TicksPerSecond));
                if (medicion.FuenteSeleccionada == "SOLAR")
                    tiempoSolar += lapso;
                else if (medicion.FuenteSeleccionada == "CARGADOR")
                    tiempoRed += lapso;
            }

            siguiente = medicion;
        }

        return new(whSolar, whRed, tiempoSolar, tiempoRed, conmutaciones);
    }

    private sealed record Totales(
        [property: Precision(38, 6)] decimal? WhSolar, [property: Precision(38, 6)] decimal? WhRed,
        long? TicksSolar, long? TicksRed, int Conmutaciones);
}
