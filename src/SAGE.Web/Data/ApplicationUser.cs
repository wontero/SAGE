using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SAGE.Web.Data;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public int? EmpresaId { get; set; }

    public Empresa? Empresa { get; set; }
}
