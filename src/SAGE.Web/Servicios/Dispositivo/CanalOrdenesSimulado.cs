using System.Text.Json;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Configuracion;
using SAGE.Web.Servicios.EnVivo;
using SAGE.Web.Servicios.Ordenes;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Abstract Factory — rol: producto concreto
internal sealed class CanalOrdenesSimulado(
    CanalOrdenesSimulado.Registro registro, IUsuarioActual usuario, EstadoDispositivosEnVivo enVivo) : ICanalOrdenes
{
    internal sealed class Registro
    {
        public Dictionary<int, List<Orden>> Ordenes { get; } = new();
        public long UltimoId { get; set; }
    }

    public Task<Orden> EnviarModoAsync(int dispositivoId, string modo)
    {
        var valor = (modo ?? string.Empty).Trim().ToUpperInvariant();
        if (!CanalOrdenesBD.ModosValidos.Contains(valor))
            throw new ArgumentException("El modo debe ser AUTO, SOLAR o CARGADOR.", nameof(modo));

        var ahora = DateTime.UtcNow;
        var hora = Formato.ALocal(ahora).TimeOfDay;
        var rechazada = valor == "SOLAR" && (hora < TimeSpan.FromHours(8) || hora >= TimeSpan.FromHours(17));
        return ResolverAsync(dispositivoId, Orden.TipoModo, JsonSerializer.Serialize(valor), ahora,
            rechazada ? "solar_no_disponible" : null);
    }

    public Task<Orden> EnviarParametrosAsync(int dispositivoId, ParametrosFuente p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return ResolverAsync(dispositivoId, Orden.TipoParametros, p.AJsonOrden(), DateTime.UtcNow,
            p.Validar().Count == 0 ? null : "parametros_invalidos");
    }

    public Task<IReadOnlyList<Orden>> UltimasAsync(int dispositivoId, int cantidad)
    {
        lock (registro.Ordenes)
        {
            IReadOnlyList<Orden> ordenes = registro.Ordenes.TryGetValue(dispositivoId, out var lista)
                ? lista.TakeLast(Math.Clamp(cantidad, 1, 200)).Reverse().ToList()
                : [];
            return Task.FromResult(ordenes);
        }
    }

    private Task<Orden> ResolverAsync(int dispositivoId, string tipo, string valorJson, DateTime ahora, string? motivo)
    {
        var usuarioId = usuario.UsuarioId
            ?? throw new UnauthorizedAccessException("No hay un usuario autenticado.");
        var orden = new Orden
        {
            DispositivoId = dispositivoId,
            UsuarioId = usuarioId,
            Usuario = new ApplicationUser { NombreCompleto = usuario.NombreCompleto },
            Tipo = tipo,
            ValorJson = valorJson,
            Creada = ahora
        };
        orden.Entregar(ahora);
        orden.Resolver(motivo is null ? Orden.EstadoEjecutada : Orden.EstadoRechazada, motivo, ahora);

        lock (registro.Ordenes)
        {
            orden.Id = ++registro.UltimoId;
            if (!registro.Ordenes.TryGetValue(dispositivoId, out var lista))
                registro.Ordenes[dispositivoId] = lista = [];

            lista.Add(orden);
            if (lista.Count > 200)
                lista.RemoveAt(0);
        }

        enVivo.NotificarOrdenes(dispositivoId);
        return Task.FromResult(orden);
    }
}
