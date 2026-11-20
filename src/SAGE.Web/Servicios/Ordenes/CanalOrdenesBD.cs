using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Configuracion;
using SAGE.Web.Servicios.EnVivo;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Ordenes;

// La fábrica lo entrega envuelto por CanalOrdenesProtegido.
// Crea su propio DbContext por llamada para no compartirlo entre
// operaciones concurrentes del mismo circuito de Blazor.
// Patrón: Command — rol: invocador
// Patrón: Abstract Factory — rol: producto concreto
public sealed class CanalOrdenesBD : ICanalOrdenes
{
    public static readonly string[] ModosValidos = ["AUTO", "SOLAR", "CARGADOR"];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUsuarioActual _usuario;
    private readonly EstadoDispositivosEnVivo _enVivo;

    public CanalOrdenesBD(
        IServiceScopeFactory scopeFactory,
        IUsuarioActual usuario,
        EstadoDispositivosEnVivo enVivo)
    {
        _scopeFactory = scopeFactory;
        _usuario = usuario;
        _enVivo = enVivo;
    }

    public Task<Orden> EnviarModoAsync(int dispositivoId, string modo)
    {
        var valor = (modo ?? string.Empty).Trim().ToUpperInvariant();

        if (!ModosValidos.Contains(valor))
            throw new ArgumentException(
                "El modo debe ser AUTO, SOLAR o CARGADOR.", nameof(modo));

        return CrearAsync(
            dispositivoId,
            Orden.TipoModo,
            JsonSerializer.Serialize(valor));
    }

    public Task<Orden> EnviarParametrosAsync(int dispositivoId, ParametrosFuente p)
    {
        ArgumentNullException.ThrowIfNull(p);

        return CrearAsync(
            dispositivoId,
            Orden.TipoParametros,
            p.AJsonOrden());
    }

    public async Task<IReadOnlyList<Orden>> UltimasAsync(int dispositivoId, int cantidad)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.Ordenes
            .AsNoTracking()
            .Include(o => o.Usuario)
            .Where(o => o.DispositivoId == dispositivoId)
            .OrderByDescending(o => o.Creada)
            .ThenByDescending(o => o.Id)
            .Take(Math.Clamp(cantidad, 1, 200))
            .ToListAsync();
    }

    private async Task<Orden> CrearAsync(int dispositivoId, string tipo, string valorJson)
    {
        await _usuario.CargarAsync();

        var usuarioId = _usuario.UsuarioId
            ?? throw new UnauthorizedAccessException("No hay un usuario autenticado.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existe = await db.Dispositivos.AnyAsync(d => d.Id == dispositivoId);

        if (!existe)
            throw new InvalidOperationException("El dispositivo no existe.");

        var ahora = DateTime.UtcNow;

        var abiertas = await db.Ordenes
            .Where(o =>
                o.DispositivoId == dispositivoId &&
                (o.Estado == Orden.EstadoPendiente || o.Estado == Orden.EstadoEntregada))
            .ToListAsync();

        // Primero se vencen las que ya pasaron su plazo.
        foreach (var vencida in abiertas.Where(o => o.Vencida(ahora)))
            vencida.ExpirarPorVencimiento(ahora);

        if (abiertas.Any(o => o.Estado == Orden.EstadoEntregada))
            throw new InvalidOperationException("Espere el resultado de la orden anterior.");

        // Una pendiente se reemplaza por la nueva.
        foreach (var pendiente in abiertas.Where(o => o.Estado == Orden.EstadoPendiente))
            pendiente.Expirar(ahora, Orden.MotivoReemplazada);

        var orden = new Orden
        {
            DispositivoId = dispositivoId,
            UsuarioId = usuarioId,
            Tipo = tipo,
            ValorJson = valorJson,
            Creada = ahora
        };

        db.Ordenes.Add(orden);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // La orden anterior cambió de estado mientras tanto (se entregó o expiró).
            throw new InvalidOperationException(
                "La orden anterior cambió de estado. Intente de nuevo.");
        }

        _enVivo.NotificarOrdenes(dispositivoId);

        return orden;
    }
}
