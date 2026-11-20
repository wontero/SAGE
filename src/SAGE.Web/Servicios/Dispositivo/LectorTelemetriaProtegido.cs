using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Proxy — rol: proxy de protección
public sealed class LectorTelemetriaProtegido(
    ILectorTelemetria lector, IUsuarioActual usuario, IServiceScopeFactory scopeFactory) : ILectorTelemetria
{
    public async Task<IReadOnlyList<Medicion>> MedicionesAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo)
    {
        await VerificarAccesoAsync(dispositivoId);
        return await lector.MedicionesAsync(dispositivoId, desdeUtc, hastaUtc, maximo);
    }

    public async Task<IReadOnlyList<Evento>> EventosAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc)
    {
        await VerificarAccesoAsync(dispositivoId);
        return await lector.EventosAsync(dispositivoId, desdeUtc, hastaUtc);
    }

    public async Task<IReadOnlyList<Medicion>> MedicionesRecibidasAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo)
    {
        await VerificarAccesoAsync(dispositivoId);
        return await lector.MedicionesRecibidasAsync(dispositivoId, desdeUtc, hastaUtc, maximo);
    }

    async Task<IReadOnlyList<PuntoPotenciaHistorial>> ILectorTelemetria.PuntosAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc)
    {
        await VerificarAccesoAsync(dispositivoId);
        return await lector.PuntosAsync(dispositivoId, desdeUtc, hastaUtc);
    }

    private async Task VerificarAccesoAsync(int dispositivoId)
    {
        await usuario.CargarAsync();
        if (usuario.EsOperador)
            throw new UnauthorizedAccessException("Su rol no permite leer la telemetría.");

        await VerificarEmpresaActivaAsync(dispositivoId, usuario, scopeFactory);
    }

    internal static async Task VerificarEmpresaActivaAsync(
        int dispositivoId, IUsuarioActual usuario, IServiceScopeFactory scopeFactory)
    {
        if (usuario.EmpresaActivaId is int empresaId && usuario.PuedeVerEmpresa(empresaId)
            && !usuario.DebeCambiarContrasena)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (await db.Dispositivos.AnyAsync(d => d.Id == dispositivoId && d.EmpresaId == empresaId)
                && usuario.EmpresaActivaId == empresaId)
                return;
        }

        throw new UnauthorizedAccessException("El dispositivo no pertenece a su empresa activa.");
    }
}
