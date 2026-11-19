
using SAGE.Web.Servicios.Alertas;
using Xunit;

namespace SAGE.Tests;

public class EvaluadorAlertasTests
{
    private readonly EvaluadorAlertas _evaluador = new();

    // =====================================================
    // SOLAR INSUFICIENTE
    // =====================================================

    [Theory]
    [InlineData(4.0, "SOLAR", true)]
    [InlineData(4.49, "SOLAR", true)]
    [InlineData(4.50, "SOLAR", false)]
    [InlineData(5.0, "SOLAR", false)]
    [InlineData(0.2, "CARGADOR", false)]
    [InlineData(0.0, "CARGADOR", false)]
    [InlineData(4.0, "solar", true)]
    public void SolarInsuficiente_EvaluaFuenteYVoltaje(
        double voltaje,
        string fuente,
        bool esperado)
    {
        var resultado = _evaluador.SolarInsuficiente(
            (decimal)voltaje,
            fuente);

        Assert.Equal(esperado, resultado);
    }

    // =====================================================
    // RED SIN TENSIÓN
    // =====================================================

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(-0.1, true)]
    [InlineData(4.0, false)]
    [InlineData(5.05, false)]
    public void RedSinTension_EvaluaVoltaje(
        double voltaje,
        bool esperado)
    {
        var resultado = _evaluador.RedSinTension(
            (decimal)voltaje);

        Assert.Equal(esperado, resultado);
    }

    // =====================================================
    // NINGUNA FUENTE VÁLIDA
    // =====================================================

    [Theory]
    [InlineData(0.0, 0.0, true)]
    [InlineData(0.2, 0.0, true)]
    [InlineData(4.9, 0.0, false)]
    [InlineData(0.2, 5.05, false)]
    [InlineData(4.9, 5.05, false)]
    public void NingunaFuenteValida_EvaluaAmbasFuentes(
        double solar,
        double red,
        bool esperado)
    {
        var resultado = _evaluador.NingunaFuenteValida(
            (decimal)solar,
            (decimal)red);

        Assert.Equal(esperado, resultado);
    }

    // =====================================================
    // SIN CONEXIÓN
    // =====================================================

    [Fact]
    public void SinConexion_DespuesDe31Segundos()
    {
        var ahora = DateTime.UtcNow;

        var ultimoContacto = ahora.AddSeconds(-31);

        Assert.True(_evaluador.SinConexion(
            ultimoContacto,
            ahora));
    }

    [Fact]
    public void SinConexion_AntesDe30Segundos()
    {
        var ahora = DateTime.UtcNow;

        var ultimoContacto = ahora.AddSeconds(-20);

        Assert.False(_evaluador.SinConexion(
            ultimoContacto,
            ahora));
    }

    [Fact]
    public void SinConexion_Exactamente30Segundos()
    {
        var ahora = DateTime.UtcNow;

        var ultimoContacto = ahora.AddSeconds(-30);

        Assert.False(_evaluador.SinConexion(
            ultimoContacto,
            ahora));
    }

    [Fact]
    public void SinConexion_SinContactoPrevio()
    {
        Assert.False(_evaluador.SinConexion(
            null,
            DateTime.UtcNow));
    }
}
