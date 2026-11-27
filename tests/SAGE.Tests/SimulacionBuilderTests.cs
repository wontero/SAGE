using SAGE.Web.Data;
using SAGE.Web.Servicios.Simulacion;

namespace SAGE.Tests;

public class SimulacionBuilderTests
{
    private static readonly DatosEscenarioSolar Referencia = new("Referencia SAGE", 8000m, null, 125m, MantenimientoBsKwpAnio: 80m);

    [Fact]
    public void Caso_calculado_a_mano()
    {
        var sim = Builder().Construir();

        Assert.Equal(8m, sim.KwpSolar);
        Assert.Equal(3m, sim.KwpMixto);
        Assert.Equal(10800m, sim.CostoAnualRedBs);
        Assert.Equal(108000m, sim.CostoAcumRedBs);
        Assert.Equal(64000m, sim.InversionSolarBs);
        Assert.Equal(sim.KwpSolar * 80m, sim.CostoAnualSolarBs);
        Assert.Equal(70400m, sim.CostoAcumSolarBs);
        Assert.Equal(24000m, sim.InversionMixtoBs);
        Assert.Equal(6990m, sim.CostoAnualMixtoBs);
        Assert.Equal(93900m, sim.CostoAcumMixtoBs);
        Assert.Equal(6.3m, sim.RecuperacionSolarAnios);
        Assert.Equal(6.3m, sim.RecuperacionMixtoAnios);
    }

    [Fact]
    public void Tarifa_ponderada_con_las_ultimas_doce_facturas_de_la_empresa()
    {
        var facturas = Facturas().Skip(1).Append(Factura(2026, 1, 2000m, 2500m))
            .Append(Factura(2025, 1, 100m, 500m, empresaId: 2))
            .Append(Factura(2024, 12, 100m, 500m)).Reverse();

        var sim = Builder().ConConsumo(facturas).Construir();

        Assert.Equal(12, sim.MesesBase);
        Assert.Equal(13000m / 12m, sim.ConsumoMensualKwh);
        Assert.Equal(12400m / 13000m, sim.TarifaUsadaBsKwh);
        var detalle = Assert.IsType<DetalleSimulacion>(sim.Detalle);
        Assert.Equal(OrigenTarifaSimulacion.Facturas, detalle.OrigenTarifa);
        Assert.Equal(new FacturaBaseSimulacion(2026, 1, 2000m, 2500m), detalle.Facturas[0]);
        Assert.Equal(new FacturaBaseSimulacion(2025, 2, 1000m, 900m), detalle.Facturas[^1]);
    }

    [Theory]
    [InlineData(OrigenTarifaSimulacion.Configurada, 9600)]
    [InlineData(OrigenTarifaSimulacion.Manual, 18000)]
    public void Usa_la_tarifa_elegida_en_los_tres_escenarios(OrigenTarifaSimulacion origen, int costoRed)
    {
        var sim = Builder().ConTarifa(origen, 0.80m, 1.50m).Construir();

        Assert.Equal(costoRed, sim.CostoAnualRedBs);
        Assert.Equal(sim.KwpSolar * 80m, sim.CostoAnualSolarBs);
        Assert.Equal(costoRed * 0.625m + 240m, sim.CostoAnualMixtoBs);
        Assert.Equal(origen, sim.Detalle!.OrigenTarifa);
    }

    [Fact]
    public void Cobertura_de_130_por_ciento_aumenta_la_potencia_y_redondea_hacia_arriba()
    {
        var sim = Builder().ConSolarSugerido(130m, Referencia with { ProduccionKwhKwpMes = 120m }).Construir();

        Assert.Equal(10.9m, sim.KwpSolar);
        Assert.Equal(308m, sim.Detalle!.ExcedenteSolarKwhMes);
        Assert.Equal(sim.KwpSolar * 80m, sim.CostoAnualSolarBs);
    }

