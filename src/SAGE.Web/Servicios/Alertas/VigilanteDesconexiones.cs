
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Alertas;

// S23-M7: Monitoreo automático de desconexiones.
public sealed class VigilanteDesconexiones : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VigilanteDesconexiones> _logger;

    // Revisar dispositivos cada 5 segundos.
    private static readonly TimeSpan Intervalo =
        TimeSpan.FromSeconds(5);

    // Tiempo máximo permitido sin comunicación.
    private static readonly TimeSpan Limite =
        TimeSpan.FromSeconds(30);

    public VigilanteDesconexiones(
        IServiceScopeFactory scopeFactory,
        ILogger<VigilanteDesconexiones> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RevisarAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error al revisar las desconexiones.");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Detención normal de SAGE.Web.
        }
    }

    private async Task RevisarAsync(CancellationToken ct)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var ahora = DateTime.UtcNow;
        var fechaLimite = ahora - Limite;

        // S23-M7:
        // La comprobación del último contacto y la
        // inserción de la alerta se realizan dentro
        // de una misma sentencia SQL.
        //
        // Los bloqueos ayudan a evitar registros
        // duplicados y cambios concurrentes durante
        // la comprobación.
        //
        // El índice único filtrado de Alertas
        // también protege frente a duplicados activos.

        var creadas = await db.Database
            .ExecuteSqlInterpolatedAsync($@"
                INSERT INTO [Alertas]
                (
                    [DispositivoId],
                    [Tipo],
                    [Mensaje],
                    [Activa],
                    [FechaCreacion],
                    [FechaResolucion]
                )
                SELECT
                    d.[Id],
                    N'SIN_CONEXION',
                    N'El dispositivo lleva más de 30 segundos sin enviar telemetría.',
                    CAST(1 AS bit),
                    {ahora},
                    NULL
                FROM [Dispositivos] AS d
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE
                    d.[Activo] = 1
                    AND d.[UltimoContacto] IS NOT NULL
                    AND d.[UltimoContacto] < {fechaLimite}
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM [Alertas] AS a
                            WITH (UPDLOCK, HOLDLOCK)
                        WHERE
                            a.[DispositivoId] = d.[Id]
                            AND a.[Tipo] = N'SIN_CONEXION'
                            AND a.[Activa] = 1
                    );
            ", ct);

        if (creadas > 0)
        {
            _logger.LogWarning(
                "S23-M7: se registraron {Cantidad} " +
                "alertas nuevas por desconexión.",
                creadas);
        }
    }
}
