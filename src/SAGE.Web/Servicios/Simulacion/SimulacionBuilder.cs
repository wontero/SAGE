using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Simulacion;

// Patrón: Builder — rol: constructor
public class SimulacionBuilder
{
    private Empresa? _empresa;
    private IEnumerable<Factura> _facturas = [];
    private ParametrosEconomicos? _parametros;
    private OrigenTarifaSimulacion _origenTarifa;
    private decimal _tarifaConfigurada;
    private decimal? _tarifaManual;
    private DatosEscenarioSolar? _solar;
    private DatosEscenarioSolar? _propuesta;
    private decimal _cobertura;
    private decimal _kwpPropuesta;
    private decimal? _presupuesto;
    private decimal? _metaKwhMes;
    private int _horizonte;

    public SimulacionBuilder ParaEmpresa(Empresa empresa)
    {
        _empresa = empresa;
        return this;
    }

    public SimulacionBuilder ConConsumo(IEnumerable<Factura> facturas)
    {
        _facturas = facturas;
        return this;
    }

    public SimulacionBuilder ConParametros(ParametrosEconomicos parametros)
    {
        _parametros = parametros;
        return this;
    }

    public SimulacionBuilder ConTarifa(OrigenTarifaSimulacion origen, decimal configurada, decimal? manual = null)
    {
        (_origenTarifa, _tarifaConfigurada, _tarifaManual) = (origen, configurada, manual);
        return this;
    }

    public SimulacionBuilder ConSolarSugerido(decimal cobertura, DatosEscenarioSolar datos)
    {
        (_cobertura, _solar) = (cobertura, datos);
        return this;
    }

    public SimulacionBuilder ConPropuesta(decimal kwp, DatosEscenarioSolar datos, decimal? presupuesto = null)
    {
        (_kwpPropuesta, _propuesta, _presupuesto) = (kwp, datos, presupuesto);
        _metaKwhMes = null;
        return this;
    }

    public SimulacionBuilder ConPropuestaPorKwh(decimal metaKwhMes, DatosEscenarioSolar datos)
    {
        ConPropuesta(0m, datos);
        _metaKwhMes = metaKwhMes;
        return this;
    }

    public static decimal PotenciaPorEnergia(decimal kwhMes, decimal produccion) =>
        decimal.Ceiling(kwhMes / produccion * 10m) / 10m;

    public SimulacionBuilder ConHorizonte(int anios)
    {
        _horizonte = anios;
        return this;
    }

    public static (decimal Consumo, decimal? Tarifa) ResumirFacturas(IReadOnlyCollection<Factura> facturas)
    {
        var totalKwh = facturas.Sum(f => f.ConsumoKwh);
        return totalKwh > 0m
            ? (totalKwh / facturas.Count, facturas.Sum(f => f.MontoBs) / totalKwh)
            : (0m, null);
    }

    public static (decimal PotenciaKwp, decimal InversionBs, decimal CostoRedMensualBs) CalcularReferencia(
        decimal consumo, decimal tarifaFacturas, ParametrosEconomicos parametros)
    {
        var kwp = PotenciaPorEnergia(consumo, parametros.GeneracionKwhKwpMes);
        return (kwp, kwp * parametros.CostoInstalacionBsKwp, CostoRedMensual(consumo, tarifaFacturas));
    }

    private static decimal CostoRedMensual(decimal consumo, decimal tarifa) => consumo * tarifa;

    public static SugerenciaPresupuesto SugerirPorPresupuesto(
        decimal presupuesto, decimal consumo, string instaladora, decimal bsPorKwp, decimal produccion)
    {
        if (consumo <= 0m)
            throw new InvalidOperationException("Cargá facturas con consumo para sugerir un sistema.");
        Validar(RangosSimulacion.Presupuesto(presupuesto));
        Validar(RangosSimulacion.Precio(bsPorKwp));
        Validar(RangosSimulacion.Produccion(produccion));

        var potenciaCompleta = PotenciaPorEnergia(consumo, produccion);
        var kwp = presupuesto >= potenciaCompleta * bsPorKwp
            ? potenciaCompleta
            : decimal.Floor(presupuesto / bsPorKwp * 10m) / 10m;
        Validar(RangosSimulacion.Potencia(kwp));
        var inversion = kwp * bsPorKwp;
        var cubierta = Math.Min(consumo, kwp * produccion);
        return new(presupuesto, instaladora, kwp, inversion, presupuesto - inversion,
            cubierta / consumo * 100m, cubierta, consumo - cubierta);
    }

