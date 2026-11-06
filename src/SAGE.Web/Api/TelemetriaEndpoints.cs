using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;

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
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        // 1. Autenticación mediante el token del dispositivo.
        var encabezado = request.Headers.Authorization.ToString();

        if (!AuthenticationHeaderValue.TryParse(
                encabezado, out var autorizacion) ||
            !string.Equals(
                autorizacion.Scheme,
                "Bearer",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(autorizacion.Parameter))
        {
            return Results.Unauthorized();
        }

        var token = autorizacion.Parameter;

        if (token.Length > 256)
            return Results.Unauthorized();

        var tokenHash = TokenDispositivo.CalcularHash(token);

        var dispositivo = await db.Dispositivos
            .SingleOrDefaultAsync(
                d => d.TokenHash == tokenHash &&
                     d.Activo &&
                     d.Empresa.Activa,
                cancellationToken);

        if (dispositivo is null)
            return Results.Unauthorized();

        // 2. Lectura del JSON.
        if (!request.HasJsonContentType())
        {
            return Results.BadRequest(new
            {
                error = "El contenido debe ser application/json."
            });
        }

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
                error = "El JSON es inválido, faltan campos obligatorios " +
                        "o algún campo tiene un tipo incorrecto."
            });
        }

        var error = Validar(datos);

        if (error is not null)
        {
            return Results.BadRequest(new { error });
        }

        var entrada = datos!;
        var horaServidor = DateTime.UtcNow;

        // 3. Comprobar si la medición ya fue recibida.
        var existe = await db.Mediciones.AnyAsync(
            m => m.DispositivoId == dispositivo.Id &&
                 m.BootId == entrada.BootId &&
                 m.Seq == entrada.Seq,
            cancellationToken);

        if (!existe)
        {
            var medicion = new Medicion
            {
                DispositivoId = dispositivo.Id,
                BootId = entrada.BootId,
                Seq = entrada.Seq,

                Ts = entrada.TsValida
                    ? entrada.Ts!.Value.UtcDateTime
                    : horaServidor,

                TsValida = entrada.TsValida,
                TsServidor = horaServidor,

                VSol = entrada.Solar!.V,
                ISolMa = entrada.Solar.IMa,
                PSolMw = entrada.Solar.PMw,
                WhSolAcum = entrada.Solar.WhAcum,

                VRed = entrada.Cargador!.V,
                IRedMa = entrada.Cargador.IMa,
                PRedMw = entrada.Cargador.PMw,
                WhRedAcum = entrada.Cargador.WhAcum,

                Modo = entrada.Modo!,
                Estado = entrada.Estado!,
                FuenteSeleccionada = entrada.FuenteSeleccionada!,
                AlimentacionComprobada = entrada.AlimentacionComprobada
            };

            db.Mediciones.Add(medicion);

            dispositivo.ModoActual = entrada.Modo;
            dispositivo.EstadoActual = entrada.Estado;
            dispositivo.FuenteSeleccionada = entrada.FuenteSeleccionada;
            dispositivo.AlimentacionComprobada =
                entrada.AlimentacionComprobada;
        }

        // Un reenvío también confirma que el dispositivo está conectado.
        // No reemplazamos su estado con el de una medición duplicada.
        dispositivo.UltimoContacto = horaServidor;

        try
        {
            // Guarda la medición y el estado del dispositivo juntos.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsDuplicado(ex))
        {
            // Dos peticiones iguales pueden llegar al mismo tiempo.
            // El índice único evita que ambas creen una fila.
            db.ChangeTracker.Clear();

            var dispositivoActual = await db.Dispositivos
                .SingleOrDefaultAsync(
                    d => d.TokenHash == tokenHash &&
                         d.Activo &&
                         d.Empresa.Activa,
                    cancellationToken);

            if (dispositivoActual is null)
                return Results.Unauthorized();

            var yaGuardada = await db.Mediciones.AnyAsync(
                m => m.DispositivoId == dispositivoActual.Id &&
                     m.BootId == entrada.BootId &&
                     m.Seq == entrada.Seq,
                cancellationToken);

            if (!yaGuardada)
                throw;

            dispositivoActual.UltimoContacto = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        // Los eventos y resultados de órdenes se aceptan,
        // pero no se guardan durante este sprint.
        return Results.Ok(new
        {
            hora_servidor = DateTime.UtcNow,
            orden = (object?)null
        });
    }

    private static string? Validar(TelemetriaRequest? datos)
    {
        if (datos is null)
            return "El cuerpo de la petición no puede estar vacío.";

        if (datos.BootId < 0 || datos.Seq < 0)
            return "boot_id y seq no pueden ser negativos.";

        if (datos.TsValida && datos.Ts is null)
            return "ts debe contener una fecha cuando ts_valida es true.";

        if (!TextoValido(datos.Modo) ||
            !TextoValido(datos.Estado) ||
            !TextoValido(datos.FuenteSeleccionada))
        {
            return "modo, estado y fuente_seleccionada son obligatorios " +
                   "y admiten hasta 50 caracteres.";
        }

        if (datos.Solar is null || datos.Cargador is null)
            return "Los objetos solar y cargador son obligatorios.";

        if (!FuenteValida(datos.Solar) ||
            !FuenteValida(datos.Cargador))
        {
            return "Los valores de las fuentes están fuera del rango " +
                   "admitido o la energía acumulada es negativa.";
        }

        if (datos.Eventos.ValueKind != JsonValueKind.Array ||
            datos.ResultadosOrdenes.ValueKind != JsonValueKind.Array)
        {
            return "eventos y resultados_ordenes deben ser arreglos.";
        }

        return null;
    }

    private static bool TextoValido(string? texto)
    {
        return !string.IsNullOrWhiteSpace(texto) &&
               texto.Length <= 50;
    }

    private static bool FuenteValida(FuenteTelemetriaRequest fuente)
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
               valores.All(v => v >= -limite && v <= limite);
    }

    private static bool EsDuplicado(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sql &&
               (sql.Number == 2601 || sql.Number == 2627);
    }
}