using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

public class Empresa
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Nit { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? CorreoContacto { get; set; }

    public bool Activa { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Usuarios { get; set; } = new List<ApplicationUser>();
}
