using SAGE.Web.Servicios.Ordenes;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Abstract Factory — rol: fábrica abstracta
public interface IFabricaDispositivo
{
    ILectorTelemetria CrearLector();
    ICanalOrdenes CrearCanalOrdenes();
}