    [Fact]
    public void Cobertura_parcial_del_sugerido_paga_la_energia_faltante()
    {
        var sim = Builder().ConSolarSugerido(50m, Referencia).Construir();

        Assert.Equal(4m, sim.KwpSolar);
        Assert.Equal(5720m, sim.CostoAnualSolarBs);
        Assert.Equal(0m, sim.Detalle!.ExcedenteSolarKwhMes);
    }

    [Fact]
    public void Propuesta_por_encima_del_consumo_acepta_excedente_con_valor_cero()
    {
        var sim = Builder().ConPropuesta(20m, Referencia with { MantenimientoBsAnio = 0m }).Construir();

        Assert.Equal(1500m, sim.Detalle!.ExcedentePropuestaKwhMes);
        Assert.Equal(0m, sim.CostoAnualMixtoBs);
        Assert.Equal(sim.CostoAnualRedBs, sim.CostoAnualRedBs - sim.CostoAnualMixtoBs);
        Assert.Contains(sim.Detalle.Pasos, paso => paso.Contains("1.500 kWh/mes × 0 Bs/kWh = 0 Bs/mes"));
        Assert.Null(sim.RecuperacionMixtoAnios);
    }

    [Fact]
    public void Cotizacion_manual_reemplaza_la_inversion_y_se_conservan_los_precios_ingresados()
    {
        var datos = new DatosEscenarioSolar("Chuvolteos", 7200m, 750m, 110m, CotizacionBs: 25320.79m);
        var sim = Builder().ConPropuesta(3m, datos).Construir();

        Assert.Equal(25320.79m, sim.InversionMixtoBs);
        Assert.Equal(datos, sim.Detalle!.Propuesta);
        Assert.Contains(sim.Detalle.Pasos, paso => paso.Contains("25.320,79 Bs (cotización ingresada)"));
    }

    [Fact]
    public void Presupuesto_redondea_hacia_abajo_y_se_guarda_si_se_aplico()
    {
        var datos = new DatosEscenarioSolar("Avatar", 8700m, 450m, 125m);
        var sugerencia = SimulacionBuilder.SugerirPorPresupuesto(29000m, 1000m, datos.Instaladora, datos.BsPorKwp, datos.ProduccionKwhKwpMes);
        var sim = Builder().ConPropuesta(sugerencia.PotenciaKwp, datos, 29000m).Construir();

        Assert.Equal(3.3m, sim.KwpMixto);
        Assert.Equal(28710m, sim.InversionMixtoBs);
        Assert.Equal(29000m, sim.Detalle!.PresupuestoBs);
        Assert.Equal(sugerencia, sim.Detalle.Sugerencia);
        Assert.Equal(290m, sugerencia.SobranteBs);
    }

    [Fact]
    public void Presupuesto_suficiente_se_limita_a_cubrir_todo_y_guarda_el_sobrante()
    {
        var sugerencia = SimulacionBuilder.SugerirPorPresupuesto(7000000m, 98450m, "Referencia SAGE", 8000m, 125m);
        var sim = Builder().ConConsumo([Factura(2026, 1, 98450m, 88605m)])
            .ConPropuesta(sugerencia.PotenciaKwp, Referencia, sugerencia.PresupuestoBs).Construir();

        Assert.Equal(787.6m, sugerencia.PotenciaKwp);
        Assert.Equal(6300800m, sugerencia.InversionBs);
        Assert.Equal(699200m, sugerencia.SobranteBs);
        Assert.Equal(100m, sugerencia.Cobertura);
        Assert.Equal(0m, sugerencia.EnergiaRedKwhMes);
        Assert.Equal(sugerencia, sim.Detalle!.Sugerencia);
        Assert.Contains(sim.Detalle.Pasos, paso => paso.Contains("699.200 Bs"));
    }