    public Data.Simulacion Construir()
    {
        if (_empresa is null || _parametros is null)
            throw new InvalidOperationException("Faltan la empresa o los parámetros económicos para simular.");

        var facturas = _facturas.Where(f => f.EmpresaId == _empresa.Id)
            .OrderByDescending(f => f.Anio).ThenByDescending(f => f.Mes).Take(12).ToArray();
        var (consumo, tarifaFacturas) = ResumirFacturas(facturas);
        if (tarifaFacturas is not decimal tarifaReal)
            throw new InvalidOperationException("No hay consumo facturado: registrá al menos una factura.");

        if (_solar is null || _propuesta is null)
            throw new InvalidOperationException("Completá los datos de los dos escenarios solares.");

        Validar(RangosSimulacion.Cobertura(_cobertura));
        Validar(RangosSimulacion.Horizonte(_horizonte));

        var tarifa = _origenTarifa switch
        {
            OrigenTarifaSimulacion.Facturas => tarifaReal,
            OrigenTarifaSimulacion.Configurada => _tarifaConfigurada,
            OrigenTarifaSimulacion.Manual when _tarifaManual.HasValue => _tarifaManual.Value,
            _ => throw new InvalidOperationException("Elegí una tarifa válida para simular.")
        };
        Validar(RangosSimulacion.Tarifa(tarifa));
        ValidarEscenario(_solar);
        ValidarEscenario(_propuesta);

        var kwpPropuesta = _kwpPropuesta;
        if (_metaKwhMes is decimal meta)
        {
            Validar(RangosSimulacion.MetaKwh(meta));
            kwpPropuesta = PotenciaPorEnergia(meta, _propuesta.ProduccionKwhKwpMes);
        }
        Validar(RangosSimulacion.Potencia(kwpPropuesta));

        var sugerencia = _presupuesto is decimal presupuesto
            ? SugerirPorPresupuesto(presupuesto, consumo, _propuesta.Instaladora, _propuesta.BsPorKwp, _propuesta.ProduccionKwhKwpMes)
            : null;
        if (sugerencia is not null && (sugerencia.PotenciaKwp != kwpPropuesta || _propuesta.CotizacionBs.HasValue))
            throw new InvalidOperationException("Volvé a calcular la potencia con tu presupuesto.");

        var kwpSinRedondear = consumo * (_cobertura / 100m) / _solar.ProduccionKwhKwpMes;
        var kwpSolar = PotenciaPorEnergia(consumo * (_cobertura / 100m), _solar.ProduccionKwhKwpMes);
        if (RangosSimulacion.Potencia(kwpSolar) is not null)
            throw new InvalidOperationException("La potencia sugerida queda fuera del rango de 0,1 a 5.000 kWp. Ajustá la cobertura o la producción.");

        var costoAnualRed = 12m * CostoRedMensual(consumo, tarifa);
        var pasos = new List<string>
        {
            $"Consumo promedio = ({string.Join(" + ", facturas.Select(f => Numero(f.ConsumoKwh)))}) ÷ {facturas.Length} ≈ {Numero(consumo)} kWh/mes",
            $"Tarifa de tus facturas = {Numero(facturas.Sum(f => f.MontoBs))} Bs ÷ {Numero(facturas.Sum(f => f.ConsumoKwh))} kWh ≈ {Numero(tarifaReal)} Bs/kWh",
            $"Tarifa usada ({NombreTarifa(_origenTarifa)}) ≈ {Numero(tarifa)} Bs/kWh",
            $"Solo red — costo anual = {Numero(consumo)} × 12 × {Numero(tarifa)} ≈ {Numero(costoAnualRed)} Bs/año",
            $"Solo red — costo acumulado = {Numero(costoAnualRed)} × {_horizonte} ≈ {Numero(costoAnualRed * _horizonte)} Bs",
            $"Solar sugerido — kWp sugerido = {Numero(consumo)} × {Numero(_cobertura)} % ÷ {Numero(_solar.ProduccionKwhKwpMes)} ≈ {Numero(kwpSinRedondear)} kWp. Se redondea hacia arriba a {Numero(kwpSolar)} kWp."
        };

        var solar = CalcularSolar("Solar sugerido", _solar, kwpSolar, consumo, tarifa, costoAnualRed, pasos);
        if (_metaKwhMes is decimal metaKwh)
            pasos.Add($"Tu propuesta — {Numero(metaKwh)} kWh/mes ÷ {Numero(_propuesta.ProduccionKwhKwpMes)} kWh/kWp/mes = {Numero(kwpPropuesta)} kWp (redondeado hacia arriba a una décima).");
        if (sugerencia is not null)
            pasos.Add($"Tu propuesta — presupuesto ({sugerencia.Instaladora}) = {Numero(sugerencia.PresupuestoBs)} Bs ÷ {Numero(_propuesta.BsPorKwp)} Bs/kWp, redondeado hacia abajo a una décima y limitado a la potencia para cubrir todo el consumo: {Numero(sugerencia.PotenciaKwp)} kWp. Sobrante = {Numero(sugerencia.PresupuestoBs)} − {Numero(sugerencia.InversionBs)} = {Numero(sugerencia.SobranteBs)} Bs.");
        var propuesta = CalcularSolar("Tu propuesta", _propuesta, kwpPropuesta, consumo, tarifa, costoAnualRed, pasos);

        var advertencias = new List<string>();
        if (facturas.Length < 3)
            advertencias.Add("Menos de 3 meses de consumo: baja confiabilidad");
        else if (facturas.Length < 12)
            advertencias.Add("Menos de 12 meses de consumo: el resultado es orientativo");

        return new Data.Simulacion
        {
            EmpresaId = _empresa.Id,
            Fecha = DateTime.UtcNow,
            ParametrosVersion = _parametros.Version,
            TarifaUsadaBsKwh = tarifa,
            ConsumoMensualKwh = consumo,
            MesesBase = facturas.Length,
            KwpSolar = kwpSolar,
            KwpMixto = kwpPropuesta,
            HorizonteAnios = _horizonte,
            InversionSolarBs = solar.Inversion,
            InversionMixtoBs = propuesta.Inversion,
            CostoAnualRedBs = costoAnualRed,
            CostoAnualSolarBs = solar.CostoAnual,
            CostoAnualMixtoBs = propuesta.CostoAnual,
            CostoAcumRedBs = costoAnualRed * _horizonte,
            CostoAcumSolarBs = solar.Acumulado,
            CostoAcumMixtoBs = propuesta.Acumulado,
            RecuperacionSolarAnios = solar.Recuperacion,
            RecuperacionMixtoAnios = propuesta.Recuperacion,
            Advertencias = advertencias,
            Detalle = new(_origenTarifa, tarifa, _tarifaConfigurada, _cobertura, _solar with { MantenimientoBsAnio = solar.Mantenimiento },
                _propuesta with { MantenimientoBsAnio = propuesta.Mantenimiento },
                _presupuesto, solar.Excedente, propuesta.Excedente,
                Array.AsReadOnly(facturas.Select(f => new FacturaBaseSimulacion(f.Anio, f.Mes, f.ConsumoKwh, f.MontoBs)).ToArray()),
                pasos.AsReadOnly(), kwpPropuesta, _metaKwhMes.HasValue ? ModoPropuesta.KwhMes : ModoPropuesta.Kwp, _metaKwhMes, sugerencia)
        };
    }

