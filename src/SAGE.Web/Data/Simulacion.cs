using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

// Patrón: Builder — rol: producto
public class Simulacion
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public Empresa Empresa { get; set; } = null!;

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ParametrosVersion { get; set; }

    public decimal TarifaUsadaBsKwh { get; set; }

    public decimal ConsumoMensualKwh { get; set; }

    public int MesesBase { get; set; }

    public decimal KwpSolar { get; set; }

    public decimal KwpMixto { get; set; }

    public int HorizonteAnios { get; set; }

    public decimal InversionSolarBs { get; set; }

    public decimal InversionMixtoBs { get; set; }

    public decimal CostoAnualRedBs { get; set; }

    public decimal CostoAnualSolarBs { get; set; }

    public decimal CostoAnualMixtoBs { get; set; }

    public decimal CostoAcumRedBs { get; set; }

    public decimal CostoAcumSolarBs { get; set; }

    public decimal CostoAcumMixtoBs { get; set; }

    public decimal? RecuperacionSolarAnios { get; set; }

    public decimal? RecuperacionMixtoAnios { get; set; }

    public List<string> Advertencias { get; set; } = new();
}
