
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Configuracion;

namespace SAGE.Web.Servicios.Alertas;

public sealed class ServicioAlertas
{
    private readonly ApplicationDbContext _db;
    private readonly ParametrosFuente _parametros;

    public ServicioAlertas(ApplicationDbContext db)
    {
        _db = db;
        _parametros = ParametrosFuente.Predeterminados;
    }

    public async Task<bool> EvaluarAsync(
        Dispositivo dispositivo,
        Medicion? medicion,
        IReadOnlyList<Evento> eventosNuevos,
        CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var huboCambios = false;

        // Recuperar las alertas abiertas del dispositivo.
        var abiertas = await _db.Alertas
            .Where(a =>
                a.DispositivoId == dispositivo.Id &&
                a.Activa)
            .ToListAsync(ct);

        foreach (var evento in eventosNuevos
                     .OrderBy(e => e.Seq))
        {
            switch (evento.Motivo)
            {
                case "solar_insuficiente":
                    huboCambios |= Abrir(
                        "solar_insuficiente",
                        evento,
                        "Solar insuficiente.");
                    break;

                case "solar_comprobada":
                    huboCambios |= Cerrar(
                        "solar_insuficiente");
                    break;

                case "sin_fuente_valida":
                    huboCambios |= Abrir(
                        "sin_fuente_valida",
                        evento,
                        "Ninguna fuente de alimentación válida.");
                    break;

                case "cargador_sin_tension":
                    huboCambios |= Abrir(
                        "cargador_sin_tension",
                        evento,
                        "Red sin tensión.");
                    break;
            }
        }

        if (medicion is not null)
        {
            // Cerrar cuando el dispositivo recupera
            // un estado con alimentación.
            if (!string.Equals(
                    medicion.Estado,
                    "SIN_ALIMENTACION",
                    StringComparison.OrdinalIgnoreCase))
            {
                huboCambios |= Cerrar(
                    "sin_fuente_valida");
            }

            // Cerrar cuando se recupera la tensión.
            if (medicion.VRed >= _parametros.VMin)
            {
                huboCambios |= Cerrar(
                    "cargador_sin_tension");
            }

            // Una nueva medición confirma la reconexión.
            huboCambios |= Cerrar("sin_conexion");
        }

        return huboCambios;

        bool Abrir(
            string tipo,
            Evento? evento,
            string mensaje)
        {
            if (BuscarAbierta(tipo) is not null)
                return false;

            var alerta = new Alerta
            {
                DispositivoId = dispositivo.Id,
                Tipo = tipo,
                Mensaje = mensaje,

                Inicio = ahora,
                Fin = null,
                Evento = evento,

                // Compatibilidad temporal.
                Activa = true,
                FechaCreacion = ahora,
                FechaResolucion = null
            };

            _db.Alertas.Add(alerta);
            abiertas.Add(alerta);

            return true;
        }

        bool Cerrar(string tipo)
        {
            var alerta = BuscarAbierta(tipo);

            if (alerta is null)
                return false;

            alerta.Fin = ahora;

            // Compatibilidad temporal.
            alerta.Activa = false;
            alerta.FechaResolucion = ahora;

            abiertas.Remove(alerta);

            return true;
        }

        Alerta? BuscarAbierta(string tipo)
        {
            return abiertas.FirstOrDefault(a =>
                NormalizarTipo(a.Tipo) == tipo &&
                a.Activa);
        }
    }

    // Reconoce temporalmente los nombres de las
    // alertas anteriores durante la migración.
    private static string NormalizarTipo(string tipo)
    {
        return tipo.ToLowerInvariant() switch
        {
            "red_sin_tension" => "cargador_sin_tension",
            "ninguna_fuente_valida" => "sin_fuente_valida",
            var valor => valor
        };
    }
}
