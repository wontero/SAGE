using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

public class Dispositivo
{
    public int Id { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    public Empresa Empresa { get; set; } = default!;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }

    public bool EsSimulado { get; set; }

    // Se guarda el hash SHA-256, nunca el token original.
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoContacto { get; set; }

    [MaxLength(50)]
    public string? ModoActual { get; set; }

    [MaxLength(50)]
    public string? EstadoActual { get; set; }

    [MaxLength(50)]
    public string? FuenteSeleccionada { get; set; }

    public bool? AlimentacionComprobada { get; set; }
}