namespace SAGE.Web.Servicios.Seguridad;

public static class ReglasUsuarios
{
    public static bool AdministradorPuedeAsignarEmpresa(
        IReadOnlyCollection<int> empresasDelAdministrador,
        int empresaId)
        => empresasDelAdministrador.Contains(empresaId);
}
