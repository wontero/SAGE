using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

public class ParametrosEconomicos
{
    public int Id { get; set; }

    public int Version { get; set; }

    public decimal TarifaBsKwh { get; set; }

    public decimal CostoInstalacionBsKwp { get; set; }

    public decimal GeneracionKwhKwpMes { get; set; }

    public decimal MantenimientoBsAnio { get; set; }

    public DateTime VigenteDesde { get; set; }

    [Required]
    public string CreadoPorId { get; set; } = string.Empty;

    public ApplicationUser CreadoPor { get; set; } = null!;
}
