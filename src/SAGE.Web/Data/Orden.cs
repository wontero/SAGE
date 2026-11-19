using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Data;

// S23-M9: orden enviada a un dispositivo (RF11).
// La entidad controla sus propias transiciones: nadie asigna Estado desde afuera.
// Patrón: Command — rol: comando
public class Orden
{
    public const string TipoModo = "modo";
    public const string TipoParametros = "parametros";

    public const string EstadoPendiente = "pendiente";
    public const string EstadoEntregada = "entregada";
    public const string EstadoEjecutada = "ejecutada";
    public const string EstadoRechazada = "rechazada";
    public const string EstadoExpirada = "expirada";

    public const string MotivoNoEntregada = "no_entregada";
    public const string MotivoSinConfirmacion = "sin_confirmacion";
    public const string MotivoReemplazada = "reemplazada";

    public static readonly TimeSpan PlazoEntrega = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan PlazoConfirmacion = TimeSpan.FromSeconds(60);

    public long Id { get; set; }

    public int DispositivoId { get; set; }

    public Dispositivo Dispositivo { get; set; } = default!;

    [Required]
    [MaxLength(450)]
    public string UsuarioId { get; set; } = string.Empty;

    public ApplicationUser? Usuario { get; set; }

    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; } = TipoModo;

    // "\"SOLAR\"" para modo; el objeto JSON de la sección 4 para parámetros.
    [Required]
    public string ValorJson { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Estado { get; private set; } = EstadoPendiente;

    [MaxLength(50)]
    public string? Motivo { get; private set; }

    public DateTime Creada { get; set; }

    public DateTime? Entregada { get; private set; }

    public DateTime? Resuelta { get; private set; }

    public bool EstaAbierta =>
        Estado is EstadoPendiente or EstadoEntregada;

    // Pendiente y pasaron más de 30 s desde que se creó,
    // o entregada y pasaron más de 60 s desde que se entregó.
    public bool Vencida(DateTime ahora)
    {
        return Estado switch
        {
            EstadoPendiente => ahora - Creada > PlazoEntrega,
            EstadoEntregada => Entregada is DateTime e && ahora - e > PlazoConfirmacion,
            _ => false
        };
    }

    // Solo desde pendiente.
    public void Entregar(DateTime ahora)
    {
        if (Estado != EstadoPendiente)
            throw new InvalidOperationException(
                $"Solo se puede entregar una orden pendiente (estado actual: {Estado}).");

        Estado = EstadoEntregada;
        Entregada = ahora;
    }

    // "no_entregada" si estaba pendiente, "sin_confirmacion" si estaba entregada.
    // El motivo explícito se usa para casos como "reemplazada".
    public void Expirar(DateTime ahora, string motivo)
    {
        if (!EstaAbierta)
            throw new InvalidOperationException(
                $"Solo se puede expirar una orden pendiente o entregada (estado actual: {Estado}).");

        Estado = EstadoExpirada;
        Motivo = motivo;
        Resuelta = ahora;
    }

    // Expira con el motivo que corresponde al estado actual.
    public void ExpirarPorVencimiento(DateTime ahora)
    {
        Expirar(
            ahora,
            Estado == EstadoPendiente ? MotivoNoEntregada : MotivoSinConfirmacion);
    }

    // Solo desde entregada; en cualquier otro estado se ignora.
    public void Resolver(string resultado, string? motivo, DateTime ahora)
    {
        if (Estado != EstadoEntregada)
            return;

        if (resultado == EstadoEjecutada)
        {
            Estado = EstadoEjecutada;
            Motivo = null;
        }
        else if (resultado == EstadoRechazada)
        {
            Estado = EstadoRechazada;
            Motivo = string.IsNullOrWhiteSpace(motivo)
                ? null
                : motivo.Length > 50 ? motivo[..50] : motivo;
        }
        else
        {
            return;
        }

        Resuelta = ahora;
    }
}
