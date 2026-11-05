using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Components.Pages.Empresas;

public class EmpresaFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El NIT es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El NIT no puede superar 20 caracteres.")]
    public string Nit { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El correo de contacto no es válido.")]
    [MaxLength(150)]
    public string? CorreoContacto { get; set; }
}
