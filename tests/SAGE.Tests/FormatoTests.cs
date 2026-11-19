using SAGE.Web.Servicios;
using Xunit;

namespace SAGE.Tests;

public class FormatoTests
{
    [Fact]
    public void Potencia_MuestraWatts()
    {
        var resultado = Formato.Potencia(967000m);

        Assert.Equal("967,0 W", resultado);
    }

    [Fact]
    public void Potencia_ConFactorEscala_MuestraKilowatts()
    {
        var resultado = Formato.Potencia(967m, 2000m);

        Assert.Equal("1,93 kW", resultado);
    }

    [Fact]
    public void Energia_MuestraWh()
    {
        var resultado = Formato.Energia(25.5m);

        Assert.Equal("25,50 Wh", resultado);
    }

    [Fact]
    public void Energia_ConFactor_MuestraKwh()
    {
        var resultado = Formato.Energia(2m, 2000m);

        Assert.Equal("4,000 kWh", resultado);
    }

    [Fact]
    public void Tension_MantieneValorReal()
    {
        var resultado = Formato.Tension(4.93m);

        Assert.Equal("4,93 V", resultado);
    }

    [Fact]
    public void Corriente_MantieneValorReal()
    {
        var resultado = Formato.Corriente(196.2m);

        Assert.Equal("196,2 mA", resultado);
    }

    [Fact]
    public void HoraLocal_ConvierteHoraBolivia()
    {
        var utc = new DateTime(
            2026, 11, 19, 12, 0, 0,
            DateTimeKind.Utc);

        var resultado = Formato.HoraLocal(utc);

        Assert.Equal("19/11/2026 08:00:00", resultado);
    }

    [Fact]
    public void Hace_SinFecha_MuestraNunca()
    {
        Assert.Equal("Nunca", Formato.Hace(null));
    }

    [Fact]
    public void FuenteSolar_MuestraSolar()
    {
        Assert.Equal("Solar", Formato.Fuente("SOLAR"));
    }

    [Fact]
    public void FuenteCargador_MuestraRed()
    {
        Assert.Equal("Red", Formato.Fuente("CARGADOR"));
    }

    [Fact]
    public void ModoAuto_MuestraAutomatico()
    {
        Assert.Equal(
            "Automático",
            Formato.Modo("AUTO", "SOLAR"));
    }

    [Fact]
    public void ModoSolarConRespaldo_MuestraRespaldo()
    {
        Assert.Equal(
            "Solar en respaldo",
            Formato.Modo("SOLAR", "CARGADOR"));
    }
}