
using SAGE.Web.Api;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Telemetria;

// Patrón: Adapter — rol: objetivo
public interface IAdaptadorTelemetria
{
    Medicion AMedicion(
        TelemetriaRequest paquete,
        int dispositivoId,
        DateTime horaServidor);

    Evento? AEvento(
        EventoTelemetriaRequest evento,
        long bootId,
        bool tsValida,
        int dispositivoId,
        DateTime horaServidor);
}
