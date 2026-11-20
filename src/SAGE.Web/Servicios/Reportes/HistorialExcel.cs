using System.Globalization;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Energia;
using SAGE.Web.Servicios.Telemetria;

namespace SAGE.Web.Servicios.Reportes;

internal static class HistorialExcel
{
    internal static void Escribir(Stream destino, string dispositivo, DateTime desdeUtc, DateTime hastaUtc,
        decimal factor, EnergiaPorFuente energia, IReadOnlyList<Medicion> mediciones, IReadOnlyList<Evento> eventos)
    {
        var detalles = new List<Detalle>();
        using (var libro = new XLWorkbook())
        {
            EscribirResumen(libro, dispositivo, desdeUtc, hastaUtc, factor, energia);
            PrepararHojas(libro, detalles, "Mediciones",
                ["Hora (Bolivia)", "Solar (V)", "Solar (mA)", "Solar real (W)", "Solar escalada (W)",
                 "Solar real acum. (Wh)", "Solar escalada acum. (Wh)", "Red (V)", "Red (mA)", "Red real (W)",
                 "Red escalada (W)", "Red real acum. (Wh)", "Red escalada acum. (Wh)", "Modo", "Estado", "Fuente"],
                mediciones.Count, i =>
                {
                    var m = mediciones[mediciones.Count - 1 - i];
                    return
                    [
                        Formato.HoraLocal(m.Ts), m.VSol, m.ISolMa, m.PSolMw / 1000, m.PSolMw * factor / 1000,
                        m.WhSolAcum, m.WhSolAcum * factor, m.VRed, m.IRedMa, m.PRedMw / 1000, m.PRedMw * factor / 1000,
                        m.WhRedAcum, m.WhRedAcum * factor, Formato.Modo(m.Modo, m.FuenteSeleccionada),
                        TextosEventos.Estado(m.Estado), Formato.Fuente(m.FuenteSeleccionada)
                    ];
                });

            PrepararHojas(libro, detalles, "Eventos",
                ["Hora (Bolivia)", "Tipo", "De → A", "Estado", "Motivo", "Origen", "Orden"],
                eventos.Count, i =>
                {
                    var e = eventos[i];
                    return
                    [
                        Formato.HoraLocal(e.Ts), TextosEventos.Tipo(e.Tipo), TextosEventos.DeA(e.De, e.A),
                        TextosEventos.Estado(e.Estado), TextosEventos.Motivo(e.Motivo), TextosEventos.Origen(e.Origen), e.OrdenId
                    ];
                });
            libro.SaveAs(destino);
        }

        // Las filas de una semana ocupan millones de celdas. Se escriben sin cargarlas en ClosedXML.
        destino.Position = 0;
        using var documento = SpreadsheetDocument.Open(destino, true, new OpenSettings { AutoSave = false });
        var libroXml = documento.WorkbookPart!;
        foreach (var detalle in detalles)
        {
            var hoja = libroXml.Workbook.Sheets!.Elements<Sheet>().Single(s => s.Name!.Value == detalle.Nombre);
            EscribirDetalle((WorksheetPart)libroXml.GetPartById(hoja.Id!.Value!), detalle);
        }
    }

