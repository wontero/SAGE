using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Simulacion;

// Patrón: Builder — rol: constructor
public class SimulacionBuilder
{
    private Empresa? _empresa;
    private List<Factura> _facturas = new();
    private ParametrosEconomicos? _parametros;
    private decimal? _tarifaBsKwh;
    private decimal? _kwpMixto;
    private int? _horizonteAnios;

    public SimulacionBuilder ParaEmpresa(Empresa empresa)
    {
        _empresa = empresa;
        return this;
    }

    public SimulacionBuilder ConConsumo(IEnumerable<Factura> facturas)
    {
        _facturas = facturas
            .OrderByDescending(f => f.Anio)
            .ThenByDescending(f => f.Mes)
            .Take(12)
            .ToList();
        return this;
    }

    public SimulacionBuilder ConParametros(ParametrosEconomicos parametros)
    {
        _parametros = parametros;
        return this;
    }

    public SimulacionBuilder ConTarifa(decimal tarifaBsKwh)
    {
        _tarifaBsKwh = tarifaBsKwh;
        return this;
    }

    public SimulacionBuilder ConPotenciaMixta(decimal kwp)
    {
        _kwpMixto = kwp;
        return this;
    }

    public SimulacionBuilder ConHorizonte(int anios)
    {
        _horizonteAnios = anios;
        return this;
    }

    public Data.Simulacion Construir()
    {
        if (_empresa is null)
        {
            throw new InvalidOperationException("Falta la empresa para la simulación.");
        }

        if (_parametros is null)
        {
            throw new InvalidOperationException("Faltan los parámetros económicos.");
        }

        if (_facturas.Count == 0)
        {
            throw new InvalidOperationException("No hay consumo facturado: registre al menos una factura");
        }

        var n = _horizonteAnios ?? 10;
        if (n is < 1 or > 25)
        {
            throw new InvalidOperationException("El horizonte debe estar entre 1 y 25 años.");
        }

        var c = _facturas.Average(f => f.ConsumoKwh);
        var t = _tarifaBsKwh ?? _parametros.TarifaBsKwh;
        var g = _parametros.GeneracionKwhKwpMes;
        var k = _parametros.CostoInstalacionBsKwp;
        var m = _parametros.MantenimientoBsAnio;

        var kwpSolar = Math.Ceiling((c / g) * 10m) / 10m;
        var p = _kwpMixto ?? 3m;

        if (p <= 0 || p >= kwpSolar)
        {
            throw new InvalidOperationException("Para cubrir todo el consumo use el escenario Solar");
        }

        var costoAnualRed = 12m * c * t;
        var costoAcumRed = n * costoAnualRed;

        var inversionSolar = kwpSolar * k;
        var costoAnualSolar = m;
        var costoAcumSolar = inversionSolar + n * m;

        var kwhRedAnual = Math.Max(0m, 12m * c - 12m * g * p);
        var inversionMixto = p * k;
        var costoAnualMixto = kwhRedAnual * t + m;
        var costoAcumMixto = inversionMixto + n * costoAnualMixto;

        var advertencias = new List<string>();
        if (_facturas.Count < 3)
        {
            advertencias.Add("Menos de 3 meses de consumo: baja confiabilidad");
        }
        else if (_facturas.Count < 12)
        {
            advertencias.Add("Menos de 12 meses de consumo: el resultado es orientativo");
        }

        return new Data.Simulacion
        {
            EmpresaId = _empresa.Id,
            Fecha = DateTime.UtcNow,
            ParametrosVersion = _parametros.Version,
            TarifaUsadaBsKwh = t,
            ConsumoMensualKwh = decimal.Round(c, 4),
            MesesBase = _facturas.Count,
            KwpSolar = kwpSolar,
            KwpMixto = p,
            HorizonteAnios = n,
            InversionSolarBs = decimal.Round(inversionSolar, 4),
            InversionMixtoBs = decimal.Round(inversionMixto, 4),
            CostoAnualRedBs = decimal.Round(costoAnualRed, 4),
            CostoAnualSolarBs = decimal.Round(costoAnualSolar, 4),
            CostoAnualMixtoBs = decimal.Round(costoAnualMixto, 4),
            CostoAcumRedBs = decimal.Round(costoAcumRed, 4),
            CostoAcumSolarBs = decimal.Round(costoAcumSolar, 4),
            CostoAcumMixtoBs = decimal.Round(costoAcumMixto, 4),
            RecuperacionSolarAnios = CalcularRecuperacion(inversionSolar, costoAnualRed, costoAnualSolar, n),
            RecuperacionMixtoAnios = CalcularRecuperacion(inversionMixto, costoAnualRed, costoAnualMixto, n),
            Advertencias = advertencias
        };
    }

    private static decimal? CalcularRecuperacion(
        decimal inversion,
        decimal costoAnualRed,
        decimal costoAnualEscenario,
        int n)
    {
        var ahorroAnual = costoAnualRed - costoAnualEscenario;
        if (ahorroAnual <= 0)
        {
            return null;
        }

        var anios = inversion / ahorroAnual;
        if (anios > n)
        {
            return null;
        }

        return decimal.Round(anios, 1);
    }
}
