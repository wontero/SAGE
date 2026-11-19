using System.Globalization;
using System.Text.Json;
using SAGE.Simulador;

// Simulador del dispositivo SAGE (S23-M11): se comporta como el firmware del ESP32,
// con la máquina de estados (patrón State) y las órdenes (patrón Command).
// Uso:
// dotnet run --project src/SAGE.Simulador -- --url http://localhost:5284 --token <TOKEN>
//     [--intervalo 1] [--hora 12]

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

var sync = new object();
var planta = new Planta(horaFija: opciones.HoraFija);
var controlador = new ControladorFuente();
var procesador = new ProcesadorOrdenes(controlador, ProcesadorOrdenes.RutaPara(opciones.Token));
var resultadosPendientes = new List<ResultadoOrden>();
var wifi = true;

// Arranque (diseño §4.4): boot_id = guardado + 1, parámetros guardados, relé en red.
var persistido = procesador.Arrancar();

controlador.EventoAgregado += e =>
    Console.WriteLine(
        $"  Evento {e.Seq}: {e.Tipo} | {e.De ?? "-"} -> {e.A ?? "-"} | {e.Estado} | " +
        $"{e.Motivo} | origen {e.Origen}" +
        (e.OrdenId is long id ? $" | orden {id}" : ""));

using var cliente = new Cliente(opciones.Url, opciones.Token, TimeSpan.FromSeconds(2));

Console.WriteLine($"SAGE.Simulador -> {cliente.Destino} (cada {opciones.IntervaloSegundos} s)");
Console.WriteLine(
    $"boot_id = {procesador.BootId} | modo guardado = {persistido.Modo} | " +
    $"parámetros v{controlador.ParametrosVersion} | última orden = {procesador.UltimaEjecutada}");
Console.WriteLine("Teclas: S sombra | R red | N noche | W Wi-Fi | Q salir");
Console.WriteLine();

lock (sync)
{
    controlador.Arrancar(DateTime.UtcNow, persistido.Modo, persistido.OrdenModoId);
}

// La planta y la máquina de estados avanzan cada 100 ms, sin depender del Wi-Fi.
var tareaPlanta = Task.Run(async () =>
{
    using var reloj = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
    var anterior = DateTime.UtcNow;

    try
    {
        while (await reloj.WaitForNextTickAsync(cts.Token))
        {
            var ahora = DateTime.UtcNow;
            var dt = (ahora - anterior).TotalSeconds;
            anterior = ahora;

            lock (sync)
            {
                var muestra = planta.Medir(controlador.ReleEnSolar, ahora, dt);
                controlador.Paso(muestra, ahora);
            }
        }
    }
    catch (OperationCanceledException)
    {
    }
});

var tareaTeclas = Task.Run(() => LeerTeclasAsync(cts.Token));

using var relojEnvio = new PeriodicTimer(TimeSpan.FromSeconds(opciones.IntervaloSegundos));

try
{
    while (await relojEnvio.WaitForNextTickAsync(cts.Token))
    {
        PaqueteTelemetria? paquete = null;
        bool conWifi;

        lock (sync)
        {
            // Hasta tener el primer promedio de 1 s no hay nada que informar.
            if (controlador.UltimasLecturas is null)
                continue;

            conWifi = wifi;

            if (conWifi)
                paquete = ConstruirPaquete();
        }

        if (paquete is null)
        {
            lock (sync)
                Console.WriteLine(LineaEstado(null, "Wi-Fi no", "HTTP -"));

            continue;
        }

        var respuesta = await cliente.EnviarAsync(paquete, cts.Token);

        lock (sync)
        {
            if (respuesta.Ok)
            {
                // Lo enviado ya está en el servidor: se saca de las colas.
                controlador.QuitarEventos(paquete.Eventos.Select(e => e.Seq));

                var enviados = paquete.ResultadosOrdenes.Select(r => r.OrdenId).ToHashSet();
                resultadosPendientes.RemoveAll(r => enviados.Contains(r.OrdenId));

                if (respuesta.Orden is JsonElement orden)
                    ProcesarOrden(orden);
            }

            var http = respuesta.Codigo is int c ? $"HTTP {c}" : $"ERROR {respuesta.Error}";
            Console.WriteLine(LineaEstado(paquete.Seq, "Wi-Fi sí", http));
        }
    }
}
catch (OperationCanceledException)
{
    // Salida normal con Q o Ctrl+C.
}

lock (sync)
    procesador.Guardar();

await Task.WhenAll(tareaPlanta, Task.WhenAny(tareaTeclas, Task.Delay(200)));

Console.WriteLine("Simulador detenido.");

return 0;

// =====================================================
// FUNCIONES LOCALES
// =====================================================

PaqueteTelemetria ConstruirPaquete()
{
    var l = controlador.UltimasLecturas ?? default;

    return new PaqueteTelemetria
    {
        BootId = procesador.BootId,
        Seq = controlador.SiguienteSeq(),
        Ts = PaqueteTelemetria.FormatoHora(DateTime.UtcNow),
        TsValida = true,
        Modo = controlador.Modo,
        Estado = controlador.Estado.Nombre,
        FuenteSeleccionada = controlador.FuenteSeleccionada,
        AlimentacionComprobada = controlador.AlimentacionComprobada,
        ParametrosVersion = controlador.ParametrosVersion,
        Solar = RamaTelemetria.Desde(l.VSol, l.ISolMa, planta.WhSol),
        Cargador = RamaTelemetria.Desde(l.VRed, l.IRedMa, planta.WhRed),
        Eventos = controlador.Eventos.Select(EventoTelemetria.Desde).ToList(),
        ResultadosOrdenes = resultadosPendientes.ToList()
    };
}

