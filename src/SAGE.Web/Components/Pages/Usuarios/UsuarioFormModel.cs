using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Components.Pages.Usuarios;

public class UsuarioFormModel : IValidatableObject
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

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Rol != "Administrador" &&
            Rol != "Tecnico" &&
            Rol != "Consulta")
        {
            yield return new ValidationResult(
                "El rol seleccionado no es válido.",
                new[] { nameof(Rol) });
        }

        if ((Rol == "Tecnico" || Rol == "Consulta") &&
            (!EmpresaId.HasValue || EmpresaId.Value <= 0))
        {
            yield return new ValidationResult(
                "Seleccioná una empresa para este usuario.",
                new[] { nameof(EmpresaId) });
        }

        if (Id is null && string.IsNullOrWhiteSpace(ContrasenaInicial))
        {
            yield return new ValidationResult(
                "La contraseña inicial es obligatoria.",
                new[] { nameof(ContrasenaInicial) });
        }
    }
}