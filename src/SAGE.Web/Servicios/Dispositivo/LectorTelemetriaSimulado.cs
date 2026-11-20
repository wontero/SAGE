using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Dispositivo;

// Patrón: Abstract Factory — rol: producto concreto
internal sealed class LectorTelemetriaSimulado : ILectorTelemetria
{
    public Task<IReadOnlyList<Medicion>> MedicionesAsync(
        int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo) =>
        Task.FromResult<IReadOnlyList<Medicion>>(Generar(dispositivoId, desdeUtc, hastaUtc, maximo).ToList());

    public Task<IReadOnlyList<Evento>> EventosAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc) =>
        Task.FromResult<IReadOnlyList<Evento>>([]);

    internal static IEnumerable<Medicion> Generar(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo)
    {
        var limite = hastaUtc < DateTime.UtcNow ? hastaUtc : DateTime.UtcNow;
        var ts = new DateTime(limite.Ticks - limite.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        for (var i = 0; i < maximo && ts >= desdeUtc; i++, ts = ts.AddSeconds(-1))
        {
            var local = Formato.ALocal(ts);
            var hora = local.TimeOfDay.TotalHours;
            var horasSol = Math.Clamp(hora - 6, 0, 12);
            var angulo = Math.PI * horasSol / 12;
            var solar = hora is >= 6 and < 18;
            var corrienteSolar = solar ? (decimal)(190 * Math.Sin(angulo)) : 0m;
            var corrienteRed = solar ? 0m : 190m;
            var voltajeSolar = solar ? 4.90m : 0.30m;
            const decimal voltajeRed = 5.05m;

            yield return new Medicion
            {
                Id = ts.Ticks,
                DispositivoId = dispositivoId,
                BootId = local.Date.Ticks,
                Seq = (long)local.TimeOfDay.TotalSeconds,
                Ts = ts,
                TsServidor = ts,
                TsValida = true,
                VSol = voltajeSolar,
                ISolMa = corrienteSolar,
                PSolMw = voltajeSolar * corrienteSolar,
                WhSolAcum = 4.90m * 0.190m * (decimal)(12 / Math.PI * (1 - Math.Cos(angulo))),
                VRed = voltajeRed,
                IRedMa = corrienteRed,
                PRedMw = voltajeRed * corrienteRed,
                WhRedAcum = voltajeRed * 0.190m * (decimal)(hora - horasSol),
                Modo = "AUTO",
                Estado = solar ? "EN_SOLAR" : "EN_CARGADOR",
                FuenteSeleccionada = solar ? "SOLAR" : "CARGADOR",
                AlimentacionComprobada = true
            };
        }
    }
}
