using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.EnVivo;

namespace SAGE.Web.Servicios.Ordenes;

// S23-M9: cada 1 s expira las órdenes vencidas de todos los dispositivos.
// Así una orden a un dispositivo apagado se ve "expirada" aunque no llegue telemetría (RNF08).
public sealed class ExpiradorOrdenes : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EstadoDispositivosEnVivo _enVivo;
    private readonly ILogger<ExpiradorOrdenes> _logger;

    public ExpiradorOrdenes(
        IServiceScopeFactory scopeFactory,
        EstadoDispositivosEnVivo enVivo,
        ILogger<ExpiradorOrdenes> logger)
    {
        _scopeFactory = scopeFactory;
        _enVivo = enVivo;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo);

        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ExpirarVencidasAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Un error puntual (p. ej., la base todavía no está lista) no detiene el servicio.
                    _logger.LogError(ex, "Error al expirar órdenes vencidas.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // La aplicación se está cerrando.
        }
    }

    public async Task<int> ExpirarVencidasAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var ahora = DateTime.UtcNow;
        var limiteEntrega = ahora - Orden.PlazoEntrega;
        var limiteConfirmacion = ahora - Orden.PlazoConfirmacion;

        var vencidas = await db.Ordenes
            .Where(o =>
                (o.Estado == Orden.EstadoPendiente && o.Creada < limiteEntrega) ||
                (o.Estado == Orden.EstadoEntregada && o.Entregada < limiteConfirmacion))
            .ToListAsync(ct);

        vencidas = vencidas.Where(o => o.Vencida(ahora)).ToList();

        if (vencidas.Count == 0)
            return 0;

        foreach (var orden in vencidas)
            orden.ExpirarPorVencimiento(ahora);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra petición (la telemetría) cambió la orden a la vez; se reintenta en el próximo ciclo.
            return 0;
        }

        foreach (var dispositivoId in vencidas.Select(o => o.DispositivoId).Distinct())
            _enVivo.NotificarOrdenes(dispositivoId);

        return vencidas.Count;
    }
}
