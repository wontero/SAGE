using SAGE.Web.Data;
using SAGE.Web.Servicios.Energia;

namespace SAGE.Tests;

public class CalculadoraEnergiaTests
{
    private static readonly DateTime Inicio = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UnArranqueUsaElUltimoAcumulado()
    {
        var resultado = CalculadoraEnergia.CalcularMuestras(
            [Medir(3, 1, 3m, 4m, "SOLAR"), Medir(1, 1, 1m, 2m, "CARGADOR"), Medir(0, 1, 0m, 1m, "SOLAR")],
            [], 1);

        Assert.Equal(new EnergiaPorFuente(3m, 4m, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), 1), resultado);
    }

    [Fact]
    public void ReinicioSumaUnaVezElAcumuladoDeCadaArranque()
    {
        var resultado = CalculadoraEnergia.CalcularMuestras(
            [Medir(3, 2, 2m, 1m, "SOLAR"), Medir(2, 2, 0m, 0m, "SOLAR"),
             Medir(1, 1, 5m, 6m, "CARGADOR"), Medir(0, 1, 4m, 3m, "CARGADOR")],
            [], 0);

        Assert.Equal(new EnergiaPorFuente(7m, 7m, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), 0), resultado);
    }

    [Fact]
    public void RestaLaReferenciaAnteriorDelMismoArranque()
    {
        var resultado = CalculadoraEnergia.CalcularMuestras(
            [Medir(3, 1, 9m, 14m, "SOLAR"), Medir(0, 1, 8m, 11m, "SOLAR")],
            [Medir(-1, 1, 7m, 10m, "SOLAR"), Medir(-2, 2, 80m, 90m, "SOLAR")], 0);

        Assert.Equal(new EnergiaPorFuente(2m, 4m, TimeSpan.FromSeconds(3), TimeSpan.Zero, 0), resultado);
    }

    [Fact]
    public void CorteDeTreintaSegundosSumaSoloCincoALaFuenteAnterior()
    {
        var resultado = CalculadoraEnergia.CalcularMuestras(
            [Medir(31, 1, 2m, 3m, "SOLAR"), Medir(30, 1, 1m, 3m, "CARGADOR"), Medir(0, 1, 0m, 2m, "SOLAR")],
            [], 0);

        Assert.Equal(TimeSpan.FromSeconds(5), resultado.TiempoSolar);
        Assert.Equal(TimeSpan.FromSeconds(1), resultado.TiempoRed);
    }

    private static Medicion Medir(int segundos, long boot, decimal solar, decimal red, string fuente) => new()
    {
        Ts = Inicio.AddSeconds(segundos),
        BootId = boot,
        WhSolAcum = solar,
        WhRedAcum = red,
        FuenteSeleccionada = fuente
    };
}
