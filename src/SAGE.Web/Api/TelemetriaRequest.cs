
using System.Text.Json.Serialization;

namespace SAGE.Web.Api;

// Patrón: Adapter — rol: adaptado
public sealed class TelemetriaRequest
{
    [JsonRequired]
    [JsonPropertyName("boot_id")]
    public long BootId { get; set; }

    [JsonRequired]
    [JsonPropertyName("seq")]
    public long Seq { get; set; }

    [JsonRequired]
    [JsonPropertyName("ts")]
    public DateTimeOffset? Ts { get; set; }

    [JsonRequired]
    [JsonPropertyName("ts_valida")]
    public bool TsValida { get; set; }

    [JsonRequired]
    [JsonPropertyName("modo")]
    public string? Modo { get; set; }

    [JsonRequired]
    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    [JsonRequired]
    [JsonPropertyName("fuente_seleccionada")]
    public string? FuenteSeleccionada { get; set; }

    [JsonRequired]
    [JsonPropertyName("alimentacion_comprobada")]
    public bool AlimentacionComprobada { get; set; }

    // S23-M5: campo opcional del contrato v2
    [JsonPropertyName("parametros_version")]
    public int ParametrosVersion { get; set; }

    [JsonRequired]
    [JsonPropertyName("solar")]
    public FuenteTelemetriaRequest? Solar { get; set; }

    [JsonRequired]
    [JsonPropertyName("cargador")]
    public FuenteTelemetriaRequest? Cargador { get; set; }

    [JsonRequired]
    [JsonPropertyName("eventos")]
    public List<EventoTelemetriaRequest>? Eventos { get; set; }

    [JsonRequired]
    [JsonPropertyName("resultados_ordenes")]
    public List<ResultadoOrdenRequest>? ResultadosOrdenes
    { get; set; }
}

public sealed class FuenteTelemetriaRequest
{
    [JsonRequired]
    [JsonPropertyName("v")]
    public decimal V { get; set; }

    [JsonRequired]
    [JsonPropertyName("i_ma")]
    public decimal IMa { get; set; }

    [JsonRequired]
    [JsonPropertyName("p_mw")]
    public decimal PMw { get; set; }

    [JsonRequired]
    [JsonPropertyName("wh_acum")]
    public decimal WhAcum { get; set; }
}

// Evento enviado por el dispositivo ESP32
public sealed class EventoTelemetriaRequest
{
    [JsonRequired]
    [JsonPropertyName("seq")]
    public long Seq { get; set; }

    [JsonRequired]
    [JsonPropertyName("ts")]
    public DateTimeOffset? Ts { get; set; }

    [JsonRequired]
    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonRequired]
    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    [JsonRequired]
    [JsonPropertyName("de")]
    public string? De { get; set; }

    [JsonRequired]
    [JsonPropertyName("a")]
    public string? A { get; set; }

    [JsonRequired]
    [JsonPropertyName("motivo")]
    public string? Motivo { get; set; }

    [JsonRequired]
    [JsonPropertyName("origen")]
    public string? Origen { get; set; }

    [JsonRequired]
    [JsonPropertyName("orden_id")]
    public long? OrdenId { get; set; }
}

// Resultado de una orden ejecutada por el ESP32
public sealed class ResultadoOrdenRequest
{
    [JsonRequired]
    [JsonPropertyName("orden_id")]
    public long OrdenId { get; set; }

    [JsonRequired]
    [JsonPropertyName("resultado")]
    public string? Resultado { get; set; }

    [JsonRequired]
    [JsonPropertyName("motivo")]
    public string? Motivo { get; set; }
}
