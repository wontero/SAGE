
using SAGE.Web.Api;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Telemetria;

// Patrón: Adapter — rol: adaptador
public sealed class AdaptadorTelemetriaEsp32
    : IAdaptadorTelemetria
{
    private static readonly HashSet<string> Modos =
        new(StringComparer.Ordinal)
        {
            "AUTO", "SOLAR", "CARGADOR"
        };

    private static readonly HashSet<string> Estados =
        new(StringComparer.Ordinal)
        {
            "EN_CARGADOR",
            "PROBANDO_SOLAR",
            "EN_SOLAR",
            "SIN_ALIMENTACION"
        };

    private static readonly HashSet<string> Fuentes =
        new(StringComparer.Ordinal)
        {
            "SOLAR", "CARGADOR"
        };

    private static readonly HashSet<string> Tipos =
        new(StringComparer.Ordinal)
        {
            "conmutacion",
            "estado",
            "alerta",
            "reinicio"
        };

    private static readonly HashSet<string> Origenes =
        new(StringComparer.Ordinal)
        {
            "regla", "orden", "arranque"
        };

    private static readonly HashSet<string> Motivos =
        new(StringComparer.Ordinal)
        {
            "intento_retorno_solar",
            "cargador_sin_tension",
            "solar_comprobada",
            "prueba_solar_fallida",
            "solar_insuficiente",
            "sin_fuente_valida",
            "cargador_recuperado",
            "orden_modo",
            "orden_fallida",
            "arranque"
        };

    public Medicion AMedicion(
        TelemetriaRequest paquete,
        int dispositivoId,
        DateTime horaServidor)
    {
        if (!Modos.Contains(paquete.Modo ?? "") ||
            !Estados.Contains(paquete.Estado ?? "") ||
            !Fuentes.Contains(
                paquete.FuenteSeleccionada ?? ""))
        {
            throw new FormatException(
                "El modo, estado o fuente seleccionada " +
                "no contiene un valor permitido.");
        }

        if (paquete.Solar is null ||
            paquete.Cargador is null)
        {
            throw new FormatException(
                "Las fuentes solar y cargador son obligatorias.");
        }

        return new Medicion
        {
            DispositivoId = dispositivoId,
            BootId = paquete.BootId,
            Seq = paquete.Seq,

            Ts = paquete.TsValida
                ? paquete.Ts?.UtcDateTime
                    ?? throw new FormatException(
                        "La fecha ts es obligatoria " +
                        "cuando ts_valida es true.")
                : horaServidor,

            TsValida = paquete.TsValida,
            TsServidor = horaServidor,

            VSol = paquete.Solar.V,
            ISolMa = paquete.Solar.IMa,
            PSolMw = paquete.Solar.PMw,
            WhSolAcum = paquete.Solar.WhAcum,

            VRed = paquete.Cargador.V,
            IRedMa = paquete.Cargador.IMa,
            PRedMw = paquete.Cargador.PMw,
            WhRedAcum = paquete.Cargador.WhAcum,

            Modo = paquete.Modo!,
            Estado = paquete.Estado!,
            FuenteSeleccionada =
                paquete.FuenteSeleccionada!,

            AlimentacionComprobada =
                paquete.AlimentacionComprobada
        };
    }

    public Evento? AEvento(
        EventoTelemetriaRequest evento,
        long bootId,
        bool tsValida,
        int dispositivoId,
        DateTime horaServidor)
    {
        if (evento.Seq < 0 ||
            !Tipos.Contains(evento.Tipo ?? "") ||
            !Estados.Contains(evento.Estado ?? "") ||
            !Motivos.Contains(evento.Motivo ?? "") ||
            !Origenes.Contains(evento.Origen ?? "") ||
            (evento.De is not null &&
                !Fuentes.Contains(evento.De)) ||
            (evento.A is not null &&
                !Fuentes.Contains(evento.A)) ||
            (evento.OrdenId < 0))
        {
            return null;
        }

        return new Evento
        {
            DispositivoId = dispositivoId,
            BootId = bootId,
            Seq = evento.Seq,

            Ts = tsValida && evento.Ts.HasValue
                ? evento.Ts.Value.UtcDateTime
                : horaServidor,

            TsValida =
                tsValida && evento.Ts.HasValue,

            TsServidor = horaServidor,

            Tipo = evento.Tipo!,
            Estado = evento.Estado!,
            De = evento.De,
            A = evento.A,
            Motivo = evento.Motivo!,
            Origen = evento.Origen!,
            OrdenId = evento.OrdenId
        };
    }
}
