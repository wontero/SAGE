using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAGE.Simulador;

// S23-M11: paquete de telemetría de la sección 4 (contrato v2).
public sealed class PaqueteTelemetria
{
    [JsonPropertyName("boot_id")] public long BootId { get; init; }
    [JsonPropertyName("seq")] public long Seq { get; init; }
    [JsonPropertyName("ts")] public string Ts { get; init; } = "";
    [JsonPropertyName("ts_valida")] public bool TsValida { get; init; } = true;
    [JsonPropertyName("modo")] public string Modo { get; init; } = "";
    [JsonPropertyName("estado")] public string Estado { get; init; } = "";
    [JsonPropertyName("fuente_seleccionada")] public string FuenteSeleccionada { get; init; } = "";
    [JsonPropertyName("alimentacion_comprobada")] public bool AlimentacionComprobada { get; init; }
    [JsonPropertyName("parametros_version")] public int ParametrosVersion { get; init; }
    [JsonPropertyName("solar")] public RamaTelemetria Solar { get; init; } = new(0, 0, 0, 0);
    [JsonPropertyName("cargador")] public RamaTelemetria Cargador { get; init; } = new(0, 0, 0, 0);
    [JsonPropertyName("eventos")] public List<EventoTelemetria> Eventos { get; init; } = new();
    [JsonPropertyName("resultados_ordenes")] public List<ResultadoOrden> ResultadosOrdenes { get; init; } = new();

    public static string FormatoHora(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}

public sealed record RamaTelemetria(
    [property: JsonPropertyName("v")] double V,
    [property: JsonPropertyName("i_ma")] double IMa,
    [property: JsonPropertyName("p_mw")] double PMw,
    [property: JsonPropertyName("wh_acum")] double WhAcum)
{
    public static RamaTelemetria Desde(double v, double iMa, double whAcum) =>
        new(Math.Round(v, 3), Math.Round(iMa, 1), Math.Round(v * iMa, 0), Math.Round(whAcum, 6));
}

public sealed record EventoTelemetria(
    [property: JsonPropertyName("seq")] long Seq,
    [property: JsonPropertyName("ts")] string Ts,
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("estado")] string Estado,
    [property: JsonPropertyName("de")] string? De,
    [property: JsonPropertyName("a")] string? A,
    [property: JsonPropertyName("motivo")] string Motivo,
    [property: JsonPropertyName("origen")] string Origen,
    [property: JsonPropertyName("orden_id")] long? OrdenId)
{
    public static EventoTelemetria Desde(EventoDispositivo e) =>
        new(e.Seq, PaqueteTelemetria.FormatoHora(e.Ts), e.Tipo, e.Estado, e.De, e.A, e.Motivo, e.Origen, e.OrdenId);
}

// Respuesta del servidor: código HTTP y la orden (si vino una).
public sealed record RespuestaEnvio(int? Codigo, JsonElement? Orden, string? Error)
{
    public bool Ok => Codigo == 200;
}

// S23-M11: envía la telemetría por HTTP (POST /api/dispositivo/telemetria).
public sealed class Cliente : IDisposable
{
    private readonly HttpClient _http;
    private readonly Uri _destino;

    public Cliente(string urlBase, string token, TimeSpan timeout)
    {
        _http = new HttpClient { Timeout = timeout };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _destino = new Uri(new Uri(urlBase.TrimEnd('/') + "/"), "api/dispositivo/telemetria");
    }

    public Uri Destino => _destino;

    public async Task<RespuestaEnvio> EnviarAsync(PaqueteTelemetria paquete, CancellationToken ct)
    {
        try
        {
            using var respuesta = await _http.PostAsJsonAsync(_destino, paquete, ct);
            var codigo = (int)respuesta.StatusCode;

            if (codigo != 200)
                return new RespuestaEnvio(codigo, null, null);

            await using var cuerpo = await respuesta.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(cuerpo, cancellationToken: ct);

            JsonElement? orden = doc.RootElement.TryGetProperty("orden", out var o) &&
                                 o.ValueKind == JsonValueKind.Object
                ? o.Clone()
                : null;

            return new RespuestaEnvio(codigo, orden, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new RespuestaEnvio(null, null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Dispose() => _http.Dispose();
}
