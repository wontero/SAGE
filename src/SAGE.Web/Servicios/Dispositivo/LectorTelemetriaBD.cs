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
}
