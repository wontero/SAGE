using System.Text.Json;
using SAGE.Web.Servicios.Configuracion;
using Xunit;

namespace SAGE.Tests;

// S23-M10: límites de los parámetros de la máquina de estados (diseño §4.1).
public class ParametrosFuenteTests
{
    private static ParametrosFuente Base => ParametrosFuente.Predeterminados;

    [Fact]
    public void Predeterminados_son_validos()
    {
        Assert.Empty(Base.Validar());
    }

    [Theory]
    [InlineData(3.99, false)]
    [InlineData(4.0, true)]
    [InlineData(4.9, true)]
    [InlineData(4.91, false)]
    [InlineData(5.5, false)]
    public void VMin_entre_4_0_y_4_9(double vMin, bool valido)
    {
        // V_retorno al máximo para que solo se pruebe V_min.
        var p = Base with { VMin = (decimal)vMin, VRetorno = 5.3m };

        Assert.Equal(valido, !p.Validar().Any(e => e.StartsWith("V_min")));
    }

    [Theory]
    [InlineData(4.69, false)]   // VMin 4,50 + 0,2 = 4,70
    [InlineData(4.70, true)]
    [InlineData(5.3, true)]
    [InlineData(5.31, false)]
    public void VRetorno_desde_VMin_mas_0_2_hasta_5_3(double vRetorno, bool valido)
    {
        var p = Base with { VRetorno = (decimal)vRetorno };

        Assert.Equal(valido, p.Validar().Count == 0);
    }

    [Fact]
    public void VRetorno_depende_de_VMin()
    {
        // Con V_min = 4,9 el mínimo de V_retorno es 5,1.
        Assert.NotEmpty((Base with { VMin = 4.9m, VRetorno = 5.0m }).Validar());
        Assert.Empty((Base with { VMin = 4.9m, VRetorno = 5.1m }).Validar());
    }

    [Theory]
    [InlineData(4.9, false)]
    [InlineData(5, true)]
    [InlineData(100, true)]
    [InlineData(100.1, false)]
    public void IMin_entre_5_y_100(double iMin, bool valido)
    {
        Assert.Equal(valido, (Base with { IMinMa = (decimal)iMin }).Validar().Count == 0);
    }

    [Theory]
    [InlineData(0.4, false)]
    [InlineData(0.5, true)]
    [InlineData(5, true)]
    [InlineData(5.1, false)]
    public void TFalla_entre_0_5_y_5(double t, bool valido)
    {
        Assert.Equal(valido, (Base with { TFallaS = (decimal)t }).Validar().Count == 0);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(300, true)]
    [InlineData(301, false)]
    public void TEstable_entre_10_y_300(double t, bool valido)
    {
        Assert.Equal(valido, (Base with { TEstableS = (decimal)t }).Validar().Count == 0);
    }

    [Theory]
    [InlineData(1.9, false)]
    [InlineData(2, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void TPrueba_entre_2_y_30(double t, bool valido)
    {
        Assert.Equal(valido, (Base with { TPruebaS = (decimal)t }).Validar().Count == 0);
    }

    [Fact]
    public void Mensajes_en_espanol()
    {
        var errores = (Base with { VMin = 5.5m }).Validar();

        Assert.Contains(errores, e => e.Contains("V_min debe estar entre 4 y 4,9 V"));
    }

    [Fact]
    public void Json_de_la_orden_tiene_el_formato_de_la_seccion_4()
    {
        var p = Base with { Version = 4 };

        using var doc = JsonDocument.Parse(p.AJsonOrden());
        var r = doc.RootElement;

        Assert.Equal(4, r.GetProperty("version").GetInt32());
        Assert.Equal(4.50m, r.GetProperty("v_min").GetDecimal());
        Assert.Equal(4.85m, r.GetProperty("v_retorno").GetDecimal());
        Assert.Equal(20m, r.GetProperty("i_min_ma").GetDecimal());
        Assert.Equal(1.0m, r.GetProperty("t_falla_s").GetDecimal());
        Assert.Equal(30m, r.GetProperty("t_estable_s").GetDecimal());
        Assert.Equal(5m, r.GetProperty("t_prueba_s").GetDecimal());
    }

    [Fact]
    public void DesdeJson_vuelve_a_leer_lo_guardado()
    {
        var original = Base with { TEstableS = 15m, Version = 3 };

        var leido = ParametrosFuente.DesdeJson(original.AJsonOrden(), 3);

        Assert.Equal(original, leido);
    }

    [Fact]
    public void DesdeJson_null_son_los_predeterminados()
    {
        Assert.Equal(Base, ParametrosFuente.DesdeJson(null, 0));
    }
}
