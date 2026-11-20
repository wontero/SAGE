using ClosedXML.Excel;
using SAGE.Web.Data;
using SAGE.Web.Servicios;
using SAGE.Web.Servicios.Energia;
using SAGE.Web.Servicios.Reportes;

namespace SAGE.Tests;

public class HistorialExcelTests
{
    [Theory]
    [InlineData(false, "Medido")]
    [InlineData(true, "SIMULADO")]
    public void ResumenYMedicionesConservanValoresRealesYEscalados(bool simulado, string origen)
    {
        var inicio = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        var anterior = new Medicion { Ts = inicio.AddSeconds(-1), BootId = 1, WhSolAcum = 10m, WhRedAcum = 3m };
        var primera = new Medicion { Ts = inicio, BootId = 1, WhSolAcum = 11m, WhRedAcum = 4m,
            FuenteSeleccionada = "SOLAR", PSolMw = 967m, VSol = 4.93m, ISolMa = 196.2m };
        var ultima = new Medicion { Ts = inicio.AddSeconds(5), BootId = 1, WhSolAcum = 13m, WhRedAcum = 4m };
        Medicion[] mediciones = [ultima, primera];
        Evento[] eventos = [new() { Ts = ultima.Ts, Tipo = "conmutacion", De = "SOLAR", A = "CARGADOR" }];
        var energia = CalculadoraEnergia.CalcularMuestras(mediciones, [anterior], eventos.Length);

        using var archivo = new MemoryStream();
        HistorialExcel.Escribir(archivo, "Maqueta", inicio, ultima.Ts, 2000m, energia, mediciones, eventos, simulado);
        archivo.Position = 0;
        using var libro = new XLWorkbook(archivo);
        var resumen = libro.Worksheet("Resumen");
        var hoja = libro.Worksheet("Mediciones");

        Assert.Equal(3, libro.Worksheets.Count);
        Assert.Equal(origen, resumen.Cell("B5").GetString());
        Assert.Equal(3m, resumen.Cell("B7").GetValue<decimal>());
        Assert.Equal(Formato.Energia(energia.WhSolar, 2000m), Formato.Energia(resumen.Cell("C7").GetValue<decimal>()));
        Assert.Equal(1m, resumen.Cell("B8").GetValue<decimal>());
        Assert.Equal(Formato.Energia(energia.WhRed, 2000m), Formato.Energia(resumen.Cell("C8").GetValue<decimal>()));
        Assert.Equal(0.75m, resumen.Cell("B9").GetValue<decimal>());
        Assert.Equal(5, resumen.Cell("B10").GetValue<int>());
        Assert.Equal(0, resumen.Cell("B11").GetValue<int>());
        Assert.Equal(1, resumen.Cell("B12").GetValue<int>());
        Assert.Equal(3, hoja.LastRowUsed()!.RowNumber());
        Assert.Equal("20/11/2026 08:00:00", hoja.Cell("A2").GetString());
        Assert.Equal(4.93m, hoja.Cell("B2").GetValue<decimal>());
        Assert.Equal(196.2m, hoja.Cell("C2").GetValue<decimal>());
        Assert.Equal(0.967m, hoja.Cell("D2").GetValue<decimal>());
        Assert.Equal(1934m, hoja.Cell("E2").GetValue<decimal>());
        Assert.Equal("Solar → Red", libro.Worksheet("Eventos").Cell("C2").GetString());
    }
}
