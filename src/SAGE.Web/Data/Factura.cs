using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

public enum OrigenFactura
{
    Manual,
    Excel
}

public class Factura
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public Empresa Empresa { get; set; } = default!;

    public int Anio { get; set; }

    // 1 a 12.
    public int Mes { get; set; }

    // Consumo facturado en kWh (mayor que 0).
    [Precision(18, 3)]
    public decimal ConsumoKwh { get; set; }

    // Monto facturado en Bs (mayor que 0).
    [Precision(18, 2)]
    public decimal MontoBs { get; set; }

    public OrigenFactura Origen { get; set; }

    [Required]
    public string RegistradaPorId { get; set; } = string.Empty;

    public ApplicationUser RegistradaPor { get; set; } = default!;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}

public sealed record EmpresaOpcion(int Id, string Nombre);

/// <summary>Reglas compartidas por el registro manual y la importación desde Excel.</summary>
public static class FacturaReglas
{
    public static readonly CultureInfo Cultura = CrearCultura();

    private static CultureInfo CrearCultura()
    {
        try { return new CultureInfo("es-BO"); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    public static readonly string[] Meses =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    public static string NombreMes(int mes) => mes is >= 1 and <= 12 ? Meses[mes - 1] : "?";

    public static string Periodo(int anio, int mes) => $"{NombreMes(mes)} {anio}";

    /// <summary>Fecha de hoy en Bolivia (con respaldo a la hora local del servidor).</summary>
    public static DateTime HoyLocal()
    {
        foreach (var id in new[] { "America/La_Paz", "SA Western Standard Time" })
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(id));
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return DateTime.Now;
    }

    public static bool EsPeriodoFuturo(int anio, int mes)
    {
        var hoy = HoyLocal();
        return anio > hoy.Year || (anio == hoy.Year && mes > hoy.Month);
    }

    /// <summary>Devuelve el mensaje de error o null si los valores son válidos.</summary>
    public static string? Validar(int anio, int mes, decimal consumoKwh, decimal montoBs)
    {
        if (mes < 1 || mes > 12)
            return "el mes debe estar entre 1 y 12";

        if (anio < 2000 || anio > 2100)
            return "el año no es válido";

        if (consumoKwh <= 0)
            return "el consumo (kWh) debe ser mayor que 0";

        if (montoBs <= 0)
            return "el monto debe ser mayor que 0";

        if (EsPeriodoFuturo(anio, mes))
            return "el periodo no puede ser futuro";

        return null;
    }
}
