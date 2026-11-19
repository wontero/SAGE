using System.Collections.Concurrent;

namespace SAGE.Web.Servicios.EnVivo;

// Patrón: Singleton — rol: instancia única
// Patrón: Observer — rol: sujeto
public sealed class EstadoDispositivosEnVivo
{
    private readonly ConcurrentDictionary<int, EstadoDispositivo>
        _estados = new();

    private readonly ILogger<EstadoDispositivosEnVivo> _logger;

    public EstadoDispositivosEnVivo(
        ILogger<EstadoDispositivosEnVivo> logger)
    {
        _logger = logger;
    }

    // S23-M6: eventos que reciben las pantallas.
    public event Action<EstadoDispositivo>? Cambio;
    public event Action<int>? EventosRegistrados;
    public event Action<int>? AlertasCambiaron;

    // =====================================================
    // ACTUALIZAR ESTADO
    // =====================================================

    public void Actualizar(EstadoDispositivo estado)
    {
        _estados.AddOrUpdate(
            estado.DispositivoId,
            estado,
            (_, anterior) =>
                estado.TsServidor >= anterior.TsServidor
                    ? estado
                    : anterior);

        var actual = Obtener(estado.DispositivoId);

        if (actual is not null)
            Notificar(Cambio, actual);
    }

    // =====================================================
    // MARCAR DESCONEXIÓN
    // =====================================================

    public void MarcarSinConexion(int dispositivoId)
    {
        if (!_estados.TryGetValue(
            dispositivoId,
            out var anterior))
        {
            return;
        }

        var desconectado = anterior with
        {
            EnLinea = false
        };

        // Evitar notificar reiteradamente un estado
        // que ya estaba marcado como desconectado.
        if (anterior.EnLinea &&
            _estados.TryUpdate(
                dispositivoId,
                desconectado,
                anterior))
        {
            Notificar(Cambio, desconectado);
        }
    }

    // =====================================================
    // OBTENER ÚLTIMO ESTADO
    // =====================================================

    public EstadoDispositivo? Obtener(int dispositivoId)
    {
        return _estados.TryGetValue(
            dispositivoId,
            out var estado)
                ? estado
                : null;
    }

    // =====================================================
    // NOTIFICACIÓN DE EVENTOS
    // =====================================================

    public void NotificarEventos(int dispositivoId)
    {
        Notificar(
            EventosRegistrados,
            dispositivoId);
    }

    // =====================================================
    // NOTIFICACIÓN DE ALERTAS
    // =====================================================

    public void NotificarAlertas(int empresaId)
    {
        Notificar(
            AlertasCambiaron,
            empresaId);
    }

    // =====================================================
    // PROTECCIÓN DE SUSCRIPTORES
    // =====================================================

    private void Notificar<T>(
        Action<T>? evento,
        T dato)
    {
        if (evento is null)
            return;

        foreach (var suscriptor in evento.GetInvocationList())
        {
            try
            {
                ((Action<T>)suscriptor)(dato);
            }
            catch (Exception ex)
            {
                // Un componente que falla no debe
                // interrumpir la telemetría.
                _logger.LogError(
                    ex,
                    "Error al notificar un observador " +
                    "de SAGE.");
            }
        }
    }
}