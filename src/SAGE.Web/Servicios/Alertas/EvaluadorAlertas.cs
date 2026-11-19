
using SAGE.Web.Servicios.Configuracion;

namespace SAGE.Web.Servicios.Alertas;

public sealed class EvaluadorAlertas
{
    private readonly ParametrosFuente _parametros;

    public EvaluadorAlertas()
    {
        _parametros = ParametrosFuente.Predeterminados;
    }

    // S23-M7: Solar insuficiente.
    // Solo generar alerta cuando se está utilizando SOLAR.
    public bool SolarInsuficiente(
        decimal voltajeSolar,
        string? fuenteSeleccionada)
    {
        var usandoSolar = string.Equals(
            fuenteSeleccionada,
            "SOLAR",
            StringComparison.OrdinalIgnoreCase);

        return usandoSolar &&
               voltajeSolar < _parametros.VMin;
    }

    // S23-M7: Red sin tensión.
    // Umbral provisional: ausencia total de tensión.
    public bool RedSinTension(decimal voltajeRed)
    {
        return voltajeRed <= 0m;
    }

    // S23-M7: Ninguna fuente válida.
    // Ambas fuentes presentan tensión insuficiente
    // según las reglas provisionales.
    public bool NingunaFuenteValida(
        decimal voltajeSolar,
        decimal voltajeRed)
    {
        return voltajeSolar < _parametros.VMin &&
               RedSinTension(voltajeRed);
    }

    // S23-M7: Desconexión de dispositivo.
    public bool SinConexion(
        DateTime? ultimoContacto,
        DateTime horaActual)
    {
        // No evaluar desconexión si el dispositivo
        // nunca se comunicó.
        if (!ultimoContacto.HasValue)
            return false;

        return horaActual - ultimoContacto.Value >
               TimeSpan.FromSeconds(30);
    }
}
