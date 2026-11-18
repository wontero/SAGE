
using System.Net.Http.Headers;
using System.Text.Json;
using SAGE.Web.Servicios.Telemetria;

namespace SAGE.Web.Api;

public static class TelemetriaEndpoints
{
    public static IEndpointRouteBuilder MapTelemetriaEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/dispositivo/telemetria",
                RecibirAsync)
            .AllowAnonymous()
            .DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> RecibirAsync(
        HttpRequest request,
        FachadaSincronizacion fachada,
        CancellationToken cancellationToken)
    {
        // 1. Obtener el token Bearer del dispositivo.
        var encabezado = request.Headers.Authorization.ToString();

        if (!AuthenticationHeaderValue.TryParse(
                encabezado, out var autorizacion) ||
            !string.Equals(
                autorizacion.Scheme,
                "Bearer",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(autorizacion.Parameter) ||
            autorizacion.Parameter.Length > 256)
        {
            return Results.Unauthorized();
        }

        // 2. Comprobar que la petición sea JSON.
        if (!request.HasJsonContentType())
        {
            return Results.BadRequest(new
            {
                error = "El contenido debe ser application/json."
            });
        }

        // 3. Leer y deserializar los datos recibidos.
        TelemetriaRequest? datos;

        try
        {
            datos = await JsonSerializer
                .DeserializeAsync<TelemetriaRequest>(
                    request.Body,
                    cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new
            {
                error = "El JSON es inválido, faltan campos " +
                        "obligatorios o algún campo tiene " +
                        "un tipo incorrecto."
            });
        }

        // 4. Validar los datos del dispositivo.
        var error = Validar(datos);

        if (error is not null)
        {
            return Results.BadRequest(new { error });
        }

        // 5. Procesar la telemetría mediante Facade.
        RespuestaSincronizacion? respuesta;

        try
        {
            respuesta = await fachada.SincronizarAsync(
                autorizacion.Parameter,
                datos!,
                cancellationToken);
        }
        catch (FormatException ex)
        {
            return Results.BadRequest(new
            {
                error = ex.Message
            });
        }

        // 6. Comprobar autorización del dispositivo.
        if (respuesta is null)
        {
            return Results.Unauthorized();
        }

        // 7. Responder al ESP32.
        return Results.Ok(new
        {
            hora_servidor = respuesta.HoraServidor,
            orden = respuesta.Orden
        });
    }

    private static string? Validar(TelemetriaRequest? datos)
    {
        if (datos is null)
        {
            return "El cuerpo de la petición no puede estar vacío.";
        }

        if (datos.BootId < 0 || datos.Seq < 0)
        {
            return "boot_id y seq no pueden ser negativos.";
        }

        if (datos.TsValida && datos.Ts is null)
        {
            return "ts debe contener una fecha cuando " +
                   "ts_valida es true.";
        }

        if (!TextoValido(datos.Modo) ||
            !TextoValido(datos.Estado) ||
            !TextoValido(datos.FuenteSeleccionada))
        {
            return "modo, estado y fuente_seleccionada " +
                   "son obligatorios y admiten hasta " +
                   "50 caracteres.";
        }

        if (datos.Solar is null || datos.Cargador is null)
        {
            return "Los objetos solar y cargador son obligatorios.";
        }

        if (!FuenteValida(datos.Solar) ||
            !FuenteValida(datos.Cargador))
        {
            return "Los valores de las fuentes están fuera " +
                   "del rango admitido o la energía " +
                   "acumulada es negativa.";
        }

        if (datos.Eventos is null ||
            datos.ResultadosOrdenes is null)
        {
            return "eventos y resultados_ordenes " +
                   "deben ser arreglos.";
        }

        return null;
    }

    private static bool TextoValido(string? texto)
    {
        return !string.IsNullOrWhiteSpace(texto) &&
               texto.Length <= 50;
    }

    private static bool FuenteValida(
        FuenteTelemetriaRequest fuente)
    {
        const decimal limite = 999999999999.999999m;

        var valores = new[]
        {
            fuente.V,
            fuente.IMa,
            fuente.PMw,
            fuente.WhAcum
        };

        return fuente.WhAcum >= 0 &&
               valores.All(v =>
                   v >= -limite && v <= limite);
    }
}
