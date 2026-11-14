using SAGE.Web.Data;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Tests;

public class UsuarioActualTests
{
    [Fact]
    public void PuedeVerEmpresa_es_false_para_Operador()
    {
        var empresas = new List<EmpresaOpcion>
        {
            new(1, "A"),
            new(2, "B")
        };

        Assert.False(UsuarioActual.PuedeVerEmpresa(esOperador: true, empresas, 1));
    }

    [Fact]
    public void PuedeVerEmpresa_es_false_si_no_esta_vinculada()
    {
        var empresas = new List<EmpresaOpcion> { new(1, "A") };
        Assert.False(UsuarioActual.PuedeVerEmpresa(esOperador: false, empresas, 99));
    }

    [Fact]
    public void PuedeVerEmpresa_es_true_si_esta_vinculada()
    {
        var empresas = new List<EmpresaOpcion> { new(1, "A") };
        Assert.True(UsuarioActual.PuedeVerEmpresa(esOperador: false, empresas, 1));
    }
}
