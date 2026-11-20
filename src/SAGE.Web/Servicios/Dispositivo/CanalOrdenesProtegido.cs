using SAGE.Web.Data;
using SAGE.Web.Servicios.Configuracion;
using SAGE.Web.Servicios.Ordenes;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Proxy — rol: proxy de protección
public sealed class CanalOrdenesProtegido(
    ICanalOrdenes canal, IUsuarioActual usuario, IServiceScopeFactory scopeFactory) : ICanalOrdenes
{
    public async Task<Orden> EnviarModoAsync(int dispositivoId, string modo)
    {
        await VerificarControlAsync(dispositivoId);
        return await canal.EnviarModoAsync(dispositivoId, modo);
    }

    public async Task<Orden> EnviarParametrosAsync(int dispositivoId, ParametrosFuente p)
    {
        await VerificarControlAsync(dispositivoId);
        return await canal.EnviarParametrosAsync(dispositivoId, p);
    }

    public async Task<IReadOnlyList<Orden>> UltimasAsync(int dispositivoId, int cantidad)
    {
        await VerificarControlAsync(dispositivoId);
        return await canal.UltimasAsync(dispositivoId, cantidad);
    }

    private async Task VerificarControlAsync(int dispositivoId)
    {
        await usuario.CargarAsync();
        if (usuario.EsOperador || (!usuario.EsAdministrador && !usuario.EsTecnico))
            throw new UnauthorizedAccessException("Su rol no permite cambiar el modo de fuente.");

        await LectorTelemetriaProtegido.VerificarEmpresaActivaAsync(dispositivoId, usuario, scopeFactory);
    }
}
