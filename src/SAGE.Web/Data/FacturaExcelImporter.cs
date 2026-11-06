using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

/// <summary>
/// Importación .xlsx de facturas (misma lógica que el diálogo de UI).
/// </summary>
public static class FacturaExcelImporter
{
    public const long MaxBytes = 1024 * 1024;
    public const int MaxFilas = 1000;

    public sealed record Resultado(int Importadas, IReadOnlyList<string> Errores);

    public static async Task<Resultado> ImportarAsync(
        Stream archivo,
        int empresaId,
        string registradaPorId,
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();

        if (!await db.Empresas.AnyAsync(x => x.Id == empresaId && x.Activa, cancellationToken))
        {
            return new Resultado(0, ["La empresa no existe o está inactiva."]);
        }

        XLWorkbook libro;
        try
        {
            libro = new XLWorkbook(archivo);
        }
        catch (Exception)
        {
            return new Resultado(0, ["No se pudo leer el archivo. Verifica que sea un .xlsx válido."]);
        }

        var nuevas = new List<Factura>();

        using (libro)
        {
            var hoja = libro.Worksheets.FirstOrDefault();
            var ultimaFila = hoja?.LastRowUsed()?.RowNumber() ?? 0;

            if (hoja is null || ultimaFila < 2)
            {
                return new Resultado(0, ["El archivo no tiene filas de datos."]);
            }

            if (ultimaFila - 1 > MaxFilas)
            {
                return new Resultado(0, [$"El archivo tiene demasiadas filas (máximo {MaxFilas})."]);
            }

            if (!EncabezadosValidos(hoja))
            {
                return new Resultado(0, ["Los encabezados de la primera fila deben ser: Año | Mes | Consumo (kWh) | Monto (Bs)."]);
            }

            var existentes = (await db.Facturas.AsNoTracking()
                    .Where(f => f.EmpresaId == empresaId)
                    .Select(f => new { f.Anio, f.Mes })
                    .ToListAsync(cancellationToken))
                .Select(f => (f.Anio, f.Mes))
                .ToHashSet();
            var vistos = new HashSet<(int, int)>();

            for (var fila = 2; fila <= ultimaFila; fila++)
            {
                var celdas = Enumerable.Range(1, 4).Select(c => hoja.Cell(fila, c)).ToList();
                if (celdas.All(c => c.IsEmpty()))
                {
                    continue;
                }

                if (!TryEntero(celdas[0], out var anio))
                {
                    errores.Add($"Fila {fila}: el año no es un número válido.");
                    continue;
                }

                if (!TryEntero(celdas[1], out var mes))
                {
                    errores.Add($"Fila {fila}: el mes no es un número válido.");
                    continue;
                }

                if (!TryDecimal(celdas[2], out var kwh))
                {
                    errores.Add($"Fila {fila}: el consumo (kWh) no es un número válido.");
                    continue;
                }

                if (!TryDecimal(celdas[3], out var monto))
                {
                    errores.Add($"Fila {fila}: el monto no es un número válido.");
                    continue;
                }

                var motivo = FacturaReglas.Validar(anio, mes, kwh, monto);
                if (motivo is not null)
                {
                    errores.Add($"Fila {fila}: {motivo}.");
                    continue;
                }

                if (existentes.Contains((anio, mes)))
                {
                    errores.Add($"Fila {fila}: el mes {mes}/{anio} ya está registrado.");
                    continue;
                }

                if (!vistos.Add((anio, mes)))
                {
                    errores.Add($"Fila {fila}: el mes {mes}/{anio} está repetido en el archivo.");
                    continue;
                }

                nuevas.Add(new Factura
                {
                    EmpresaId = empresaId,
                    Anio = anio,
                    Mes = mes,
                    ConsumoKwh = kwh,
                    MontoBs = monto,
                    Origen = OrigenFactura.Excel,
                    RegistradaPorId = registradaPorId,
                    FechaRegistro = DateTime.UtcNow
                });
            }
        }

        if (errores.Count > 0)
        {
            return new Resultado(0, errores);
        }

        if (nuevas.Count == 0)
        {
            return new Resultado(0, ["El archivo no tiene filas de datos."]);
        }

        db.Facturas.AddRange(nuevas);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return new Resultado(0, ["No se pudo guardar: alguno de los meses ya fue registrado por otra persona. Vuelve a intentarlo."]);
        }

        return new Resultado(nuevas.Count, Array.Empty<string>());
    }

    private static bool EncabezadosValidos(IXLWorksheet hoja)
    {
        var esperados = new[]
        {
            new[] { "ano", "anio" },
            new[] { "mes" },
            new[] { "consumokwh" },
            new[] { "montobs" }
        };

        for (var i = 0; i < esperados.Length; i++)
        {
            var texto = Normalizar(hoja.Cell(1, i + 1).GetString());
            if (!esperados[i].Contains(texto))
            {
                return false;
            }
        }

        return true;
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }

    private static bool TryEntero(IXLCell celda, out int valor)
    {
        valor = 0;
        if (celda.IsEmpty() || !celda.TryGetValue<double>(out var d))
        {
            return false;
        }

        if (d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue)
        {
            return false;
        }

        valor = (int)d;
        return true;
    }

    private static bool TryDecimal(IXLCell celda, out decimal valor)
    {
        valor = 0;
        return !celda.IsEmpty() && celda.TryGetValue<decimal>(out valor);
    }
}
