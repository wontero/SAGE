using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

public class UsuarioEmpresa
{
    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    public int EmpresaId { get; set; }

    public DateTime FechaVinculo { get; set; } = DateTime.UtcNow;

    public ApplicationUser Usuario { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;
}