void ProcesarOrden(JsonElement orden)
{
    var id = orden.TryGetProperty("id", out var i) ? i.ToString() : "?";
    var tipo = orden.TryGetProperty("tipo", out var t) ? t.ToString() : "?";
    var valor = orden.TryGetProperty("valor", out var v) ? v.ToString() : "?";

    var resultado = procesador.Procesar(orden);

    if (resultado is null)
    {
        Console.WriteLine($"  Orden {id} ({tipo} {valor}): ya ejecutada, se ignora.");
        return;
    }

    resultadosPendientes.Add(resultado);

    Console.WriteLine(
        $"  Orden {id} ({tipo} {valor}): {resultado.Resultado}" +
        (resultado.Motivo is null ? "" : $" / {resultado.Motivo}"));
}

string LineaEstado(long? seq, string textoWifi, string http)
{
    var l = controlador.UltimasLecturas ?? default;
    var ci = CultureInfo.InvariantCulture;

    return string.Format(
        ci,
        "{0:HH:mm:ss} seq={1} modo={2} estado={3} fuente={4} | sol {5:F2} V {6:F0} mW | red {7:F2} V {8:F0} mW | {9} | {10}",
        DateTime.Now,
        seq?.ToString(ci) ?? "-",
        controlador.Modo,
        controlador.Estado.Nombre,
        controlador.FuenteSeleccionada,
        l.VSol, l.VSol * l.ISolMa,
        l.VRed, l.VRed * l.IRedMa,
        textoWifi,
        http);
}

void ProcesarTecla(char tecla)
{
    lock (sync)
    {
        switch (char.ToUpperInvariant(tecla))
        {
            case 'S':
                planta.Sombreado = !planta.Sombreado;
                Console.WriteLine(planta.Sombreado ? ">> Panel sombreado" : ">> Panel sin sombra");
                break;

            case 'R':
                planta.RedCortada = !planta.RedCortada;
                Console.WriteLine(planta.RedCortada ? ">> Red cortada (cargador a 0 V)" : ">> Red restablecida");
                break;

            case 'N':
                planta.NocheForzada = !planta.NocheForzada;
                Console.WriteLine(planta.NocheForzada ? ">> Noche forzada" : ">> Noche forzada desactivada");
                break;

            case 'W':
                wifi = !wifi;
                Console.WriteLine(wifi ? ">> Wi-Fi restablecido" : ">> Wi-Fi cortado (se acumulan eventos)");
                break;

            case 'Q':
                Console.WriteLine(">> Saliendo...");
                cts.Cancel();
                break;
        }
    }
}

async Task LeerTeclasAsync(CancellationToken ct)
{
    try
    {
        if (!Console.IsInputRedirected)
        {
            while (!ct.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                    ProcesarTecla(Console.ReadKey(intercept: true).KeyChar);
                else
                    await Task.Delay(50, ct);
            }

            return;
        }

        // Entrada redirigida (scripts de prueba): cada carácter de cada línea es una tecla.
        while (!ct.IsCancellationRequested)
        {
            var linea = await Console.In.ReadLineAsync(ct);

            if (linea is null)
                return;

            foreach (var tecla in linea)
                ProcesarTecla(tecla);
        }
    }
    catch (OperationCanceledException)
    {
    }
}

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
        var intervalo = 1;
        double? hora = null;

        for (var i = 0; i < args.Length; i++)
        {
            var valor = i + 1 < args.Length ? args[i + 1] : null;

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
                    if (!int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out intervalo) ||
                        intervalo < 1)
                    {
                        Console.WriteLine("--intervalo debe ser un entero de 1 o más segundos.");
                        return null;
                    }

                    i++;
                    break;

                case "--hora":
                    if (!double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var h) ||
                        h < 0 || h >= 24)
                    {
                        Console.WriteLine("--hora debe estar entre 0 y 24.");
                        return null;
                    }

                    hora = h;
                    i++;
                    break;

                default:
                    Console.WriteLine($"Parámetro desconocido: {args[i]}");
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
            return null;

        return new Opciones(url, token, intervalo, hora);
    }

    public static void MostrarUso()
    {
        Console.WriteLine("Simulador del dispositivo SAGE");
        Console.WriteLine();
        Console.WriteLine("Uso:");
        Console.WriteLine(
            "dotnet run --project src/SAGE.Simulador -- --url <URL> --token <TOKEN> " +
            "[--intervalo <segundos>] [--hora <0-24>]");
        Console.WriteLine();
        Console.WriteLine("--url        Dirección de la plataforma.");
        Console.WriteLine("--token      Token del dispositivo.");
        Console.WriteLine("--intervalo  Segundos entre envíos (por defecto 1).");
        Console.WriteLine("--hora       Hora simulada del día (fija la irradiancia).");
        Console.WriteLine();
        Console.WriteLine("Teclas: S sombra | R red | N noche | W Wi-Fi | Q salir");
    }
}
