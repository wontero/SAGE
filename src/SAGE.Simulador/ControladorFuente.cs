using System.Globalization;
using System.Text.Json.Serialization;

namespace SAGE.Simulador;

// S23-M11: parámetros de la máquina de estados, con los límites del diseño §4.1.
public sealed record Parametros(
    decimal VMin,
    decimal VRetorno,
    decimal IMinMa,
    decimal TFallaS,
    decimal TEstableS,
    decimal TPruebaS)
{
    public static Parametros Predeterminados => new(4.50m, 4.85m, 20m, 1.0m, 30m, 5m);

    [JsonIgnore] public double VMinD => (double)VMin;
    [JsonIgnore] public double VRetornoD => (double)VRetorno;
    [JsonIgnore] public double IMinMaD => (double)IMinMa;
    [JsonIgnore] public double TFallaD => (double)TFallaS;
    [JsonIgnore] public double TEstableD => (double)TEstableS;
    [JsonIgnore] public double TPruebaD => (double)TPruebaS;

    // Mismos límites que la plataforma (S23-M10).
    [JsonIgnore]
    public bool EsValido =>
        VMin is >= 4.0m and <= 4.9m &&
        VRetorno >= VMin + 0.2m && VRetorno <= 5.3m &&
        IMinMa is >= 5m and <= 100m &&
        TFallaS is >= 0.5m and <= 5m &&
        TEstableS is >= 10m and <= 300m &&
        TPruebaS is >= 2m and <= 30m;
}

// S23-M11: "condición sostenida durante X segundos", con el reloj que recibe Tick.
public sealed class Temporizador
{
    private DateTime? _desde;

    public bool Sostenido(bool condicion, double segundos, DateTime ahora)
    {
        if (!condicion)
        {
            _desde = null;
            return false;
        }

        _desde ??= ahora;

        return (ahora - _desde.Value).TotalSeconds >= segundos - 1e-9;
    }

    public void Reiniciar() => _desde = null;
}

// S23-M11: evento que el dispositivo encola y envía en la telemetría (sección 4).
public sealed record EventoDispositivo(
    long Seq,
    DateTime Ts,
    string Tipo,
    string Estado,
    string? De,
    string? A,
    string Motivo,
    string Origen,
    long? OrdenId);

// S23-M11: el controlador de fuente del dispositivo (igual que el firmware).
// Patrón: State — rol: contexto
public sealed class ControladorFuente
{
    public const string ModoAuto = "AUTO";
    public const string ModoSolar = "SOLAR";
    public const string ModoCargador = "CARGADOR";

    public const string FuenteSolar = "SOLAR";
    public const string FuenteCargador = "CARGADOR";

    public const int MuestrasPorSegundo = 10;
    public const int MaximoEventos = 200;
    public static readonly TimeSpan EsperaArranque = TimeSpan.FromSeconds(3);

    // Espera tras fallo: 30 → 60 → 120 → 300 s, con tope de 600 s.
    public static readonly int[] EsperasS = [30, 60, 120, 300, 600];
    public static readonly TimeSpan EstableParaReiniciarEspera = TimeSpan.FromMinutes(10);

    private readonly Queue<Muestra> _muestras = new();
    private readonly List<EventoDispositivo> _eventos = new();

    private long _seq;
    private int _indiceEspera;
    private DateTime _ultimaSalidaSolar;
    private DateTime? _enSolarDesde;
    private bool _alertaCargadorEmitida;
    private DateTime? _arranqueHasta;

    public ControladorFuente(Parametros? parametros = null, int parametrosVersion = 0)
    {
        Parametros = parametros ?? Parametros.Predeterminados;
        ParametrosVersion = parametrosVersion;
    }

    // Se llama con cada evento nuevo (la consola lo imprime).
    public event Action<EventoDispositivo>? EventoAgregado;

    public EstadoFuente Estado { get; private set; } = EstadosFuente.EnCargador;

