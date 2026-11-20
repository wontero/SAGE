using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Abstract Factory — rol: producto abstracto
public interface ILectorTelemetria
{
    Task<IReadOnlyList<Medicion>> MedicionesAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo);
    Task<IReadOnlyList<Evento>> EventosAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc);

    internal Task<IReadOnlyList<Medicion>> MedicionesRecibidasAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo) =>
        MedicionesAsync(dispositivoId, desdeUtc, hastaUtc, maximo);

    internal Task<IReadOnlyList<PuntoPotenciaHistorial>> PuntosAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc) => throw new NotSupportedException();
}
