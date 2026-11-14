using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Tests;

public class ReglasUsuariosTests
{
    [Fact]
    public void Administrador_solo_asigna_empresas_propias()
    {
        var propias = new[] { 1, 2 };
        Assert.True(ReglasUsuarios.AdministradorPuedeAsignarEmpresa(propias, 1));
        Assert.False(ReglasUsuarios.AdministradorPuedeAsignarEmpresa(propias, 9));
    }
}
