using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAGE.Web.Api;

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

    [JsonRequired]
    [JsonPropertyName("solar")]
    public FuenteTelemetriaRequest? Solar { get; set; }

    [JsonRequired]
    [JsonPropertyName("cargador")]
    public FuenteTelemetriaRequest? Cargador { get; set; }

    [JsonRequired]
    [JsonPropertyName("eventos")]
    public JsonElement Eventos { get; set; }

    [JsonRequired]
    [JsonPropertyName("resultados_ordenes")]
    public JsonElement ResultadosOrdenes { get; set; }
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