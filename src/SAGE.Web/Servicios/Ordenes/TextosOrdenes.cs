using System.Text.Json;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Ordenes;

// S23-M9: textos en español y formato de entrega de las órdenes.
public static class TextosOrdenes
{
    public static string Motivo(string? motivo) => motivo switch
    {
        null or "" => "",
        "solar_no_disponible" => "Solar no disponible",
        "orden_fallida" => "La prueba de solar falló",
        "parametros_invalidos" => "Parámetros fuera de rango",
        "orden_desconocida" => "Orden desconocida",
        Orden.MotivoNoEntregada => "No se entregó a tiempo (30 s)",
        Orden.MotivoSinConfirmacion => "Sin confirmación del dispositivo",
        Orden.MotivoReemplazada => "Reemplazada por otra orden",
        _ => motivo
    };

    public static string Estado(string estado) => estado switch
    {
        Orden.EstadoPendiente => "Pendiente",
        Orden.EstadoEntregada => "Entregada",
        Orden.EstadoEjecutada => "Ejecutada",
        Orden.EstadoRechazada => "Rechazada",
        Orden.EstadoExpirada => "Expirada",
        _ => estado
    };

    // "Rechazada: Solar no disponible", "Expirada: No se entregó a tiempo (30 s)", "Ejecutada".
    public static string EstadoConMotivo(Orden orden)
    {
        var texto = Estado(orden.Estado);
        var motivo = Motivo(orden.Motivo);

        return string.IsNullOrEmpty(motivo) ? texto : $"{texto}: {motivo}";
    }

    // Texto corto de lo que pidió la orden.
    public static string Descripcion(Orden orden)
    {
        if (orden.Tipo == Orden.TipoModo)
        {
            var modo = LeerModo(orden.ValorJson);
            return "Modo " + Formato.Modo(modo, "");
        }

        if (orden.Tipo == Orden.TipoParametros)
        {
            try
            {
                using var doc = JsonDocument.Parse(orden.ValorJson);
                if (doc.RootElement.TryGetProperty("version", out var v))
                    return $"Parámetros v{v.GetInt32()}";
            }
            catch (JsonException)
            {
            }

            return "Parámetros";
        }

        return orden.Tipo;
    }

    public static string LeerModo(string valorJson)
    {
        try
        {
            return JsonSerializer.Deserialize<string>(valorJson) ?? "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    // Formato de la sección 4: { "id": 78, "tipo": "modo", "valor": "SOLAR" }.
    public static object ARespuesta(Orden orden)
    {
        using var doc = JsonDocument.Parse(orden.ValorJson);

        return new Dictionary<string, object?>
        {
            ["id"] = orden.Id,
            ["tipo"] = orden.Tipo,
            ["valor"] = doc.RootElement.Clone()
        };
    }
}
