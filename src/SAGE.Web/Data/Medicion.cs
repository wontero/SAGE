using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

public class Medicion
{
    public long Id { get; set; }

    public int DispositivoId { get; set; }

    public Dispositivo Dispositivo { get; set; } = default!;

    public long BootId { get; set; }

    public long Seq { get; set; }

    public DateTime Ts { get; set; }

    public bool TsValida { get; set; }

    public DateTime TsServidor { get; set; }

    // Fuente solar: V, mA, mW y Wh.
    [Precision(18, 6)]
    public decimal VSol { get; set; }

    [Precision(18, 6)]
    public decimal ISolMa { get; set; }

    [Precision(18, 6)]
    public decimal PSolMw { get; set; }

    [Precision(18, 6)]
    public decimal WhSolAcum { get; set; }

    // Fuente de red: V, mA, mW y Wh.
    // En el JSON del dispositivo se llama "cargador".
    [Precision(18, 6)]
    public decimal VRed { get; set; }

    [Precision(18, 6)]
    public decimal IRedMa { get; set; }

    [Precision(18, 6)]
    public decimal PRedMw { get; set; }

    [Precision(18, 6)]
    public decimal WhRedAcum { get; set; }

    [Required]
    [MaxLength(50)]
    public string Modo { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Estado { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FuenteSeleccionada { get; set; } = string.Empty;

    public bool AlimentacionComprobada { get; set; }
}