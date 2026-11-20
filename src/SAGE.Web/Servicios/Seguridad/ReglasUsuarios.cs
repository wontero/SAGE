using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Seguridad;

public enum AccionUsuario { RestablecerContrasena, Editar, CambiarEstado }

public static class ReglasUsuarios
{
    public static bool AdministradorPuedeAsignarEmpresa(
        IReadOnlyCollection<int> empresasDelAdministrador,
        int empresaId)
        => empresasDelAdministrador.Contains(empresaId);

    public static async Task<string?> ErrorDeGestionAsync(ApplicationDbContext db, IUsuarioActual gestor,
        string destinoId, ICollection<string> rolesDestino, AccionUsuario accion)
    {
        if (gestor.UsuarioId is null || gestor.DebeCambiarContrasena ||
            (!gestor.EsOperador && !gestor.EsAdministrador))
            return "No tenés permiso para modificar usuarios.";

        var esPropio = gestor.UsuarioId == destinoId;
        var esAdministrador = rolesDestino.Contains(DatosIniciales.RolAdministrador);
        var esOperador = rolesDestino.Contains(DatosIniciales.RolOperador);

        if (esPropio && accion is AccionUsuario.Editar)
            return "No podés editar tu propia cuenta desde aquí.";
        if (esPropio && accion is AccionUsuario.CambiarEstado)
            return "No podés desactivar tu propia cuenta.";

        if (gestor.EsOperador)
            return esAdministrador && !esOperador && accion is not AccionUsuario.Editar
                ? null : "El Operador solo puede gestionar administradores.";

        if (gestor.EmpresaActivaId is not int empresaId || !gestor.PuedeVerEmpresa(empresaId) ||
            !await db.UsuarioEmpresas.AnyAsync(ue => ue.UsuarioId == destinoId && ue.EmpresaId == empresaId) ||
            gestor.EmpresaActivaId != empresaId)
            return "El usuario no pertenece a su empresa activa.";

        if (esOperador || (esAdministrador && !esPropio))
            return "Este usuario lo gestiona SAGE.";

        return rolesDestino.Contains(DatosIniciales.RolTecnico) || rolesDestino.Contains(DatosIniciales.RolConsulta) || esPropio
            ? null : "Solo podés gestionar usuarios con rol Técnico o Consulta.";
    }
}
