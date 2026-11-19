namespace SAGE.Simulador;

// S23-M11: una muestra de los dos INA219 (10 por segundo).
public readonly record struct Muestra(double VSol, double ISolMa, double VRed, double IRedMa);

// S23-M11: el mundo físico simulado (panel, cargador y lámpara). Avanza cada 100 ms.
public sealed class Planta
{
    // Sombreado: la irradiancia se multiplica por este factor.
    // La guía dice × 0,1, pero al mediodía eso deja 0,1 > 0,05 y el panel sombreado
    // seguiría marcando 5 V en vacío, con lo que una orden Solar con sombra no se
    // rechazaría (lo contrario de lo que pide S23-M11 y la demo). Con × 0,05 la
    // irradiancia sombreada queda siempre ≤ 0,05 (0,3 V en vacío). Ver Bitácora.
    public const double FactorSombra = 0.05;

    public const double UmbralVacio = 0.05;     // irradiancia mínima para 5 V en vacío
    public const double UmbralCarga = 0.25;     // irradiancia mínima para sostener la carga
    public const double VSolVacio = 5.00;
    public const double VSolSinLuz = 0.3;
    public const double VSolConCarga = 4.90;
    public const double VSolCargaInsuficiente = 3.80;
    public const double VRedNormal = 5.05;
    public const double VMinimaCarga = 4.5;     // la lámpara solo consume con ≥ 4,5 V
    public const double CorrienteCarga = 190;   // mA
    public const double CorrienteReposo = 0.4;  // mA en la rama no seleccionada

    private readonly Random? _azar;

    public Planta(bool determinista = false, double? horaFija = null, int? semilla = null)
    {
        _azar = determinista
            ? null
            : semilla is int s ? new Random(s) : new Random();

        HoraFija = horaFija;
    }

    public double? HoraFija { get; set; }

    // Para pruebas: fija la irradiancia (0 a 1) sin depender de la hora.
    public double? IrradianciaFija { get; set; }

    public bool Sombreado { get; set; }

    public bool RedCortada { get; set; }

    public bool NocheForzada { get; set; }

    public double WhSol { get; private set; }

    public double WhRed { get; private set; }

    public double Irradiancia(DateTime ahoraUtc)
    {
        if (NocheForzada)
            return 0;

        var irradiancia = IrradianciaFija ?? Curva(HoraFija ?? HoraLocal(ahoraUtc));

        if (Sombreado)
            irradiancia *= FactorSombra;

        return Math.Clamp(irradiancia, 0, 1);
    }

    // Una muestra de ambas ramas según la posición del relé. Acumula los Wh del intervalo.
    public Muestra Medir(bool releEnSolar, DateTime ahoraUtc, double dtSegundos)
    {
        var irradiancia = Irradiancia(ahoraUtc);

        // Rama solar: en vacío parece que hay sol aunque no alcance para la carga.
        var vSol = releEnSolar
            ? (irradiancia >= UmbralCarga ? VSolConCarga : VSolCargaInsuficiente)
            : (irradiancia > UmbralVacio ? VSolVacio : VSolSinLuz);

        vSol = Math.Max(0, vSol + Ruido(0.02));

        var vRed = RedCortada ? 0 : Math.Max(0, VRedNormal + Ruido(0.02));

        var iSol = releEnSolar ? Carga(vSol) : CorrienteReposo;
        var iRed = releEnSolar ? CorrienteReposo : Carga(vRed);

        // Wh acumulados: nunca bajan dentro de un boot_id.
        WhSol += vSol * iSol / 1000.0 * dtSegundos / 3600.0;
        WhRed += vRed * iRed / 1000.0 * dtSegundos / 3600.0;

        return new Muestra(vSol, iSol, vRed, iRed);
    }

    private double Carga(double v) =>
        v >= VMinimaCarga ? Math.Max(0, CorrienteCarga + Ruido(5)) : 0;

    private static double Curva(double hora) =>
        hora is > 6 and < 18 ? Math.Sin(Math.PI * (hora - 6) / 12) : 0;

    private static double HoraLocal(DateTime ahoraUtc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(ahoraUtc, DateTimeKind.Utc),
            TimeZoneInfo.Local);

        return local.Hour + local.Minute / 60.0 + local.Second / 3600.0;
    }

    private double Ruido(double amplitud) =>
        _azar is null ? 0 : (_azar.NextDouble() * 2 - 1) * amplitud;
}
