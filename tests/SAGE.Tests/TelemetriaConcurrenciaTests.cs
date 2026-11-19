
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace SAGE.Tests;

public class TelemetriaConcurrenciaTests
{
    private readonly ITestOutputHelper _output;

    public TelemetriaConcurrenciaTests(
        ITestOutputHelper output)
    {
        _output = output;
    }

    // =====================================================
    // CONFIGURACIÓN DEL ENTORNO DE PRUEBAS
    // =====================================================

    private bool PruebaHabilitada()
    {
        var habilitada = Environment.GetEnvironmentVariable(
            "SAGE_TEST_CONCURRENCIA");

        if (habilitada == "1")
            return true;

        _output.WriteLine(
            "Prueba no ejecutada: SAGE_TEST_CONCURRENCIA no es 1.");

        return false;
    }

    private static (Uri Url, string Token) ObtenerConfiguracion()
    {
        var url = Environment.GetEnvironmentVariable(
            "SAGE_TEST_URL");

        var token = Environment.GetEnvironmentVariable(
            "SAGE_TEST_TOKEN");

        Assert.False(
            string.IsNullOrWhiteSpace(url),
            "Falta SAGE_TEST_URL.");

        Assert.False(
            string.IsNullOrWhiteSpace(token),
            "Falta SAGE_TEST_TOKEN.");

        Assert.True(
            Uri.TryCreate(
                url,
                UriKind.Absolute,
                out var uri),
            "La URL configurada no es válida.");

        // Protección: ejecutar exclusivamente contra
        // el servidor local del perfil Concurrencia.
        Assert.True(
            uri!.Scheme == Uri.UriSchemeHttps &&
            uri.IsLoopback &&
            uri.Port == 7101 &&
            uri.AbsolutePath ==
                "/api/dispositivo/telemetria",
            "La prueba debe utilizar localhost:7101 " +
            "y el endpoint de telemetría.");

        return (uri, token!);
    }

    private static HttpClient CrearCliente(string token)
    {
        var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return http;
    }

    // =====================================================
    // CREAR TELEMETRÍA PARA LAS PRUEBAS
    // =====================================================

    private static object CrearPaquete(
        long bootId,
        long seq,
        decimal voltajeSolar,
        decimal voltajeRed)
    {
        var usandoSolar = voltajeSolar >= 4.50m;

        return new
        {
            boot_id = bootId,
            seq = seq,
            ts = DateTime.UtcNow,
            ts_valida = true,
            modo = "AUTO",
            estado = usandoSolar
                ? "EN_SOLAR"
                : "EN_CARGADOR",
            fuente_seleccionada = "SOLAR",
            alimentacion_comprobada = true,

            solar = new
            {
                v = voltajeSolar,
                i_ma = 200m,
                p_mw = voltajeSolar * 200m,
                wh_acum = 100m
            },

            cargador = new
            {
                v = voltajeRed,
                i_ma = 0m,
                p_mw = 0m,
                wh_acum = 100m
            },

            eventos = Array.Empty<object>(),
            resultados_ordenes = Array.Empty<object>()
        };
    }

    // =====================================================
    // ENVÍO DE DOS SOLICITUDES SIMULTÁNEAS
    // =====================================================

    private static async Task<(HttpResponseMessage, HttpResponseMessage)>
        EnviarSimultaneamenteAsync(
            HttpClient http,
            Uri url,
            object paquete1,
            object paquete2)
    {
        var inicio = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> EnviarAsync(object paquete)
        {
            await inicio.Task;

            return await http.PostAsJsonAsync(
                url,
                paquete);
        }

        var tarea1 = EnviarAsync(paquete1);
        var tarea2 = EnviarAsync(paquete2);

        inicio.SetResult(true);

        var respuestas = await Task.WhenAll(
            tarea1,
            tarea2);

        return (respuestas[0], respuestas[1]);
    }

    // =====================================================
    // PRUEBA 1: MEDICIONES DUPLICADAS
    // =====================================================

    [Fact]
    public async Task DosSolicitudesSimultaneas_NoDebenFallar()
    {
        if (!PruebaHabilitada())
            return;

        var (url, token) = ObtenerConfiguracion();

        using var http = CrearCliente(token);

        var bootId = Random.Shared.NextInt64(
            1_000_000_000,
            9_000_000_000);

        const long seq = 1;

        var paquete = CrearPaquete(
            bootId,
            seq,
            4.90m,
            5.05m);

        var (r1, r2) = await EnviarSimultaneamenteAsync(
            http,
            url,
            paquete,
            paquete);

        using (r1)
        using (r2)
        {
            _output.WriteLine(
                $"MEDICIONES - BootId: {bootId}");

            _output.WriteLine(
                $"Primera respuesta: {(int)r1.StatusCode}");

            _output.WriteLine(
                $"Segunda respuesta: {(int)r2.StatusCode}");

            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        }
    }

    // =====================================================
    // PRUEBA 2: ALERTAS SIMULTÁNEAS
    // =====================================================

    [Fact]
    public async Task DosSolicitudesSimultaneas_NoDebenDuplicarAlertas()
    {
        if (!PruebaHabilitada())
            return;

        var (url, token) = ObtenerConfiguracion();

        using var http = CrearCliente(token);

        var bootId = Random.Shared.NextInt64(
            1_000_000_000,
            9_000_000_000);

        // Primero enviar una medición normal
        // para resolver cualquier alerta solar anterior.
        var paqueteNormal = CrearPaquete(
            bootId,
            1,
            4.90m,
            5.05m);

        using (var respuestaNormal = await http.PostAsJsonAsync(
            url,
            paqueteNormal))
        {
            Assert.Equal(
                HttpStatusCode.OK,
                respuestaNormal.StatusCode);
        }

        // Las dos solicitudes tienen secuencias diferentes.
        // Ambas provocan SOLAR_INSUFICIENTE.
        var paquete1 = CrearPaquete(
            bootId,
            2,
            4.00m,
            5.05m);

        var paquete2 = CrearPaquete(
            bootId,
            3,
            4.00m,
            5.05m);

        var (r1, r2) = await EnviarSimultaneamenteAsync(
            http,
            url,
            paquete1,
            paquete2);

        using (r1)
        using (r2)
        {
            var cuerpo1 = await r1.Content.ReadAsStringAsync();
            var cuerpo2 = await r2.Content.ReadAsStringAsync();

            _output.WriteLine(
                $"ALERTAS - BootId: {bootId}");

            _output.WriteLine(
                "Condición: SOLAR_INSUFICIENTE");

            _output.WriteLine(
                $"Solicitud seq=2: {(int)r1.StatusCode}");

            _output.WriteLine(
                $"Solicitud seq=3: {(int)r2.StatusCode}");

            _output.WriteLine(
                $"Respuesta 1: {cuerpo1}");

            _output.WriteLine(
                $"Respuesta 2: {cuerpo2}");

            Assert.True(
                r1.StatusCode == HttpStatusCode.OK &&
                r2.StatusCode == HttpStatusCode.OK,
                "Las dos solicitudes deben ser procesadas " +
                "sin fallos de concurrencia. " +
                $"HTTP: {(int)r1.StatusCode} " +
                $"y {(int)r2.StatusCode}.");
        }

        _output.WriteLine(
            "Verificar en SAGE_Pruebas que exista " +
            "una sola alerta SOLAR_INSUFICIENTE activa.");
    }
}
