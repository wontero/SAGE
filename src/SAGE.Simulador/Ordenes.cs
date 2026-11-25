using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAGE.Simulador;

// S23-M11: resultado que el dispositivo informa en "resultados_ordenes".
public sealed record ResultadoOrden(
    [property: JsonPropertyName("orden_id")] long OrdenId,
    [property: JsonPropertyName("resultado")] string Resultado,
    [property: JsonPropertyName("motivo")] string? Motivo)
{
    public static ResultadoOrden Ejecutada(long id) => new(id, "ejecutada", null);

    public static ResultadoOrden Rechazada(long id, string motivo) => new(id, "rechazada", motivo);
}

// Patrón: Command — rol: comando
public interface Comando
{
    long Id { get; }

    void Ejecutar(ControladorFuente c, Action<ResultadoOrden> informar);
}

// Patrón: Command — rol: comando concreto
public sealed class ComandoModo : Comando
{
    public ComandoModo(long id, string modo)
    {
        Id = id;
        Modo = modo;
    }

    public long Id { get; }

    public string Modo { get; }

    public void Ejecutar(ControladorFuente c, Action<ResultadoOrden> informar)
    {
        switch (Modo)
        {
            case ControladorFuente.ModoCargador:
                c.Modo = ControladorFuente.ModoCargador;
                c.OrdenModoId = Id;

                // Modo Red: relé en red siempre. Se ejecuta siempre.
                if (c.ReleEnSolar)
                    c.CambiarA(EstadosFuente.EnCargador, "orden_modo", "orden", Id);

                informar(ResultadoOrden.Ejecutada(Id));
                break;

            case ControladorFuente.ModoAuto:
                c.Modo = ControladorFuente.ModoAuto;
                c.OrdenModoId = Id;
                informar(ResultadoOrden.Ejecutada(Id));
                break;

            case ControladorFuente.ModoSolar:
                EjecutarSolar(c, informar);
                break;

            default:
                informar(ResultadoOrden.Rechazada(Id, "orden_desconocida"));
                break;
        }
    }

    private void EjecutarSolar(ControladorFuente c, Action<ResultadoOrden> informar)
    {
        // Ya está en solar: solo cambia el modo.
        if (c.ReleEnSolar)
        {
            c.Modo = ControladorFuente.ModoSolar;
            c.OrdenModoId = Id;
            informar(ResultadoOrden.Ejecutada(Id));
            return;
        }

        // Relé en red: la rama solar está en vacío. Si no llega a V_retorno, se rechaza.
        var vSolVacio = c.UltimasLecturas?.VSol ?? 0;

        if (vSolVacio < c.Parametros.VRetornoD)
        {
            informar(ResultadoOrden.Rechazada(Id, "solar_no_disponible"));
            return;
        }

        c.Modo = ControladorFuente.ModoSolar;
        c.OrdenModoId = Id;

        // Se informa "ejecutada" en cuanto el relé pasa a solar (sección 3.6).
        c.CambiarA(EstadosFuente.ProbandoSolar, "orden_modo", "orden", Id);
        informar(ResultadoOrden.Ejecutada(Id));
    }
}

// Patrón: Command — rol: comando concreto
public sealed class ComandoParametros : Comando
{
    public ComandoParametros(long id, Parametros? parametros, int version)
    {
        Id = id;
        Parametros = parametros;
        Version = version;
    }

    public long Id { get; }

    public Parametros? Parametros { get; }

    public int Version { get; }

    public void Ejecutar(ControladorFuente c, Action<ResultadoOrden> informar)
    {
        if (Parametros is null || !Parametros.EsValido || Version < 0)
        {
            informar(ResultadoOrden.Rechazada(Id, "parametros_invalidos"));
            return;
        }

        c.Parametros = Parametros;
        c.ParametrosVersion = Version;
        informar(ResultadoOrden.Ejecutada(Id));
    }
}

// S23-M11: lo que el simulador guarda entre reinicios (imita Preferences del ESP32).
public sealed class EstadoPersistido
{
    [JsonPropertyName("boot_id")]
    public long BootId { get; set; }

    [JsonPropertyName("ult_orden")]
    public long UltimaEjecutada { get; set; }

    [JsonPropertyName("modo")]
    public string Modo { get; set; } = ControladorFuente.ModoAuto;

    [JsonPropertyName("orden_modo")]
    public long? OrdenModoId { get; set; }

    [JsonPropertyName("param_ver")]
    public int ParametrosVersion { get; set; }

    [JsonPropertyName("parametros")]
    public Parametros? Parametros { get; set; }
}

// Convierte la "orden" de la respuesta en un Comando, con idempotencia por id.
// Patrón: Command — rol: invocador
public sealed class ProcesadorOrdenes
{
    private static readonly JsonSerializerOptions OpcionesJson = new() { WriteIndented = true };

