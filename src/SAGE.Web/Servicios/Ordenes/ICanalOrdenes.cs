using SAGE.Web.Data;
using SAGE.Web.Servicios.Configuracion;

namespace SAGE.Web.Servicios.Ordenes;

// S23-M9: canal por el que la plataforma envía órdenes a un dispositivo.
// Las órdenes se guardan y viajan en la respuesta de la siguiente telemetría.
// Patrón: Command — rol: invocador
// Patrón: Abstract Factory — rol: producto abstracto
public interface ICanalOrdenes
{
    // AUTO | SOLAR | CARGADOR
    Task<Orden> EnviarModoAsync(int dispositivoId, string modo);

    // S23-M10: parámetros de la máquina de estados (con su versión).
    Task<Orden> EnviarParametrosAsync(int dispositivoId, ParametrosFuente p);

    Task<IReadOnlyList<Orden>> UltimasAsync(int dispositivoId, int cantidad);
}