    public static string NombreTarifa(OrigenTarifaSimulacion origen) => origen switch
    {
        OrigenTarifaSimulacion.Facturas => "Facturas",
        OrigenTarifaSimulacion.Configurada => "Configurada",
        OrigenTarifaSimulacion.Manual => "Manual",
        _ => throw new ArgumentOutOfRangeException(nameof(origen))
    };

    private static void ValidarEscenario(DatosEscenarioSolar datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Instaladora))
            throw new InvalidOperationException("Elegí una instaladora.");
        Validar(RangosSimulacion.Precio(datos.BsPorKwp));
        if (datos.MantenimientoBsAnio.HasValue)
            Validar(RangosSimulacion.Mantenimiento(datos.MantenimientoBsAnio));
        if (datos.MantenimientoBsKwpAnio.HasValue || !datos.MantenimientoBsAnio.HasValue)
            Validar(RangosSimulacion.MantenimientoPorKwp(datos.MantenimientoBsKwpAnio));
        Validar(RangosSimulacion.Produccion(datos.ProduccionKwhKwpMes));
        if (datos.CotizacionBs.HasValue)
            Validar(RangosSimulacion.Inversion(datos.CotizacionBs));
    }

    private static void Validar(string? error)
    {
        if (error is not null)
            throw new InvalidOperationException(error);
    }

    private (decimal Inversion, decimal CostoAnual, decimal Acumulado, decimal? Recuperacion, decimal Excedente, decimal Mantenimiento)
        CalcularSolar(string nombre, DatosEscenarioSolar datos, decimal kwp, decimal consumo,
            decimal tarifa, decimal costoAnualRed, List<string> pasos)
    {
        var inversion = datos.CotizacionBs ?? kwp * datos.BsPorKwp;
        var produccion = kwp * datos.ProduccionKwhKwpMes;
        var excedente = Math.Max(0m, produccion - consumo);
        var energiaRed = Math.Max(0m, consumo - produccion);
        var mantenimiento = datos.MantenimientoBsAnio ?? kwp * datos.MantenimientoBsKwpAnio!.Value;
        var costoAnual = 12m * energiaRed * tarifa + mantenimiento;
        var ahorroAnual = costoAnualRed - costoAnual;
        var acumulado = inversion + _horizonte * costoAnual;
        decimal? recuperacion = ahorroAnual > 0m && inversion / _horizonte <= ahorroAnual
            ? decimal.Round(inversion / ahorroAnual, 1) : null;

        var formulaInversion = datos.CotizacionBs.HasValue
            ? $"{Numero(inversion)} Bs (cotización ingresada)"
            : $"{Numero(kwp)} × {Numero(datos.BsPorKwp)} ≈ {Numero(inversion)} Bs";
        pasos.Add($"{nombre} — inversión ({datos.Instaladora}) = {formulaInversion}");
        pasos.Add($"{nombre} — producción = {Numero(kwp)} × {Numero(datos.ProduccionKwhKwpMes)} ≈ {Numero(produccion)} kWh/mes");
        pasos.Add($"{nombre} — energía que sigue viniendo de la red = máximo entre 0 y ({Numero(consumo)} − {Numero(produccion)}) ≈ {Numero(energiaRed)} kWh/mes");
        if (excedente > 0m)
            pasos.Add($"{nombre} — excedente = {Numero(produccion)} − {Numero(consumo)} ≈ {Numero(excedente)} kWh/mes × 0 Bs/kWh = 0 Bs/mes");
        pasos.Add(datos.MantenimientoBsAnio.HasValue
            ? $"{nombre} — Mantenimiento = {Numero(mantenimiento)} Bs/año (ingresado a mano)"
            : $"{nombre} — Mantenimiento = {Numero(kwp)} kWp × {Numero(datos.MantenimientoBsKwpAnio!.Value)} Bs = {Numero(mantenimiento)} Bs/año");
        pasos.Add($"{nombre} — costo anual = {Numero(energiaRed)} × 12 × {Numero(tarifa)} + {Numero(mantenimiento)} ≈ {Numero(costoAnual)} Bs/año");
        pasos.Add($"{nombre} — ahorro anual = {Numero(costoAnualRed)} − {Numero(costoAnual)} ≈ {Numero(ahorroAnual)} Bs/año");
        pasos.Add($"{nombre} — costo acumulado = {Numero(inversion)} + {Numero(costoAnual)} × {_horizonte} ≈ {Numero(acumulado)} Bs");
        pasos.Add(recuperacion is decimal anios
            ? $"{nombre} — recuperás la inversión en {Numero(inversion)} ÷ {Numero(ahorroAnual)} ≈ {Numero(anios)} años"
            : $"{nombre} — no se recupera dentro del horizonte de {_horizonte} años");

        return (inversion, costoAnual, acumulado, recuperacion, excedente, mantenimiento);
    }

    private static string Numero(decimal valor) => valor.ToString("#,0.####", FacturaReglas.Cultura);
}