    private readonly ControladorFuente _controlador;
    private readonly string? _rutaEstado;

    public ProcesadorOrdenes(ControladorFuente controlador, string? rutaEstado)
    {
        _controlador = controlador;
        _rutaEstado = rutaEstado;
    }

    public long UltimaEjecutada { get; private set; }

    public long BootId { get; private set; }

    // simulador_estado_<primeros 8 caracteres del SHA-256 del token>.json junto al ejecutable.
    public static string RutaPara(string token)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        return Path.Combine(
            AppContext.BaseDirectory,
            $"simulador_estado_{hash[..8].ToLowerInvariant()}.json");
    }

    // Lee el estado guardado, incrementa el boot_id, aplica parámetros y lo vuelve a guardar.
    public EstadoPersistido Arrancar()
    {
        // Sin estado guardado (token nuevo o archivo borrado) el boot_id arranca en la hora Unix:
        // si volviera a 1, la plataforma descartaría como repetidos los boot_id + seq de corridas anteriores.
        var estado = Leer() ?? new EstadoPersistido { BootId = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };

        estado.BootId++;
        BootId = estado.BootId;
        UltimaEjecutada = estado.UltimaEjecutada;

        if (estado.Parametros is { EsValido: true } p)
        {
            _controlador.Parametros = p;
            _controlador.ParametrosVersion = estado.ParametrosVersion;
        }

        Escribir(estado);

        return estado;
    }

    // Procesa la orden de la respuesta. Devuelve null si no había orden o ya se ejecutó.
    public ResultadoOrden? Procesar(JsonElement orden)
    {
        if (orden.ValueKind != JsonValueKind.Object ||
            !orden.TryGetProperty("id", out var idJson) ||
            !idJson.TryGetInt64(out var id))
        {
            return null;
        }

        // Idempotencia: una orden ya ejecutada (o más vieja) no se vuelve a ejecutar.
        if (id <= UltimaEjecutada)
            return null;

        var comando = CrearComando(id, orden);

        ResultadoOrden? resultado = null;

        if (comando is null)
            resultado = ResultadoOrden.Rechazada(id, "orden_desconocida");
        else
            comando.Ejecutar(_controlador, r => resultado = r);

        UltimaEjecutada = id;
        Guardar();

        return resultado;
    }

    public void Guardar()
    {
        Escribir(new EstadoPersistido
        {
            BootId = BootId,
            UltimaEjecutada = UltimaEjecutada,
            Modo = _controlador.Modo,
            OrdenModoId = _controlador.OrdenModoId,
            ParametrosVersion = _controlador.ParametrosVersion,
            Parametros = _controlador.Parametros
        });
    }

    private static Comando? CrearComando(long id, JsonElement orden)
    {
        if (!orden.TryGetProperty("tipo", out var tipoJson) ||
            tipoJson.ValueKind != JsonValueKind.String ||
            !orden.TryGetProperty("valor", out var valor))
        {
            return null;
        }

        switch (tipoJson.GetString())
        {
            case "modo":
                return valor.ValueKind == JsonValueKind.String
                    ? new ComandoModo(id, valor.GetString() ?? "")
                    : null;

            case "parametros":
                return LeerParametros(id, valor);

            default:
                return null;
        }
    }

    private static ComandoParametros LeerParametros(long id, JsonElement valor)
    {
        try
        {
            var p = new Parametros(
                valor.GetProperty("v_min").GetDecimal(),
                valor.GetProperty("v_retorno").GetDecimal(),
                valor.GetProperty("i_min_ma").GetDecimal(),
                valor.GetProperty("t_falla_s").GetDecimal(),
                valor.GetProperty("t_estable_s").GetDecimal(),
                valor.GetProperty("t_prueba_s").GetDecimal());

            return new ComandoParametros(id, p, valor.GetProperty("version").GetInt32());
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException)
        {
            // Faltan campos o tienen otro tipo: se rechaza como parametros_invalidos.
            return new ComandoParametros(id, null, 0);
        }
    }

    private EstadoPersistido? Leer()
    {
        if (_rutaEstado is null || !File.Exists(_rutaEstado))
            return null;

        try
        {
            return JsonSerializer.Deserialize<EstadoPersistido>(File.ReadAllText(_rutaEstado));
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return null;
        }
    }

    private void Escribir(EstadoPersistido estado)
    {
        if (_rutaEstado is null)
            return;

        try
        {
            File.WriteAllText(_rutaEstado, JsonSerializer.Serialize(estado, OpcionesJson));
        }
        catch (IOException)
        {
            // Si no se puede guardar, el simulador sigue funcionando.
        }
    }
}