    public string Modo { get; set; } = ModoAuto;

    // Orden que fijó el modo actual (para el evento orden_fallida).
    public long? OrdenModoId { get; set; }

    public bool ReleEnSolar { get; private set; }

    public string FuenteSeleccionada => ReleEnSolar ? FuenteSolar : FuenteCargador;

    public Parametros Parametros { get; set; }

    public int ParametrosVersion { get; set; }

    public DateTime Ahora { get; private set; }

    public Lecturas? UltimasLecturas { get; private set; }

    public bool Arrancando => _arranqueHasta is not null;

    public int EsperaActualS => EsperasS[_indiceEspera];

    public bool EsperaVencida =>
        (Ahora - _ultimaSalidaSolar).TotalSeconds >= EsperaActualS;

    // Temporizadores (viven en el contexto para que los estados sean instancias únicas).
    public Temporizador Falla { get; } = new();
    public Temporizador Estable { get; } = new();
    public Temporizador Prueba { get; } = new();
    public Temporizador FallaCargador { get; } = new();
    public Temporizador Recuperacion { get; } = new();
    public Temporizador SinFuente { get; } = new();

    public IReadOnlyList<EventoDispositivo> Eventos => _eventos;

    // "Comprobada" (diseño §1.4): la rama seleccionada entrega V ≥ V_min e I > I_min.
    public bool AlimentacionComprobada
    {
        get
        {
            if (UltimasLecturas is not Lecturas l)
                return false;

            var (v, i) = ReleEnSolar ? (l.VSol, l.ISolMa) : (l.VRed, l.IRedMa);

            return v >= Parametros.VMinD && i > Parametros.IMinMaD;
        }
    }

    public long SiguienteSeq() => ++_seq;

    // =====================================================
    // ARRANQUE (diseño §4.4)
    // =====================================================

    // Relé en red, evento reinicio/arranque y espera de 3 s antes de aplicar el modo guardado.
    public void Arrancar(DateTime ahora, string modoGuardado, long? ordenModoId)
    {
        Ahora = ahora;
        Estado = EstadosFuente.EnCargador;
        ReleEnSolar = false;
        _muestras.Clear();
        _ultimaSalidaSolar = ahora;
        _enSolarDesde = null;
        _indiceEspera = 0;
        ReiniciarTemporizadores();

        Modo = modoGuardado is ModoAuto or ModoSolar or ModoCargador ? modoGuardado : ModoAuto;
        OrdenModoId = ordenModoId;
        _arranqueHasta = ahora + EsperaArranque;

        Encolar("reinicio", Estado.Nombre, null, FuenteCargador, "arranque", "arranque", null);
    }

    // =====================================================
    // MUESTREO Y EVALUACIÓN
    // =====================================================

    // Un paso de 100 ms: agrega la muestra y evalúa.
    public void Paso(Muestra muestra, DateTime ahora)
    {
        AgregarMuestra(muestra);
        Tick(ahora);
    }

    public void AgregarMuestra(Muestra muestra)
    {
        _muestras.Enqueue(muestra);

        while (_muestras.Count > MuestrasPorSegundo)
            _muestras.Dequeue();
    }

    // Evalúa primero la regla global (ambas ramas < V_min) y luego delega en el estado actual.
    public void Tick(DateTime ahora)
    {
        Ahora = ahora;

        // Tras mover el relé se espera 1 s completo de muestras nuevas.
        if (_muestras.Count < MuestrasPorSegundo)
            return;

        var l = Lecturas.Promedio(_muestras);
        UltimasLecturas = l;

        if (_arranqueHasta is DateTime hasta)
        {
            if (ahora < hasta)
                return;

            _arranqueHasta = null;

            // El modo Solar guardado vuelve a pasar por su comprobación previa.
            if (Modo == ModoSolar && l.VSol >= Parametros.VRetornoD)
                CambiarA(EstadosFuente.ProbandoSolar, "orden_modo", "orden", OrdenModoId);

            return;
        }

        var p = Parametros;
        var sinFuente = l.VSol < p.VMinD && l.VRed < p.VMinD;

        if (Estado != EstadosFuente.SinAlimentacion &&
            SinFuente.Sostenido(sinFuente, p.TFallaD, ahora))
        {
            // Si estaba probando solar, cuenta como prueba fallida (evita alternar sin fin).
            if (Estado == EstadosFuente.ProbandoSolar && Modo != ModoSolar)
                DuplicarEspera();

            CambiarA(EstadosFuente.SinAlimentacion, "sin_fuente_valida", "regla", null);
            return;
        }

        Estado.Evaluar(this, l);
    }

