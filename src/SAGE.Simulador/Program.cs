using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

// Simulador del dispositivo SAGE (imita al ESP32).
// Uso:
// dotnet run --project src/SAGE.Simulador --
// --url https://localhost:7001 --token <TOKEN>
// [--intervalo 1] [--hora 12]

var opciones = Opciones.Leer(args);

if (opciones is null)
{
    Opciones.MostrarUso();
    return 1;
}

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var http = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(5)
};

http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue(
        "Bearer",
        opciones.Token);

var destino = new Uri(
    new Uri(opciones.Url.TrimEnd('/') + "/"),
    "api/dispositivo/telemetria");

var generador = new Generador(opciones.HoraFija);

var intervalo = TimeSpan.FromSeconds(
    opciones.IntervaloSegundos);

Console.WriteLine(
    $"SAGE.Simulador -> {destino} " +
    $"(cada {opciones.IntervaloSegundos} s). " +
    "Ctrl+C para salir.");

Console.WriteLine($"boot_id = {generador.BootId}");

using var reloj = new PeriodicTimer(intervalo);

try
{
    do
    {
        var medicion = generador.Siguiente(
            opciones.IntervaloSegundos);

        var hora = DateTime.Now.ToString(
            "HH:mm:ss",
            CultureInfo.InvariantCulture);

        var potencia =
            medicion.FuenteSeleccionada == "SOLAR"
                ? medicion.Solar.PMw
                : medicion.Cargador.PMw;

        try
        {
            using var respuesta =
                await http.PostAsJsonAsync(
                    destino,
                    medicion,
                    cts.Token);

            Console.WriteLine(
                $"{hora} seq={medicion.Seq} " +
                $"fuente={medicion.FuenteSeleccionada} " +
                $"potencia={potencia.ToString("F0", CultureInfo.InvariantCulture)} mW " +
                $"HTTP {(int)respuesta.StatusCode}");

            if (medicion.Eventos.Length > 0)
            {
                Console.WriteLine(
                    $"  Evento enviado: reinicio " +
                    $"({medicion.Eventos.Length} evento).");
            }
        }
        catch (OperationCanceledException)
            when (cts.IsCancellationRequested)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"{hora} seq={medicion.Seq} " +
                $"ERROR de envío: " +
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }
    while (await reloj.WaitForNextTickAsync(cts.Token));
}
catch (OperationCanceledException)
{
    // Salida normal con Ctrl+C.
}

Console.WriteLine("Simulador detenido.");

return 0;

// =====================================================
// OPCIONES DEL SIMULADOR
// =====================================================

