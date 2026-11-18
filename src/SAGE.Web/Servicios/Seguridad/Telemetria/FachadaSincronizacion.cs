
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Api;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Telemetria;

// Respuesta de la sincronización.
public sealed record RespuestaSincronizacion(
    DateTime HoraServidor,
    object? Orden);

// Patrón de diseño: Facade
public sealed class FachadaSincronizacion
{
    private readonly ApplicationDbContext _db;
    private readonly IAdaptadorTelemetria _adaptador;
    private readonly ILogger<FachadaSincronizacion> _logger;

    public FachadaSincronizacion(
        ApplicationDbContext db,
        IAdaptadorTelemetria adaptador,
        ILogger<FachadaSincronizacion> logger)
    {
        _db = db;
        _adaptador = adaptador;
        _logger = logger;
    }

    public async Task<RespuestaSincronizacion?> SincronizarAsync(
        string token,
        TelemetriaRequest paquete,
        CancellationToken ct)
    {
        for (var intento = 0; intento < 2; intento++)
        {
            try
            {
                return await SincronizarInternoAsync(
                    token, paquete, ct);
            }
            catch (DbUpdateException ex)
                when (EsDuplicado(ex) && intento == 0)
            {
                _logger.LogInformation(
                    "Se detectó un envío simultáneo duplicado.");

                _db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException(
            "No se pudo completar la sincronización.");
    }

    private async Task<RespuestaSincronizacion?>
        SincronizarInternoAsync(
            string token,
            TelemetriaRequest paquete,
            CancellationToken ct)
    {
        // 1. Autenticar el dispositivo.
        var dispositivo = await AutenticarAsync(token, ct);

        if (dispositivo is null)
            return null;

        var horaServidor = DateTime.UtcNow;

        // 2. Registrar medición.
        var medicionNueva = await RegistrarMedicionAsync(
            dispositivo,
            paquete,
            horaServidor,
            ct);

        // 3. Actualizar contacto.
        RegistrarContacto(
            dispositivo,
            paquete,
            horaServidor,
            medicionNueva);

        // 4. Registrar eventos.
        await RegistrarEventosAsync(
            dispositivo,
            paquete,
            horaServidor,
            ct);

        // 5. Resultados de órdenes.
        await ActualizarResultadosOrdenesAsync(paquete, ct);

        // 6. Alertas.
        await EvaluarAlertasAsync(dispositivo, paquete, ct);

        // 7. Órdenes pendientes.
        var orden = await EntregarOrdenPendienteAsync(
            dispositivo, ct);

        // 8. Guardar cambios.
        await _db.SaveChangesAsync(ct);

        return new RespuestaSincronizacion(
            horaServidor,
            orden);
    }

    // =====================================================
    // AUTENTICACIÓN Y DIAGNÓSTICO DE HTTP 401
    // =====================================================

    private async Task<Dispositivo?> AutenticarAsync(
        string token,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 256)
        {
            _logger.LogWarning(
                "Autenticación rechazada: token vacío " +
                "o longitud no permitida.");

            return null;
        }

        // Nunca registrar el token ni su hash.
        var tokenHash = TokenDispositivo.CalcularHash(token);

        // Diagnóstico: buscar coincidencia del hash.
        var dispositivo = await _db.Dispositivos
            .SingleOrDefaultAsync(
                d => d.TokenHash == tokenHash,
                ct);

        if (dispositivo is null)
        {
            _logger.LogWarning(
                "Autenticación rechazada: ningún dispositivo " +
                "coincide con el token recibido.");

            return null;
        }

        if (!dispositivo.Activo)
        {
            _logger.LogWarning(
                "Autenticación rechazada: dispositivo {Id} inactivo.",
                dispositivo.Id);

            return null;
        }

        var empresaActiva = await _db.Empresas
            .AnyAsync(
                e => e.Id == dispositivo.EmpresaId && e.Activa,
                ct);

        if (!empresaActiva)
        {
            _logger.LogWarning(
                "Autenticación rechazada: empresa del " +
                "dispositivo {Id} no activa.",
                dispositivo.Id);

            return null;
        }

        _logger.LogInformation(
            "Dispositivo {Id} autenticado correctamente.",
            dispositivo.Id);

        return dispositivo;
    }

    // =====================================================
    // REGISTRO DE MEDICIONES
    // =====================================================

    private async Task<bool> RegistrarMedicionAsync(
        Dispositivo dispositivo,
        TelemetriaRequest paquete,
        DateTime horaServidor,
        CancellationToken ct)
    {
        var medicion = _adaptador.AMedicion(
            paquete,
            dispositivo.Id,
            horaServidor);

        var existe = await _db.Mediciones.AnyAsync(
            m => m.DispositivoId == dispositivo.Id &&
                 m.BootId == paquete.BootId &&
                 m.Seq == paquete.Seq,
            ct);

        if (existe)
            return false;

        _db.Mediciones.Add(medicion);

        return true;
    }

    private static void RegistrarContacto(
        Dispositivo dispositivo,
        TelemetriaRequest paquete,
        DateTime horaServidor,
        bool medicionNueva)
    {
        dispositivo.UltimoContacto = horaServidor;

        if (!medicionNueva)
            return;

        dispositivo.ModoActual = paquete.Modo;
        dispositivo.EstadoActual = paquete.Estado;
        dispositivo.FuenteSeleccionada =
            paquete.FuenteSeleccionada;

        dispositivo.AlimentacionComprobada =
            paquete.AlimentacionComprobada;
    }

    // =====================================================
    // REGISTRO DE EVENTOS
    // =====================================================

    private async Task RegistrarEventosAsync(
        Dispositivo dispositivo,
        TelemetriaRequest paquete,
        DateTime horaServidor,
        CancellationToken ct)
    {
        if (paquete.Eventos is null)
            return;

        var secuencias = new HashSet<long>();

        foreach (var recibido in paquete.Eventos)
        {
            if (recibido is null)
            {
                _logger.LogWarning(
                    "Evento nulo recibido del dispositivo {Id}.",
                    dispositivo.Id);

                continue;
            }

            if (!secuencias.Add(recibido.Seq))
                continue;

            var evento = _adaptador.AEvento(
                recibido,
                paquete.BootId,
                paquete.TsValida,
                dispositivo.Id,
                horaServidor);

            if (evento is null)
            {
                _logger.LogWarning(
                    "Evento inválido descartado. " +
                    "Dispositivo {Id}, secuencia {Seq}.",
                    dispositivo.Id,
                    recibido.Seq);

                continue;
            }

            var existe = await _db.Eventos.AnyAsync(
                e => e.DispositivoId == dispositivo.Id &&
                     e.BootId == paquete.BootId &&
                     e.Seq == recibido.Seq,
                ct);

            if (!existe)
                _db.Eventos.Add(evento);
        }
    }

    // =====================================================
    // FUNCIONALIDADES DE PRÓXIMOS SPRINTS
    // =====================================================

    private static Task ActualizarResultadosOrdenesAsync(
        TelemetriaRequest paquete,
        CancellationToken ct)
    {
        // Pendiente: S23-M9.
        return Task.CompletedTask;
    }

    private static Task EvaluarAlertasAsync(
        Dispositivo dispositivo,
        TelemetriaRequest paquete,
        CancellationToken ct)
    {
        // Pendiente: S23-M7.
        return Task.CompletedTask;
    }

    private static Task<object?> EntregarOrdenPendienteAsync(
        Dispositivo dispositivo,
        CancellationToken ct)
    {
        // Pendiente: S23-M9.
        return Task.FromResult<object?>(null);
    }

    private static bool EsDuplicado(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sql &&
               (sql.Number == 2601 || sql.Number == 2627);
    }
}