    // =====================================================
    // CAMBIO DE ESTADO
    // =====================================================

    // Todo cambio de estado pasa por aquí: mueve el relé, reinicia temporizadores,
    // encola el evento (tipo según la tabla de la sección 4) y llama a Entrar.
    public void CambiarA(EstadoFuente nuevo, string motivo, string origen, long? ordenId)
    {
        var releAntes = ReleEnSolar;
        var fuenteAntes = FuenteSeleccionada;

        Estado = nuevo;
        ReleEnSolar = nuevo.ReleEnSolar;
        ReiniciarTemporizadores();

        var movio = ReleEnSolar != releAntes;

        if (movio)
        {
            // Las muestras anteriores son de la otra posición del relé.
            _muestras.Clear();

            if (!ReleEnSolar)
                _ultimaSalidaSolar = Ahora;
        }

        _enSolarDesde = nuevo == EstadosFuente.EnSolar ? Ahora : null;

        if (nuevo != EstadosFuente.EnCargador)
            _alertaCargadorEmitida = false;

        Encolar(
            movio ? "conmutacion" : "estado",
            nuevo.Nombre,
            movio ? fuenteAntes : null,
            movio ? FuenteSeleccionada : null,
            motivo,
            origen,
            ordenId);

        nuevo.Entrar(this);
    }

    // =====================================================
    // ESPERA TRAS FALLO
    // =====================================================

    public void DuplicarEspera()
    {
        _indiceEspera = Math.Min(_indiceEspera + 1, EsperasS.Length - 1);
    }

    public void RevisarReinicioEspera()
    {
        if (_enSolarDesde is DateTime desde &&
            Ahora - desde >= EstableParaReiniciarEspera)
        {
            _indiceEspera = 0;
        }
    }

    // =====================================================
    // ALERTA EN MODO RED
    // =====================================================

    public void AlertarCargadorSinTension()
    {
        if (_alertaCargadorEmitida)
            return;

        _alertaCargadorEmitida = true;
        Encolar("alerta", Estado.Nombre, null, null, "cargador_sin_tension", "regla", null);
    }

    public void RearmarAlertaCargador() => _alertaCargadorEmitida = false;

    // =====================================================
    // COLA DE EVENTOS (máximo 200; se descartan los más viejos)
    // =====================================================

    public void QuitarEventos(IEnumerable<long> seqsEnviados)
    {
        var enviados = seqsEnviados.ToHashSet();
        _eventos.RemoveAll(e => enviados.Contains(e.Seq));
    }

    private void Encolar(
        string tipo, string estado, string? de, string? a,
        string motivo, string origen, long? ordenId)
    {
        var evento = new EventoDispositivo(
            SiguienteSeq(), Ahora, tipo, estado, de, a, motivo, origen, ordenId);

        _eventos.Add(evento);

        if (_eventos.Count > MaximoEventos)
            _eventos.RemoveRange(0, _eventos.Count - MaximoEventos);

        EventoAgregado?.Invoke(evento);
    }

    private void ReiniciarTemporizadores()
    {
        Falla.Reiniciar();
        Estable.Reiniciar();
        Prueba.Reiniciar();
        FallaCargador.Reiniciar();
        Recuperacion.Reiniciar();
        SinFuente.Reiniciar();
    }
}