    [Fact]
    public void Presupuesto_parcial_calcula_la_cobertura_y_la_energia_restante()
    {
        var sugerencia = SimulacionBuilder.SugerirPorPresupuesto(2000000m, 98450m, "Referencia SAGE", 8000m, 125m);

        Assert.Equal(250m, sugerencia.PotenciaKwp);
        Assert.Equal(31250m, sugerencia.EnergiaCubiertaKwhMes);
        Assert.Equal(67200m, sugerencia.EnergiaRedKwhMes);
        Assert.Equal(31.742m, decimal.Round(sugerencia.Cobertura, 3));
    }

    [Fact]
    public void Sugerencia_rechaza_un_presupuesto_invalido()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => SimulacionBuilder.SugerirPorPresupuesto(0m, 98450m, "Referencia SAGE", 8000m, 125m));
        Assert.Contains("presupuesto", error.Message);
    }

    [Fact]
    public void Desglose_usa_los_mismos_numeros_que_el_resultado()
    {
        var sim = Builder().Construir();
        var pasos = Assert.IsType<DetalleSimulacion>(sim.Detalle).Pasos;

        Assert.Contains(pasos, paso => paso.Contains("10.800 Bs ÷ 12.000 kWh ≈ 0,9 Bs/kWh"));
        Assert.Contains(pasos, paso => paso.Contains("8 × 8.000 ≈ 64.000 Bs"));
        Assert.Contains(pasos, paso => paso.Contains("Tu propuesta — ahorro anual = 10.800 − 6.990 ≈ 3.810 Bs/año"));
        Assert.Contains(pasos, paso => paso.Contains("24.000 ÷ 3.810 ≈ 6,3 años"));
    }

    [Fact]
    public void Sin_facturas_lanza_mensaje()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Builder().ConConsumo([]).Construir());
        Assert.Contains("No hay consumo facturado", ex.Message);
    }

    [Theory]
    [InlineData("potencia", 0)]
    [InlineData("potencia", 6000)]
    [InlineData("precio", 1999)]
    [InlineData("precio", 30001)]
    [InlineData("mantenimiento", -1)]
    [InlineData("mantenimiento por kWp", 1001)]
    [InlineData("meta", 0)]
    [InlineData("meta", 1000001)]
    [InlineData("producción", 49)]
    [InlineData("producción", 201)]
    [InlineData("cobertura", 49)]
    [InlineData("cobertura", 301)]
    [InlineData("horizonte", 0)]
    [InlineData("horizonte", 26)]
    [InlineData("inversión", 0)]
    [InlineData("presupuesto", 0)]
    [InlineData("tarifa", 0)]
    [InlineData("tarifa", 6)]
    public void Rangos_invalidos_lanzan_error(string campo, int valor)
    {
        var builder = Builder();
        var invalido = campo switch
        {
            "potencia" => builder.ConPropuesta(valor, Referencia),
            "precio" => builder.ConSolarSugerido(100m, Referencia with { BsPorKwp = valor }),
            "mantenimiento" => builder.ConPropuesta(3m, Referencia with { MantenimientoBsAnio = valor }),
            "mantenimiento por kWp" => builder.ConPropuesta(3m, Referencia with { MantenimientoBsKwpAnio = valor }),
            "meta" => builder.ConPropuestaPorKwh(valor, Referencia),
            "producción" => builder.ConPropuesta(3m, Referencia with { ProduccionKwhKwpMes = valor }),
            "cobertura" => builder.ConSolarSugerido(valor, Referencia),
            "horizonte" => builder.ConHorizonte(valor),
            "inversión" => builder.ConPropuesta(3m, Referencia with { CotizacionBs = valor }),
            "presupuesto" => builder.ConPropuesta(3m, Referencia, valor),
            "tarifa" => builder.ConTarifa(OrigenTarifaSimulacion.Manual, 0.90m, valor),
            _ => throw new ArgumentOutOfRangeException(nameof(campo))
        };

        var ex = Assert.Throws<InvalidOperationException>(invalido.Construir);
        Assert.Contains(campo, ex.Message);
    }

    [Fact]
    public void Potencia_fuera_de_rango_por_consumo_o_presupuesto_se_rechaza()
    {
        Assert.Throws<InvalidOperationException>(() => Builder().ConConsumo([Factura(2025, 1, 700000m, 630000m)]).Construir());
        Assert.Throws<InvalidOperationException>(() => SimulacionBuilder.SugerirPorPresupuesto(500m, 1000m, "Referencia SAGE", 8000m, 125m));
        Assert.Throws<InvalidOperationException>(() => SimulacionBuilder.SugerirPorPresupuesto(48000000m, 700000m, "Referencia SAGE", 8000m, 125m));
    }

    [Theory]
    [InlineData(2500, 20)]
    [InlineData(2501, 20.1)]
    public void Meta_mensual_redondea_hacia_arriba_y_queda_guardada(int meta, decimal kwp)
    {
        var sim = Builder().ConPropuestaPorKwh(meta, Referencia).Construir();

        Assert.Equal(kwp, sim.KwpMixto);
        Assert.Equal(ModoPropuesta.KwhMes, sim.Detalle!.ModoPropuesta);
        Assert.Equal(meta, sim.Detalle.MetaKwhMes);
        Assert.Contains(sim.Detalle.Pasos, paso => paso.Contains("kWh/mes ÷ 125 kWh/kWp/mes"));
    }

    [Fact]
    public void Mantenimiento_se_calcula_por_potencia_o_respeta_el_importe_manual()
    {
        var auto = Builder().ConPropuesta(20m, Referencia).Construir();
        var manual = Builder().ConPropuesta(25m, Referencia with { MantenimientoBsAnio = 1234m }).Construir();

        Assert.Equal(1600m, auto.CostoAnualMixtoBs);
        Assert.Equal(1600m, auto.Detalle!.Propuesta.MantenimientoBsAnio);
        Assert.Contains(auto.Detalle.Pasos, paso => paso.Contains("20 kWp × 80 Bs = 1.600 Bs/año"));
        Assert.Equal(1234m, manual.CostoAnualMixtoBs);
        Assert.Contains(manual.Detalle!.Pasos, paso => paso.Contains("1.234 Bs/año (ingresado a mano)"));
    }

    [Fact]
    public void Admite_mil_kwp_y_la_meta_maxima_con_produccion_suficiente()
    {
        var sim = Builder().ConPropuesta(1000m, Referencia).Construir();
        var metaMaxima = Builder().ConPropuestaPorKwh(1000000m, Referencia with { ProduccionKwhKwpMes = 200m }).Construir();

        Assert.Equal(1000m, sim.KwpMixto);
        Assert.Equal(ModoPropuesta.Kwp, sim.Detalle!.ModoPropuesta);
        Assert.Null(sim.Detalle.MetaKwhMes);
        Assert.Equal(5000m, metaMaxima.KwpMixto);
        Assert.Throws<InvalidOperationException>(() => Builder().ConPropuestaPorKwh(1000000m, Referencia).Construir());
    }

    private static SimulacionBuilder Builder() => new SimulacionBuilder()
        .ParaEmpresa(new Empresa { Id = 1, Nombre = "Demo", Nit = "1" })
        .ConConsumo(Facturas())
        .ConParametros(new ParametrosEconomicos { Version = 1 })
        .ConTarifa(OrigenTarifaSimulacion.Facturas, 0.90m)
        .ConSolarSugerido(100m, Referencia)
        .ConPropuesta(3m, Referencia)
        .ConHorizonte(10);

    private static IEnumerable<Factura> Facturas() =>
        Enumerable.Range(1, 12).Select(mes => Factura(2025, mes, 1000m, 900m));

    private static Factura Factura(int anio, int mes, decimal kwh, decimal monto, int empresaId = 1) =>
        new() { EmpresaId = empresaId, Anio = anio, Mes = mes, ConsumoKwh = kwh, MontoBs = monto };
}
