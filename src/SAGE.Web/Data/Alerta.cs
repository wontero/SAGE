
using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

public class Alerta
{
    public long Id { get; set; }

    public int DispositivoId { get; set; }

    public Dispositivo Dispositivo { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Tipo { get; set; } = string.Empty;

    // ================================================
    // S23-M7: CAMPOS DE LA GUÍA OFICIAL
    // ================================================

    // Inicio de la alerta en UTC.
    // Nullable temporalmente para migrar el historial.
    public DateTime? Inicio { get; set; }

    // Fin de la alerta en UTC.
    // NULL significa que sigue abierta.
    public DateTime? Fin { get; set; }

    // Evento que originó la alerta, si corresponde.
    public long? EventoId { get; set; }

    public Evento? Evento { get; set; }

    // ================================================
    // COMPATIBILIDAD CON LA IMPLEMENTACIÓN ANTERIOR
    // ================================================

    // Se mantiene temporalmente para no romper
    // las pantallas ni los servicios existentes.
    [MaxLength(500)]
    public string Mensaje { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
        = DateTime.UtcNow;

    public DateTime? FechaResolucion { get; set; }
}
