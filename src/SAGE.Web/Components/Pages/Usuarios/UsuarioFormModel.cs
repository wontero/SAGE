using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Components.Pages.Usuarios;

public class UsuarioFormModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo válido.")]
    [MaxLength(256, ErrorMessage = "El correo es demasiado largo.")]
    public string Correo { get; set; } = string.Empty;

    public string? ContrasenaInicial { get; set; }

    [Required(ErrorMessage = "Seleccioná un rol.")]
    public string Rol { get; set; } = "Tecnico";

    public int? EmpresaId { get; set; }
}
