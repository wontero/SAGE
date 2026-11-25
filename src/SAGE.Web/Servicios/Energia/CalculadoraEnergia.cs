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
        int empresaId, int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, bool soloMedida = false)
    {
        await usuario.CargarAsync();
        if (usuario.EsOperador || usuario.DebeCambiarContrasena || !usuario.PuedeVerEmpresa(empresaId))
            throw new UnauthorizedAccessException("El dispositivo no pertenece a una empresa vinculada.");

        if (hastaUtc < desdeUtc)
            throw new ArgumentException("El fin debe ser posterior al inicio.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!soloMedida && fabrica is FabricaDispositivoSimulado)
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
            WITH Pares AS (
                SELECT m.FuenteSeleccionada, m.Ts, LEAD(m.Ts) OVER (ORDER BY m.Ts, m.Id) AS Siguiente
                FROM Mediciones m
                WHERE m.DispositivoId = {dispositivoId} AND m.Ts >= {desdeUtc} AND m.Ts <= {hastaUtc}
            ), Duraciones AS (
                SELECT FuenteSeleccionada,
                    CASE WHEN Siguiente IS NULL THEN 0
                         WHEN DATEDIFF_BIG(second, Ts, Siguiente) > 5 THEN 50000000
                         WHEN DATEDIFF_BIG(nanosecond, Ts, Siguiente) > 5000000000 THEN 50000000
                         ELSE DATEDIFF_BIG(nanosecond, Ts, Siguiente) / 100
                    END AS Ticks
                FROM Pares
            ), Arranques AS (
                -- Acota la lectura al intervalo, aunque el dispositivo tenga un historial mucho mayor.
                SELECT DISTINCT m.BootId
                FROM Mediciones m WITH (FORCESEEK (IX_Mediciones_DispositivoId_Ts (DispositivoId, Ts)))
                WHERE m.DispositivoId = {dispositivoId} AND m.Ts >= {desdeUtc} AND m.Ts <= {hastaUtc}
            ), Energias AS (
                -- Si el contador quedó por debajo de la referencia, es otra corrida con el mismo BootId:
                -- se toma como reinicio del contador (cuenta desde 0) en vez de dar energía negativa.
                SELECT SUM(CASE WHEN u.WhSolAcum >= COALESCE(r.WhSolAcum, 0)
                                THEN u.WhSolAcum - COALESCE(r.WhSolAcum, 0) ELSE u.WhSolAcum END) AS WhSolar,
                    SUM(CASE WHEN u.WhRedAcum >= COALESCE(r.WhRedAcum, 0)
                             THEN u.WhRedAcum - COALESCE(r.WhRedAcum, 0) ELSE u.WhRedAcum END) AS WhRed
                FROM Arranques a
                CROSS APPLY (
                    SELECT TOP (1) m.WhSolAcum, m.WhRedAcum
                    FROM Mediciones m
                    WHERE m.DispositivoId = {dispositivoId} AND m.BootId = a.BootId
                        AND m.Ts >= {desdeUtc} AND m.Ts <= {hastaUtc}
                    ORDER BY m.Ts DESC, m.Id DESC
                ) u
                OUTER APPLY (
                    SELECT TOP (1) m.WhSolAcum, m.WhRedAcum
                    FROM Mediciones m
                    WHERE m.DispositivoId = {dispositivoId} AND m.BootId = a.BootId AND m.Ts < {desdeUtc}
                    ORDER BY m.Ts DESC, m.Id DESC
                ) r
            )
            SELECT e.WhSolar, e.WhRed, t.TicksSolar, t.TicksRed,
                (SELECT COUNT(*) FROM Eventos ev WHERE ev.DispositivoId = {dispositivoId}
                    AND ev.Tipo = N'conmutacion' AND ev.Ts >= {desdeUtc} AND ev.Ts <= {hastaUtc}) AS Conmutaciones
            FROM Energias e CROSS JOIN (
                SELECT SUM(CASE WHEN FuenteSeleccionada = N'SOLAR' THEN Ticks ELSE 0 END) AS TicksSolar,
                       SUM(CASE WHEN FuenteSeleccionada = N'CARGADOR' THEN Ticks ELSE 0 END) AS TicksRed
                FROM Duraciones
            ) t
            WHERE EXISTS (SELECT 1 FROM Dispositivos WHERE Id = {dispositivoId} AND EmpresaId = {empresaId})
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
                whSolar += Delta(medicion.WhSolAcum, referencia?.WhSolAcum ?? 0);
                whRed += Delta(medicion.WhRedAcum, referencia?.WhRedAcum ?? 0);
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

        // Mismo criterio que la consulta SQL: un contador menor que la referencia se toma como reinicio.
        static decimal Delta(decimal actual, decimal referencia) =>
            actual >= referencia ? actual - referencia : actual;
    }

    private sealed record Totales(
        [property: Precision(38, 6)] decimal? WhSolar, [property: Precision(38, 6)] decimal? WhRed,
        long? TicksSolar, long? TicksRed, int Conmutaciones);
}
