using System.Globalization;

namespace SAGE.Web.Servicios;

public static class Formato
{
    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-BO");

    private static readonly TimeZoneInfo ZonaBolivia =
        ObtenerZonaBolivia();

    private static TimeZoneInfo ObtenerZonaBolivia()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SA Western Standard Time");
        }
    }

    public static string HoraLocal(DateTime utc)
    {
        var fecha = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(fecha, ZonaBolivia);

        return local.ToString("dd/MM/yyyy HH:mm:ss", Cultura);
    }

    // S23-M12 (Ethan): "dd/MM HH:mm" en hora de Bolivia.
    public static string FechaCorta(DateTime utc)
    {
        return ALocal(utc).ToString("dd/MM HH:mm", Cultura);
    }

    // S23-M12 (Ethan): UTC → hora de Bolivia (para formularios de fecha y hora).
    public static DateTime ALocal(DateTime utc)
    {
        var fecha = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(fecha, ZonaBolivia),
            DateTimeKind.Unspecified);
    }

    // S23-M12 (Ethan): hora de Bolivia escrita en un formulario → UTC (para guardar).
    public static DateTime DesdeLocal(DateTime local)
    {
        var fecha = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(fecha, ZonaBolivia);
    }

    // S23-M12 (Ethan): duración "1 h 05 min", "2 min 10 s", "45 s".
    public static string Duracion(TimeSpan duracion)
    {
        if (duracion < TimeSpan.Zero)
            duracion = TimeSpan.Zero;

        if (duracion.TotalHours >= 1)
            return $"{(int)duracion.TotalHours} h {duracion.Minutes:00} min";

        if (duracion.TotalMinutes >= 1)
            return $"{(int)duracion.TotalMinutes} min {duracion.Seconds:00} s";

        return $"{(int)duracion.TotalSeconds} s";
    }

    public static string Potencia(decimal mw, decimal factor = 1m)
    {
        var watts = mw * factor / 1000m;

        return watts < 1000m
            ? watts.ToString("N1", Cultura) + " W"
            : (watts / 1000m).ToString("N2", Cultura) + " kW";
    }

    public static string Energia(decimal wh, decimal factor = 1m)
    {
        var energia = wh * factor;

        return energia < 1000m
            ? energia.ToString("N2", Cultura) + " Wh"
            : (energia / 1000m).ToString("N3", Cultura) + " kWh";
    }

    public static string Tension(decimal v)
    {
        return v.ToString("N2", Cultura) + " V";
    }

    public static string Porcentaje(decimal valor) => valor.ToString("N1", Cultura) + " %";

    public static string Corriente(decimal ma)
    {
        return ma.ToString("N1", Cultura) + " mA";
    }

    public static string Hace(DateTime? utc)
    {
        if (utc is null)
            return "Nunca";

        var fecha = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc);
        var diferencia = DateTime.UtcNow - fecha;

        if (diferencia.TotalSeconds < 60)
            return $"hace {Math.Max(0, (int)diferencia.TotalSeconds)} s";

        if (diferencia.TotalMinutes < 60)
            return $"hace {(int)diferencia.TotalMinutes} min";

        return $"hace {(int)diferencia.TotalHours} h";
    }

    public static string Fuente(string fuente)
    {
        return fuente?.ToUpperInvariant() switch
        {
            "SOLAR" => "Solar",
            "CARGADOR" => "Red",
            _ => "Desconocida"
        };
    }

    public static string Modo(string modo, string fuente)
    {
        if (modo == "SOLAR" && fuente == "CARGADOR")
            return "Solar en respaldo";

        return modo?.ToUpperInvariant() switch
        {
            "AUTO" => "Automático",
            "SOLAR" => "Solar",
            "CARGADOR" => "Red",
            _ => "Desconocido"
        };
    }
}
