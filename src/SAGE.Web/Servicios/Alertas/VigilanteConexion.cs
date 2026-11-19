
using System.Data;
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.EnVivo;

namespace SAGE.Web.Servicios.Alertas;

public sealed class VigilanteConexion : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EstadoDispositivosEnVivo _enVivo;
    private readonly ILogger<VigilanteConexion> _logger;

    private static readonly TimeSpan Intervalo =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan Limite =
        TimeSpan.FromSeconds(30);

    public VigilanteConexion(
        IServiceScopeFactory scopeFactory,
        EstadoDispositivosEnVivo enVivo,
        ILogger<VigilanteConexion> logger)
    {
        _scopeFactory = scopeFactory;
        _enVivo = enVivo;
        _logger = logger;
    }

    // =====================================================
    // S23-M7: VIGILANCIA DE CONEXIÓN
    // =====================================================

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        try
        {
            while (await timer.WaitForNextTickAsync(
                stoppingToken))
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
                        "Error al revisar la conexión " +
                        "de los dispositivos.");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Detención normal de la aplicación.
        }
    }

    // =====================================================
    // DETECTAR Y REGISTRAR DESCONEXIONES
    // =====================================================

    private async Task RevisarAsync(
        CancellationToken ct)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var ahora = DateTime.UtcNow;
        var fechaLimite = ahora - Limite;

        // Utilizar una sola operación SQL para
        // comprobar y registrar las desconexiones.
        //
        // OUTPUT devuelve únicamente los dispositivos
        // para los que se insertó una alerta nueva.

        var dispositivosNuevos =
            new List<int>();

        var conexion = db.Database.GetDbConnection();

        await db.Database.OpenConnectionAsync(ct);

        try
        {
            await using var comando =
                conexion.CreateCommand();

            comando.CommandText = """
                SET NOCOUNT ON;

                INSERT INTO [Alertas]
                (
                    [DispositivoId],
                    [Tipo],
                    [Mensaje],
                    [Activa],
                    [FechaCreacion],
                    [FechaResolucion],
                    [Inicio],
                    [Fin],
                    [EventoId]
                )
                OUTPUT INSERTED.[DispositivoId]
                SELECT
                    d.[Id],
                    N'sin_conexion',
                    N'El dispositivo lleva más de 30 segundos sin enviar telemetría.',
                    CAST(1 AS bit),
                    @ahora,
                    NULL,
                    @ahora,
                    NULL,
                    NULL
                FROM [Dispositivos] AS d
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE
                    d.[Activo] = 1
                    AND d.[UltimoContacto] IS NOT NULL
                    AND d.[UltimoContacto] < @fechaLimite
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM [Alertas] AS a
                            WITH (UPDLOCK, HOLDLOCK)
                        WHERE
                            a.[DispositivoId] = d.[Id]
                            AND LOWER(a.[Tipo]) = N'sin_conexion'
                            AND a.[Activa] = 1
                    );
                """;

            var parametroAhora =
                comando.CreateParameter();

            parametroAhora.ParameterName = "@ahora";
            parametroAhora.DbType = DbType.DateTime2;
            parametroAhora.Value = ahora;

            comando.Parameters.Add(parametroAhora);

            var parametroLimite =
                comando.CreateParameter();

            parametroLimite.ParameterName = "@fechaLimite";
            parametroLimite.DbType = DbType.DateTime2;
            parametroLimite.Value = fechaLimite;

            comando.Parameters.Add(parametroLimite);

            await using var lector =
                await comando.ExecuteReaderAsync(ct);

            while (await lector.ReadAsync(ct))
            {
                dispositivosNuevos.Add(
                    lector.GetInt32(0));
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        if (dispositivosNuevos.Count == 0)
            return;

        // Obtener las empresas de los dispositivos
        // para notificar solo a las correspondientes.

        var dispositivos = await db.Dispositivos
            .AsNoTracking()
            .Where(d =>
                dispositivosNuevos.Contains(d.Id))
            .Select(d => new
            {
                d.Id,
                d.EmpresaId
            })
            .ToListAsync(ct);

        // =====================================================
        // S23-M6: NOTIFICAR AL OBSERVER
        // =====================================================

        foreach (var dispositivo in dispositivos)
        {
            _enVivo.MarcarSinConexion(
                dispositivo.Id);
        }

        foreach (var empresaId in dispositivos
                     .Select(d => d.EmpresaId)
                     .Distinct())
        {
            _enVivo.NotificarAlertas(
                empresaId);
        }

        _logger.LogWarning(
            "S23-M7: se registraron {Cantidad} " +
            "alertas nuevas por desconexión.",
            dispositivosNuevos.Count);
    }
}
