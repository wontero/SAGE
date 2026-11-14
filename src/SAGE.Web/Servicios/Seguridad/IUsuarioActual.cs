using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Seguridad;

public interface IUsuarioActual
{
    string? UsuarioId { get; }
    string NombreCompleto { get; }
    bool EsOperador { get; }
    bool EsAdministrador { get; }
    bool EsTecnico { get; }
    bool EsConsulta { get; }
    bool DebeCambiarContrasena { get; }
    IReadOnlyList<EmpresaOpcion> Empresas { get; }
    int? EmpresaActivaId { get; }
    EmpresaOpcion? EmpresaActiva { get; }
    bool EnRol(string rol);
    bool PuedeVerEmpresa(int empresaId);
    Task CargarAsync();
    Task SeleccionarEmpresaAsync(int empresaId);
    event Action? EmpresaCambiada;
}
