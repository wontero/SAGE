
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Api;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Alertas;
using SAGE.Web.Servicios.EnVivo;
using Dispositivo = SAGE.Web.Data.Dispositivo;

namespace SAGE.Web.Servicios.Telemetria;

public sealed record RespuestaSincronizacion(
    DateTime HoraServidor,
    object? Orden);

// Patrón: Facade — rol: fachada
public sealed class FachadaSincronizacion
{
    private readonly ApplicationDbContext _db;
    private readonly IAdaptadorTelemetria _adaptador;
    private readonly ServicioAlertas _servicioAlertas;
    private readonly EstadoDispositivosEnVivo _enVivo;
    private readonly ILogger<FachadaSincronizacion> _logger;

    public FachadaSincronizacion(
        ApplicationDbContext db,
        IAdaptadorTelemetria adaptador,
        ServicioAlertas servicioAlertas,
        EstadoDispositivosEnVivo enVivo,
        ILogger<FachadaSincronizacion> logger)
    {
        _db = db;
        _adaptador = adaptador;
        _servicioAlertas = servicioAlertas;
        _enVivo = enVivo;
        _logger = logger;
    }

    // =====================================================
    // SINCRONIZACIÓN PRINCIPAL
    // =====================================================

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
                    token,
                    paquete,
                    ct);
            }
            catch (DbUpdateException ex)
                when (EsDuplicado(ex) && intento == 0)
            {
                _logger.LogInformation(
                    "Registro duplicado detectado. " +
                    "Reintentando sincronización.");

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
        // 1. Autenticar dispositivo y empresa.
        var dispositivo = await AutenticarAsync(
            token,
            ct);

        if (dispositivo is null)
            return null;

        var horaServidor = DateTime.UtcNow;

        // 2. Convertir el paquete a Medicion.
        var medicion = _adaptador.AMedicion(
            paquete,
            dispositivo.Id,
            horaServidor);

        // 3. Evitar mediciones duplicadas.
        var medicionNueva = await RegistrarMedicionAsync(
            dispositivo,
            paquete,
            medicion,
            ct);

        // 4. Actualizar el último contacto.
        RegistrarContacto(
            dispositivo,
            paquete,
            horaServidor,
            medicionNueva);

        // 5. Registrar únicamente eventos nuevos.
        var eventosNuevos = await RegistrarEventosAsync(
            dispositivo,
            paquete,
            horaServidor,
            ct);

        // 6. Resultados de órdenes (M9).
        await ActualizarResultadosOrdenesAsync(
            paquete,
            ct);

        // 7. Evaluar las alertas (M7).
        var alertasCambiaron =
            await _servicioAlertas.EvaluarAsync(
                dispositivo,
                medicionNueva ? medicion : null,
                eventosNuevos,
                ct);

        // 8. Órdenes pendientes (M9).
        var orden = await EntregarOrdenPendienteAsync(
            dispositivo,
            ct);

        // 9. Guardar los cambios en SQL Server.
        await _db.SaveChangesAsync(ct);

        // =====================================================
        // S23-M6: NOTIFICACIONES EN TIEMPO REAL
        // =====================================================
        // Notificar únicamente DESPUÉS de guardar.
        // Si falla SaveChangesAsync, no se emiten avisos.

        if (medicionNueva)
        {
            // Factor provisional 1 hasta implementar
            // FactorEscala en Dispositivo (S23-M6).
            var estado = EstadoDispositivo.Desde(
                medicion,
                dispositivo.EmpresaId,
                1m);

            _enVivo.Actualizar(estado);
        }

        if (eventosNuevos.Count > 0)
        {
            _enVivo.NotificarEventos(dispositivo.Id);
        }

        if (alertasCambiaron)
        {
            _enVivo.NotificarAlertas(
                dispositivo.EmpresaId);

            _logger.LogInformation(
                "Alertas actualizadas. " +
                "Dispositivo {Id}, empresa {EmpresaId}.",
                dispositivo.Id,
                dispositivo.EmpresaId);
        }

        return new RespuestaSincronizacion(
            horaServidor,
            orden);
    }

    // =====================================================
    // AUTENTICACIÓN
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

        // Nunca registrar token ni hash.
        var tokenHash =
            TokenDispositivo.CalcularHash(token);

        var dispositivo = await _db.Dispositivos
            .SingleOrDefaultAsync(
                d => d.TokenHash == tokenHash,
                ct);

        if (dispositivo is null)
        {
            _logger.LogWarning(
                "Autenticación rechazada: token " +
                "sin dispositivo asociado.");

            return null;
        }

        if (!dispositivo.Activo)
        {
            _logger.LogWarning(
                "Dispositivo {Id} inactivo.",
                dispositivo.Id);

            return null;
        }

        var empresaActiva = await _db.Empresas
            .AnyAsync(
                e => e.Id == dispositivo.EmpresaId &&
                     e.Activa,
                ct);

        if (!empresaActiva)
        {
            _logger.LogWarning(
                "Empresa inactiva del dispositivo {Id}.",
                dispositivo.Id);

            return null;
        }

        return dispositivo;
    }

    // =====================================================
    // REGISTRO DE MEDICIONES
    // =====================================================

    private async Task<bool> RegistrarMedicionAsync(
        Dispositivo dispositivo,
        TelemetriaRequest paquete,
        Medicion medicion,
        CancellationToken ct)
    {
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

    // =====================================================
    // ACTUALIZAR ÚLTIMO CONTACTO
    // =====================================================

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
    // S23-M5: REGISTRO DE EVENTOS
    // =====================================================

    private async Task<List<Evento>> RegistrarEventosAsync(
        Dispositivo dispositivo,
        TelemetriaRequest paquete,
        DateTime horaServidor,
        CancellationToken ct)
    {
        var eventosNuevos = new List<Evento>();

        if (paquete.Eventos is null)
            return eventosNuevos;

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

            if (existe)
                continue;

            _db.Eventos.Add(evento);
            eventosNuevos.Add(evento);
        }

        return eventosNuevos;
    }

    // =====================================================
    // S23-M9: RESULTADOS DE ÓRDENES
    // =====================================================

    private static Task ActualizarResultadosOrdenesAsync(
        TelemetriaRequest paquete,
        CancellationToken ct)
    {
        // Pendiente: Ethan implementará M9.
        return Task.CompletedTask;
    }

    // =====================================================
    // S23-M9: ENTREGAR ÓRDENES
    // =====================================================

    private static Task<object?> EntregarOrdenPendienteAsync(
        Dispositivo dispositivo,
        CancellationToken ct)
    {
        // Pendiente: Ethan implementará M9.
        return Task.FromResult<object?>(null);
    }

    // =====================================================
    // DETECCIÓN DE DUPLICADOS
    // =====================================================

    private static bool EsDuplicado(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sql &&
               (sql.Number == 2601 || sql.Number == 2627);
    }
}
