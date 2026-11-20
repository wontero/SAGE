using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Abstract Factory — rol: producto concreto
internal sealed class LectorTelemetriaBD(IServiceScopeFactory scopeFactory, IUsuarioActual usuario) : ILectorTelemetria
{
    public Task<IReadOnlyList<Medicion>> MedicionesAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo) =>
        ConsultarMedicionesAsync(dispositivoId, desdeUtc, hastaUtc, maximo, porRecepcion: false);

    public Task<IReadOnlyList<Medicion>> MedicionesRecibidasAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo) =>
        ConsultarMedicionesAsync(dispositivoId, desdeUtc, hastaUtc, maximo, porRecepcion: true);

    private async Task<IReadOnlyList<Medicion>> ConsultarMedicionesAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo, bool porRecepcion)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var empresaId = usuario.EmpresaActivaId;
        var consulta = db.Mediciones.AsNoTracking()
            .Where(m => m.DispositivoId == dispositivoId && m.Dispositivo.EmpresaId == empresaId);
        var ordenada = porRecepcion
            ? consulta.Where(m => m.TsServidor >= desdeUtc && m.TsServidor <= hastaUtc).OrderByDescending(m => m.TsServidor)
            : consulta.Where(m => m.Ts >= desdeUtc && m.Ts <= hastaUtc).OrderByDescending(m => m.Ts);

        return await ordenada.ThenByDescending(m => m.Id)
            .Take(maximo)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Evento>> EventosAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var empresaId = usuario.EmpresaActivaId;
        return await db.Eventos.AsNoTracking()
            .Where(e => e.DispositivoId == dispositivoId && e.Dispositivo.EmpresaId == empresaId
                && e.Ts >= desdeUtc && e.Ts <= hastaUtc)
            .OrderByDescending(e => e.Ts)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PuntoPotenciaHistorial>> PuntosAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var empresaId = usuario.EmpresaActivaId;
        const int maximo = PuntoPotenciaHistorial.MaximoPuntos;
        var consulta = db.Mediciones.AsNoTracking()
            .Where(m => m.DispositivoId == dispositivoId && m.Dispositivo.EmpresaId == empresaId
                && m.Ts >= desdeUtc && m.Ts <= hastaUtc);
        var cantidad = await consulta.OrderBy(m => m.Ts).Take(maximo + 1).CountAsync();
        if (cantidad <= maximo)
        {
            return await consulta.OrderBy(m => m.Ts).ThenBy(m => m.Id)
                .Select(m => new PuntoPotenciaHistorial(m.Ts, m.PSolMw, m.PRedMw))
                .Take(maximo)
                .ToListAsync();
        }

        return await db.Database.SqlQuery<PuntoPotenciaHistorial>($"""
            SELECT MIN(m.Ts) AS Ts, AVG(m.PSolMw) AS PSolMw, AVG(m.PRedMw) AS PRedMw
            FROM Mediciones m INNER JOIN Dispositivos d ON d.Id = m.DispositivoId
            WHERE m.DispositivoId = {dispositivoId} AND d.EmpresaId = {empresaId}
                AND m.Ts >= {desdeUtc} AND m.Ts <= {hastaUtc}
            GROUP BY DATEDIFF_BIG(millisecond, {desdeUtc}, m.Ts) * {maximo}
                / (DATEDIFF_BIG(millisecond, {desdeUtc}, {hastaUtc}) + 1)
            ORDER BY MIN(m.Ts)
            """).ToListAsync();
    }
}
