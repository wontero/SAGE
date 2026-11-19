namespace SAGE.Simulador;

// S23-M11: promedio del último segundo (10 muestras). Las decisiones nunca usan una muestra suelta.
public readonly record struct Lecturas(double VSol, double ISolMa, double VRed, double IRedMa)
{
    public static Lecturas Promedio(IReadOnlyCollection<Muestra> muestras)
    {
        if (muestras.Count == 0)
            return default;

        return new Lecturas(
            muestras.Average(m => m.VSol),
            muestras.Average(m => m.ISolMa),
            muestras.Average(m => m.VRed),
            muestras.Average(m => m.IRedMa));
    }
}

// Patrón: State — rol: estado
public abstract class EstadoFuente
{
    public abstract string Nombre { get; }

    // Posición del relé en este estado: true = NO (solar), false = NC (cargador, reposo seguro).
    public abstract bool ReleEnSolar { get; }

    public virtual void Entrar(ControladorFuente c)
    {
    }

    public abstract void Evaluar(ControladorFuente c, Lecturas l);

    public override string ToString() => Nombre;
}

// Una instancia de cada estado: no guardan datos propios (los tiempos viven en el contexto).
public static class EstadosFuente
{
    public static readonly EnCargador EnCargador = new();
    public static readonly ProbandoSolar ProbandoSolar = new();
    public static readonly EnSolar EnSolar = new();
    public static readonly SinAlimentacion SinAlimentacion = new();
}

// Patrón: State — rol: estado concreto
public sealed class EnCargador : EstadoFuente
{
    public override string Nombre => "EN_CARGADOR";

    public override bool ReleEnSolar => false;

    public override void Evaluar(ControladorFuente c, Lecturas l)
    {
        var p = c.Parametros;
        var cae = c.FallaCargador.Sostenido(l.VRed < p.VMinD, p.TFallaD, c.Ahora);

        // Modo Red: el usuario fijó la fuente. Si el cargador cae, alerta una vez y no cambia.
        if (c.Modo == ControladorFuente.ModoCargador)
        {
            if (cae)
                c.AlertarCargadorSinTension();
            else if (l.VRed >= p.VMinD)
                c.RearmarAlertaCargador();

            return;
        }

        // Cargador caído y solar disponible: se prueba solar sin esperar (tabla 4.2).
        if (cae && l.VSol >= p.VRetornoD)
        {
            c.CambiarA(EstadosFuente.ProbandoSolar, "cargador_sin_tension", "regla", null);
            return;
        }

        // Modo Solar en respaldo: no reintenta hasta una nueva orden (tabla 4.3).
        if (c.Modo == ControladorFuente.ModoSolar)
            return;

        var estable = c.Estable.Sostenido(l.VSol >= p.VRetornoD, p.TEstableD, c.Ahora);

        if (estable && c.EsperaVencida)
            c.CambiarA(EstadosFuente.ProbandoSolar, "intento_retorno_solar", "regla", null);
    }
}

// Patrón: State — rol: estado concreto
public sealed class ProbandoSolar : EstadoFuente
{
    public override string Nombre => "PROBANDO_SOLAR";

    public override bool ReleEnSolar => true;

    public override void Evaluar(ControladorFuente c, Lecturas l)
    {
        var p = c.Parametros;

        if (c.Falla.Sostenido(l.VSol < p.VMinD, p.TFallaD, c.Ahora))
        {
            if (c.Modo == ControladorFuente.ModoSolar)
            {
                // Orden Solar: la prueba falló; vuelve a red y queda "Solar en respaldo".
                c.CambiarA(EstadosFuente.EnCargador, "orden_fallida", "orden", c.OrdenModoId);
            }
            else
            {
                // Automático: duplica la espera antes del próximo intento.
                c.DuplicarEspera();
                c.CambiarA(EstadosFuente.EnCargador, "prueba_solar_fallida", "regla", null);
            }

            return;
        }

        if (c.Prueba.Sostenido(l.VSol >= p.VMinD, p.TPruebaD, c.Ahora))
            c.CambiarA(EstadosFuente.EnSolar, "solar_comprobada", "regla", null);
    }
}

// Patrón: State — rol: estado concreto
public sealed class EnSolar : EstadoFuente
{
    public override string Nombre => "EN_SOLAR";

    public override bool ReleEnSolar => true;

    public override void Evaluar(ControladorFuente c, Lecturas l)
    {
        var p = c.Parametros;

        if (c.Falla.Sostenido(l.VSol < p.VMinD, p.TFallaD, c.Ahora))
        {
            // En modo Solar queda en respaldo (EnCargador no reintenta en ese modo).
            c.CambiarA(EstadosFuente.EnCargador, "solar_insuficiente", "regla", null);
            return;
        }

        // 10 min seguidos en solar: la espera vuelve a 30 s.
        c.RevisarReinicioEspera();
    }
}

// Patrón: State — rol: estado concreto
public sealed class SinAlimentacion : EstadoFuente
{
    public override string Nombre => "SIN_ALIMENTACION";

    public override bool ReleEnSolar => false;

    public override void Evaluar(ControladorFuente c, Lecturas l)
    {
        var p = c.Parametros;

        // El cargador vuelve a ≥ V_min durante T_falla → EN_CARGADOR (sección 3.6).
        if (c.Recuperacion.Sostenido(l.VRed >= p.VMinD, p.TFallaD, c.Ahora))
        {
            c.CambiarA(EstadosFuente.EnCargador, "cargador_recuperado", "regla", null);
            return;
        }

        if (c.Modo == ControladorFuente.ModoCargador)
            return;

        // Cargador sigue mal pero solar ≥ V_retorno → PROBANDO_SOLAR (sección 3.6).
        // Se respeta la espera para no alternar sin fin cuando solar tampoco alcanza.
        var solarDisponible = c.Estable.Sostenido(l.VSol >= p.VRetornoD, p.TFallaD, c.Ahora);

        if (solarDisponible && c.EsperaVencida)
            c.CambiarA(EstadosFuente.ProbandoSolar, "cargador_sin_tension", "regla", null);
    }
}