internal sealed record Opciones(
    string Url,
    string Token,
    int IntervaloSegundos,
    double? HoraFija)
{
    public static Opciones? Leer(string[] args)
    {
        string? url = null;
        string? token = null;

        // M6: intervalo predeterminado de 1 segundo.
        var intervalo = 1;
        double? hora = null;

        for (var i = 0; i < args.Length; i++)
        {
            var valor =
                i + 1 < args.Length
                    ? args[i + 1]
                    : null;

            switch (args[i])
            {
                case "--url":
                    url = valor;
                    i++;
                    break;

                case "--token":
                    token = valor;
                    i++;
                    break;

                case "--intervalo":
                    if (!int.TryParse(
                            valor,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out intervalo) ||
                        intervalo < 1)
                    {
                        Console.WriteLine(
                            "--intervalo debe ser un entero " +
                            "de 1 o más segundos.");

                        return null;
                    }

                    i++;
                    break;

                case "--hora":
                    if (!double.TryParse(
                            valor,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var h) ||
                        h < 0 ||
                        h >= 24)
                    {
                        Console.WriteLine(
                            "--hora debe estar entre 0 y 24.");

                        return null;
                    }

                    hora = h;
                    i++;
                    break;

                default:
                    Console.WriteLine(
                        $"Parámetro desconocido: {args[i]}");

                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(url) ||
            string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return new Opciones(
            url,
            token,
            intervalo,
            hora);
    }

    public static void MostrarUso()
    {
        Console.WriteLine(
            "Simulador del dispositivo SAGE");

        Console.WriteLine();

        Console.WriteLine("Uso:");

        Console.WriteLine(
            "dotnet run --project src/SAGE.Simulador -- " +
            "--url <URL> --token <TOKEN> " +
            "[--intervalo <segundos>] [--hora <0-24>]");

        Console.WriteLine();

        Console.WriteLine(
            "--url        Dirección de la plataforma.");

        Console.WriteLine(
            "--token      Token del dispositivo.");

        Console.WriteLine(
            "--intervalo  Segundos entre envíos.");

        Console.WriteLine(
            "--hora       Hora simulada del día.");
    }
}

// =====================================================
// GENERADOR DE TELEMETRÍA
// =====================================================

internal sealed class Generador
{
    private readonly Random _azar = new();

    private readonly double? _horaFija;

    private double _whSol;
    private double _whRed;

    private int _nubeRestante;

    public Generador(double? horaFija)
    {
        _horaFija = horaFija;

        BootId = _azar.NextInt64(
            1,
            1_000_000_000);
    }

    public long BootId { get; }

    private long _seq;

    public Telemetria Siguiente(int intervaloSegundos)
    {
        _seq++;

        var irradiancia = Irradiancia(
            intervaloSegundos);

        var haySol = irradiancia >= 0.25;

        double vSol;
        double iSol;
        double vRed;
        double iRed;

        string estado;
        string fuente;

        if (haySol)
        {
            vSol = 4.9 + Ruido(0.03);

            iSol = Math.Clamp(
                150 + 50 * irradiancia + Ruido(4),
                150,
                200);

            vRed = 5.05 + Ruido(0.02);

            iRed = Math.Clamp(
                0.3 + Ruido(0.2),
                0,
                1);

            estado = "EN_SOLAR";
            fuente = "SOLAR";
        }
        else
        {
            vSol = Math.Max(
                0,
                0.2 + 3 * irradiancia + Ruido(0.05));

            iSol = 0;

            vRed = 5.05 + Ruido(0.02);

            iRed = Math.Clamp(
                150 + Ruido(25),
                150,
                200);

            estado = "EN_CARGADOR";
            fuente = "CARGADOR";
        }

        var pSol = vSol * iSol;
        var pRed = vRed * iRed;

        // Energía acumulada en Wh.
        _whSol +=
            pSol / 1000 * intervaloSegundos / 3600;

        _whRed +=
            pRed / 1000 * intervaloSegundos / 3600;

        // =================================================
        // S23-M5: EVENTO DE PRUEBA
        // =================================================

        // Generamos un evento de reinicio únicamente
        // en la primera medición del simulador.
        object[] eventos = _seq == 1
            ? new object[]
            {
                new
                {
                    seq = 1L,

                    ts = DateTime.UtcNow.ToString(
                        "O",
                        CultureInfo.InvariantCulture),

                    tipo = "reinicio",

                    estado = estado,

                    de = (string?)null,

                    a = (string?)null,

                    motivo = "arranque",

                    origen = "arranque",

                    orden_id = (long?)null
                }
            }
            : Array.Empty<object>();

        return new Telemetria
        {
            BootId = BootId,

            Seq = _seq,

            Ts = DateTime.UtcNow.ToString(
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture),

            TsValida = true,

            Modo = "AUTO",

            Estado = estado,

            FuenteSeleccionada = fuente,

            AlimentacionComprobada = true,

            Solar = new Fuente(
                Math.Round(vSol, 3),
                Math.Round(iSol, 1),
                Math.Round(pSol, 0),
                Math.Round(_whSol, 6)),

            Cargador = new Fuente(
                Math.Round(vRed, 3),
                Math.Round(iRed, 1),
                Math.Round(pRed, 0),
                Math.Round(_whRed, 6)),

            // Evento de arranque para probar S23-M5.
            Eventos = eventos,

            ResultadosOrdenes = []
        };
    }

    // =====================================================
    // SIMULACIÓN DE IRRADIANCIA SOLAR
    // =====================================================

    private double Irradiancia(int intervaloSegundos)
    {
        var hora =
            _horaFija ?? ObtenerHoraLocal();

        var curva =
            hora is > 6 and < 18
                ? Math.Sin(
                    Math.PI * (hora - 6) / 12)
                : 0;

        if (_nubeRestante > 0)
        {
            _nubeRestante -= intervaloSegundos;
        }
        else if (_azar.NextDouble() < 0.02)
        {
            _nubeRestante = _azar.Next(
                10,
                31);
        }

        var factorNube =
            _nubeRestante > 0 ? 0.1 : 1.0;

        return Math.Clamp(
            (curva + Ruido(0.03)) * factorNube,
            0,
            1);
    }

    private static double ObtenerHoraLocal()
    {
        var ahora = DateTime.Now;

        return ahora.Hour +
               ahora.Minute / 60.0 +
               ahora.Second / 3600.0;
    }

    private double Ruido(double amplitud)
    {
        return (_azar.NextDouble() * 2 - 1) *
               amplitud;
    }
}

// =====================================================
// CLASES DE DATOS
// =====================================================

internal sealed record Fuente(
    [property: JsonPropertyName("v")]
    double V,

    [property: JsonPropertyName("i_ma")]
    double IMa,

    [property: JsonPropertyName("p_mw")]
    double PMw,

    [property: JsonPropertyName("wh_acum")]
    double WhAcum);

internal sealed class Telemetria
{
    [JsonPropertyName("boot_id")]
    public long BootId { get; init; }

    [JsonPropertyName("seq")]
    public long Seq { get; init; }

    [JsonPropertyName("ts")]
    public string Ts { get; init; } = "";

    [JsonPropertyName("ts_valida")]
    public bool TsValida { get; init; }

    [JsonPropertyName("modo")]
    public string Modo { get; init; } = "";

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = "";

    [JsonPropertyName("fuente_seleccionada")]
    public string FuenteSeleccionada { get; init; } = "";

    [JsonPropertyName("alimentacion_comprobada")]
    public bool AlimentacionComprobada { get; init; }

    [JsonPropertyName("solar")]
    public Fuente Solar { get; init; } = default!;

    [JsonPropertyName("cargador")]
    public Fuente Cargador { get; init; } = default!;

    [JsonPropertyName("eventos")]
    public object[] Eventos { get; init; } = [];

    [JsonPropertyName("resultados_ordenes")]
    public object[] ResultadosOrdenes { get; init; } = [];
}