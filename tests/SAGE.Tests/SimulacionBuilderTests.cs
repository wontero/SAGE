using SAGE.Web.Data;
using SAGE.Web.Servicios.Simulacion;

namespace SAGE.Tests;

public class SimulacionBuilderTests
{
    [Fact]
    public void Caso_calculado_a_mano()
    {
        var empresa = new Empresa { Id = 1, Nombre = "Demo", Nit = "1" };
        var parametros = new ParametrosEconomicos
        {
            Version = 1,
            TarifaBsKwh = 0.90m,
            CostoInstalacionBsKwp = 8000m,
            GeneracionKwhKwpMes = 125m,
            MantenimientoBsAnio = 600m
        };

        var facturas = Enumerable.Range(1, 12)
            .Select(mes => new Factura
            {
                EmpresaId = 1,
                Anio = 2025,
                Mes = mes,
                ConsumoKwh = 1000m,
                MontoBs = 900m,
                Origen = OrigenFactura.Manual,
                RegistradaPorId = "x"
            })
            .ToList();

        var sim = new SimulacionBuilder()
            .ParaEmpresa(empresa)
            .ConConsumo(facturas)
            .ConParametros(parametros)
            .ConTarifa(0.90m)
            .ConPotenciaMixta(3m)
            .ConHorizonte(10)
            .Construir();

        Assert.Equal(8.0m, sim.KwpSolar);
        Assert.Equal(3m, sim.KwpMixto);
        Assert.Equal(10800m, sim.CostoAnualRedBs);
        Assert.Equal(108000m, sim.CostoAcumRedBs);
        Assert.Equal(64000m, sim.InversionSolarBs);
        Assert.Equal(600m, sim.CostoAnualSolarBs);
        Assert.Equal(70000m, sim.CostoAcumSolarBs);
        Assert.Equal(24000m, sim.InversionMixtoBs);
        // kWhRedAnual = max(0, 12000 - 12*125*3) = max(0, 12000-4500)=7500
        // CostoAnualMixto = 7500*0.9 + 600 = 6750+600=7350
        Assert.Equal(7350m, sim.CostoAnualMixtoBs);
        Assert.Equal(97500m, sim.CostoAcumMixtoBs);
        // Solar recuperación: ahorro=10800-600=10200; 64000/10200≈6.3
        Assert.Equal(6.3m, sim.RecuperacionSolarAnios);
        // Mixto: ahorro=10800-7350=3450; 24000/3450≈7.0
        Assert.Equal(7.0m, sim.RecuperacionMixtoAnios);
    }

    [Fact]
    public void Sin_facturas_lanza_mensaje()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SimulacionBuilder()
                .ParaEmpresa(new Empresa { Id = 1, Nombre = "A", Nit = "1" })
                .ConConsumo(Array.Empty<Factura>())
                .ConParametros(new ParametrosEconomicos { Version = 1, GeneracionKwhKwpMes = 125 })
                .ConPotenciaMixta(1)
                .ConHorizonte(10)
                .Construir());

        Assert.Contains("No hay consumo facturado", ex.Message);
    }

    [Fact]
    public void Kwp_mixto_invalido_lanza()
    {
        var facturas = new[]
        {
            new Factura { ConsumoKwh = 1000m, Anio = 2025, Mes = 1, MontoBs = 1, RegistradaPorId = "x" }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SimulacionBuilder()
                .ParaEmpresa(new Empresa { Id = 1, Nombre = "A", Nit = "1" })
                .ConConsumo(facturas)
                .ConParametros(new ParametrosEconomicos
                {
                    Version = 1,
                    GeneracionKwhKwpMes = 125m,
                    TarifaBsKwh = 0.9m,
                    CostoInstalacionBsKwp = 8000m,
                    MantenimientoBsAnio = 600m
                })
                .ConPotenciaMixta(8.0m)
                .ConHorizonte(10)
                .Construir());

        Assert.Contains("escenario Solar", ex.Message);
    }
}
