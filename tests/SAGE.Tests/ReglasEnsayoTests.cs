using SAGE.Web.Servicios.Ensayos;
using Xunit;

namespace SAGE.Tests;

// S23-M12: reglas de los ensayos (superposición, fin posterior al inicio, inicio no futuro).
public class ReglasEnsayoTests
{
    // 14:00 UTC = 10:00 en Bolivia.
    private static readonly DateTime Ahora = new(2026, 11, 20, 14, 0, 0, DateTimeKind.Utc);

    private static readonly IntervaloEnsayo Existente =
        new(1, Ahora.AddHours(-2), Ahora.AddHours(-1));   // 08:00 a 09:00 en Bolivia

    [Fact]
    public void Ensayo_valido_sin_otros()
    {
        Assert.Null(ReglasEnsayo.Validar(Ahora.AddMinutes(-2), Ahora, Ahora, []));
    }

    [Fact]
    public void Fin_antes_o_igual_al_inicio_se_rechaza()
    {
        Assert.Equal("El fin debe ser posterior al inicio.",
            ReglasEnsayo.Validar(Ahora.AddMinutes(-5), Ahora.AddMinutes(-10), Ahora, []));

        Assert.Equal("El fin debe ser posterior al inicio.",
            ReglasEnsayo.Validar(Ahora.AddMinutes(-5), Ahora.AddMinutes(-5), Ahora, []));
    }

    [Fact]
    public void Inicio_futuro_se_rechaza()
    {
        Assert.Equal("El inicio no puede ser una fecha futura.",
            ReglasEnsayo.Validar(Ahora.AddMinutes(10), null, Ahora, []));
    }

    [Fact]
    public void Superpuesto_con_otro_ensayo_muestra_la_fecha_en_hora_de_Bolivia()
    {
        var error = ReglasEnsayo.Validar(
            Ahora.AddMinutes(-90), Ahora.AddMinutes(-30), Ahora, [Existente]);

        Assert.Equal("Se superpone con el ensayo del 20/11 08:00", error);
    }

    [Fact]
    public void Contiguo_no_se_superpone()
    {
        // Empieza justo cuando termina el otro.
        Assert.Null(ReglasEnsayo.Validar(Existente.Fin!.Value, Ahora, Ahora, [Existente]));
    }

    [Fact]
    public void En_curso_se_superpone_con_cualquiera_posterior()
    {
        var enCurso = new IntervaloEnsayo(2, Ahora.AddMinutes(-30), null);

        Assert.NotNull(ReglasEnsayo.Validar(Ahora.AddMinutes(-10), Ahora.AddMinutes(-5), Ahora, [enCurso]));
        Assert.Null(ReglasEnsayo.Validar(Ahora.AddMinutes(-50), Ahora.AddMinutes(-40), Ahora, [enCurso]));
    }

    [Fact]
    public void Nuevo_en_curso_se_superpone_con_uno_que_termina_despues()
    {
        Assert.NotNull(ReglasEnsayo.Validar(Ahora.AddMinutes(-90), null, Ahora, [Existente]));
    }

    [Fact]
    public void Al_finalizar_se_excluye_el_propio_ensayo()
    {
        var propio = new IntervaloEnsayo(5, Ahora.AddMinutes(-10), null);

        Assert.Null(ReglasEnsayo.Validar(propio.Inicio, Ahora, Ahora, [propio], idActual: 5));
    }

    [Theory]
    [InlineData(0, 10, 5, 15, true)]
    [InlineData(0, 10, 10, 20, false)]
    [InlineData(0, 10, 2, 8, true)]
    [InlineData(5, 15, 0, 10, true)]
    [InlineData(20, 30, 0, 10, false)]
    public void SeSuperponen_intervalos_cerrados(int a1, int a2, int b1, int b2, bool esperado)
    {
        Assert.Equal(esperado, ReglasEnsayo.SeSuperponen(
            Ahora.AddMinutes(a1), Ahora.AddMinutes(a2), Ahora.AddMinutes(b1), Ahora.AddMinutes(b2)));
    }
}
