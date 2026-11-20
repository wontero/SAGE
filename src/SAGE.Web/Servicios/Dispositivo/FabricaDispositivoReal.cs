using SAGE.Web.Servicios.EnVivo;
using SAGE.Web.Servicios.Ordenes;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Abstract Factory — rol: fábrica concreta
internal sealed class FabricaDispositivoReal(
    IServiceScopeFactory scopeFactory, IUsuarioActual usuario, EstadoDispositivosEnVivo enVivo) : IFabricaDispositivo
{
    public ILectorTelemetria CrearLector() =>
        new LectorTelemetriaProtegido(new LectorTelemetriaBD(scopeFactory, usuario), usuario, scopeFactory);

    public ICanalOrdenes CrearCanalOrdenes() =>
        new CanalOrdenesProtegido(new CanalOrdenesBD(scopeFactory, usuario, enVivo), usuario, scopeFactory);
}
