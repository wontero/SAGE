using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

public class TarifaEmpresa
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public Empresa Empresa { get; set; } = null!;

    public decimal TarifaBsKwh { get; set; }

    public DateTime VigenteDesde { get; set; }

    [Required]
    public string CreadoPorId { get; set; } = string.Empty;

    public ApplicationUser CreadoPor { get; set; } = null!;
}
