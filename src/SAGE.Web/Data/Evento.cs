
using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

public class Evento
{
    public long Id { get; set; }

    public int DispositivoId { get; set; }

    public Dispositivo Dispositivo { get; set; } = default!;

    public long BootId { get; set; }

    public long Seq { get; set; }

    public DateTime Ts { get; set; }

    public bool TsValida { get; set; }

    public DateTime TsServidor { get; set; }

    [Required]
    [MaxLength(30)]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Estado { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? De { get; set; }

    [MaxLength(30)]
    public string? A { get; set; }

    [Required]
    [MaxLength(30)]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Origen { get; set; } = string.Empty;

    // S23-M9: FK a Orden (SetNull).
    public long? OrdenId { get; set; }

    public Orden? Orden { get; set; }
}