    private static void EscribirResumen(XLWorkbook libro, string dispositivo, DateTime desdeUtc, DateTime hastaUtc,
        decimal factor, EnergiaPorFuente energia)
    {
        var resumen = libro.Worksheets.Add("Resumen");
        resumen.Cell("A1").Value = "Dispositivo";
        resumen.Cell("B1").Value = dispositivo;
        resumen.Cell("A2").Value = "Desde (Bolivia)";
        resumen.Cell("B2").Value = Formato.HoraLocal(desdeUtc);
        resumen.Cell("A3").Value = "Hasta (Bolivia)";
        resumen.Cell("B3").Value = Formato.HoraLocal(hastaUtc);
        resumen.Cell("A4").Value = "Factor de escala";
        resumen.Cell("B4").Value = factor;
        resumen.Cell("A6").InsertData(new[] { new[] { "Indicador", "Real (Wh)", "Escalada (Wh)" } });
        resumen.Range("A6:C6").Style.Font.Bold = true;
        resumen.Cell("A7").Value = "Energía solar";
        resumen.Cell("B7").Value = energia.WhSolar;
        resumen.Cell("C7").Value = energia.WhSolar * factor;
        resumen.Cell("A8").Value = "Energía red";
        resumen.Cell("B8").Value = energia.WhRed;
        resumen.Cell("C8").Value = energia.WhRed * factor;
        resumen.Cell("A9").Value = "% solar";
        resumen.Cell("B9").Value = energia.PorcentajeSolar / 100;
        resumen.Cell("B9").Style.NumberFormat.Format = "0.0%";
        resumen.Cell("A10").Value = "Tiempo en solar (s)";
        resumen.Cell("B10").Value = energia.TiempoSolar.TotalSeconds;
        resumen.Cell("A11").Value = "Tiempo en red (s)";
        resumen.Cell("B11").Value = energia.TiempoRed.TotalSeconds;
        resumen.Cell("A12").Value = "Conmutaciones";
        resumen.Cell("B12").Value = energia.Conmutaciones;
        resumen.Columns("A:C").AdjustToContents();
    }

    private static void PrepararHojas(XLWorkbook libro, List<Detalle> detalles, string nombre,
        string[] encabezados, int cantidad, Func<int, object?[]> fila)
    {
        const int filasPorHoja = XLHelper.MaxRowNumber - 1;
        var pagina = 0;
        for (var offset = 0; offset < Math.Max(1, cantidad); offset += filasPorHoja)
        {
            var hoja = libro.Worksheets.Add(++pagina == 1 ? nombre : $"{nombre} {pagina}");
            hoja.Cell(1, 1).InsertData(new[] { encabezados });
            hoja.Range(1, 1, 1, encabezados.Length).Style.Font.Bold = true;
            hoja.Columns(1, encabezados.Length).AdjustToContents(1, 1);
            hoja.SheetView.FreezeRows(1);

            var filas = Math.Min(filasPorHoja, cantidad - offset);
            if (filas > 0)
                detalles.Add(new Detalle(hoja.Name, offset, filas, fila));
        }
    }

    private static void EscribirDetalle(WorksheetPart parte, Detalle detalle)
    {
        var plantilla = parte.Worksheet;
        var encabezado = plantilla.GetFirstChild<SheetData>()!.GetFirstChild<Row>()!;
        using var escritor = OpenXmlWriter.Create(parte);
        escritor.WriteStartElement(new Worksheet());
        if (plantilla.GetFirstChild<SheetViews>() is { } vistas)
            escritor.WriteElement(vistas);
        if (plantilla.GetFirstChild<Columns>() is { } columnas)
            escritor.WriteElement(columnas);
        escritor.WriteStartElement(new SheetData());
        escritor.WriteElement(encabezado);
        for (var i = 0; i < detalle.Cantidad; i++)
        {
            escritor.WriteStartElement(new Row { RowIndex = (uint)i + 2 });
            foreach (var valor in detalle.Fila(detalle.Inicio + i))
            {
                var celda = valor switch
                {
                    null => new Cell(),
                    string texto => new Cell(new InlineString(new Text(texto))) { DataType = CellValues.InlineString },
                    IFormattable numero => new Cell(new CellValue(numero.ToString(null, CultureInfo.InvariantCulture))),
                    _ => throw new InvalidOperationException("Tipo de celda no admitido.")
                };
                escritor.WriteElement(celda);
            }
            escritor.WriteEndElement();
        }

        escritor.WriteEndElement();
        escritor.WriteEndElement();
    }

    private sealed record Detalle(string Nombre, int Inicio, int Cantidad, Func<int, object?[]> Fila);
}
