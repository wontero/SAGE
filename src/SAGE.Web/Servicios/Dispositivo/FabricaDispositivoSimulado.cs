using SAGE.Web.Servicios.EnVivo;
using SAGE.Web.Servicios.Ordenes;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Abstract Factory — rol: fábrica concreta
internal sealed class FabricaDispositivoSimulado(
    IServiceScopeFactory scopeFactory, IUsuarioActual usuario, EstadoDispositivosEnVivo enVivo,
    CanalOrdenesSimulado.Registro registro) : IFabricaDispositivo
{
    public ILectorTelemetria CrearLector() =>
        new LectorTelemetriaProtegido(new LectorTelemetriaSimulado(), usuario, scopeFactory);

    public ICanalOrdenes CrearCanalOrdenes() =>
        new CanalOrdenesProtegido(new CanalOrdenesSimulado(registro, usuario, enVivo), usuario, scopeFactory);
}
