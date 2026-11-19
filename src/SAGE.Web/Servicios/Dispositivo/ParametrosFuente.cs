
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAGE.Web.Servicios.Configuracion;

public sealed record ParametrosFuente(
    decimal VMin,
    decimal VRetorno,
    decimal IMinMa,
    decimal TFallaS,
    decimal TEstableS,
    decimal TPruebaS)
{
    public static ParametrosFuente Predeterminados =>
        new(
            4.50m,
            4.85m,
            20m,
            1.0m,
            30m,
            5m
        );

    // S23-M10: versión de los parámetros (0 = predeterminados de fábrica).
    public int Version { get; init; }

    // =====================================================
    // S23-M9 / S23-M10: JSON de la orden "parametros" (sección 4)
    // =====================================================

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private sealed record ParametrosJson(
        [property: JsonPropertyName("version")] int Version,
        [property: JsonPropertyName("v_min")] decimal VMin,
        [property: JsonPropertyName("v_retorno")] decimal VRetorno,
        [property: JsonPropertyName("i_min_ma")] decimal IMinMa,
        [property: JsonPropertyName("t_falla_s")] decimal TFallaS,
        [property: JsonPropertyName("t_estable_s")] decimal TEstableS,
        [property: JsonPropertyName("t_prueba_s")] decimal TPruebaS);

    // { "version": 4, "v_min": 4.50, "v_retorno": 4.85, "i_min_ma": 20,
    //   "t_falla_s": 1.0, "t_estable_s": 30, "t_prueba_s": 5 }
    public string AJsonOrden()
    {
        return JsonSerializer.Serialize(
            new ParametrosJson(Version, VMin, VRetorno, IMinMa, TFallaS, TEstableS, TPruebaS),
            OpcionesJson);
    }

    // Lee el JSON guardado en Dispositivo.ParametrosJson (null = predeterminados).
    public static ParametrosFuente DesdeJson(string? json, int version)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Predeterminados with { Version = version };

        try
        {
            var p = JsonSerializer.Deserialize<ParametrosJson>(json, OpcionesJson);

            if (p is null)
                return Predeterminados with { Version = version };

            return new ParametrosFuente(p.VMin, p.VRetorno, p.IMinMa, p.TFallaS, p.TEstableS, p.TPruebaS)
            {
                Version = version
            };
        }
        catch (JsonException)
        {
            return Predeterminados with { Version = version };
        }
    }
}
