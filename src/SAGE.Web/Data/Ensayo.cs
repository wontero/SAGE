using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

// S23-M12: ensayo sobre un dispositivo (RF17). Fin = null significa "en curso".
public class Ensayo
{
    public int Id { get; set; }

    public int DispositivoId { get; set; }

    public Dispositivo Dispositivo { get; set; } = default!;

    [Required]
    [MaxLength(450)]
    public string CreadoPorId { get; set; } = string.Empty;

    public ApplicationUser? CreadoPor { get; set; }

    // UTC.
    public DateTime Inicio { get; set; }

    // UTC; null = en curso.
    public DateTime? Fin { get; set; }

    [Required(ErrorMessage = "La descripción de la carga es obligatoria.")]
    [MaxLength(200, ErrorMessage = "La carga no puede superar 200 caracteres.")]
    public string CargaDescripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Las condiciones son obligatorias.")]
    [MaxLength(500, ErrorMessage = "Las condiciones no pueden superar 500 caracteres.")]
    public string Condiciones { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Las notas no pueden superar 1000 caracteres.")]
    public string? Notas { get; set; }

    public bool EnCurso => Fin is null;
}
