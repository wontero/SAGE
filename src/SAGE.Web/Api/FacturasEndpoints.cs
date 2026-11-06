using ClosedXML.Excel;

namespace SAGE.Web.Api;

public static class FacturasEndpoints
{
    public static IEndpointRouteBuilder MapFacturasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Plantilla .xlsx para importar facturas (solo Administrador y Técnico).
        endpoints.MapGet("/facturas/plantilla", () =>
            {
                using var libro = new XLWorkbook();
                var hoja = libro.Worksheets.Add("Facturas");

                hoja.Cell(1, 1).Value = "Año";
                hoja.Cell(1, 2).Value = "Mes";
                hoja.Cell(1, 3).Value = "Consumo (kWh)";
                hoja.Cell(1, 4).Value = "Monto (Bs)";
                hoja.Row(1).Style.Font.Bold = true;

                hoja.Cell(2, 1).Value = 2026;
                hoja.Cell(2, 2).Value = 1;
                hoja.Cell(2, 3).Value = 1250.5;
                hoja.Cell(2, 4).Value = 1480.75;

                hoja.Columns().AdjustToContents();

                using var memoria = new MemoryStream();
                libro.SaveAs(memoria);

                return Results.File(
                    memoria.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "plantilla_facturas.xlsx");
            })
            .RequireAuthorization(politica => politica.RequireRole("Administrador", "Tecnico"));

        return endpoints;
    }
}
