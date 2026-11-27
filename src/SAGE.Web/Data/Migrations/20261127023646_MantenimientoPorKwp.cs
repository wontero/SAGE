using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MantenimientoPorKwp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MantenimientoBsAnio",
                table: "ParametrosEconomicos",
                newName: "MantenimientoBsKwpAnio");

            migrationBuilder.RenameColumn(
                name: "MantenimientoBsAnio",
                table: "Instaladoras",
                newName: "MantenimientoBsKwpAnio");

            // 7,5 kWp conservan el costo anual original de Referencia SAGE (600 Bs a 80 Bs/kWp).
            migrationBuilder.Sql("""
                UPDATE ParametrosEconomicos
                SET MantenimientoBsKwpAnio = CASE
                    WHEN Version = (SELECT MAX(Version) FROM ParametrosEconomicos) THEN 80
                    ELSE MantenimientoBsKwpAnio / 7.5 END;

                UPDATE Instaladoras
                SET MantenimientoBsKwpAnio = CASE Nombre
                    WHEN N'Chuvolteos' THEN 95
                    WHEN N'Avatar' THEN 65
                    ELSE MantenimientoBsKwpAnio / 7.5 END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MantenimientoBsKwpAnio",
                table: "ParametrosEconomicos",
                newName: "MantenimientoBsAnio");

            migrationBuilder.RenameColumn(
                name: "MantenimientoBsKwpAnio",
                table: "Instaladoras",
                newName: "MantenimientoBsAnio");

            migrationBuilder.Sql("""
                UPDATE ParametrosEconomicos SET MantenimientoBsAnio = MantenimientoBsAnio * 7.5;

                UPDATE Instaladoras
                SET MantenimientoBsAnio = CASE
                    WHEN Nombre = N'Chuvolteos' AND MantenimientoBsAnio = 95 THEN 750
                    WHEN Nombre = N'Avatar' AND MantenimientoBsAnio = 65 THEN 450
                    ELSE MantenimientoBsAnio * 7.5 END;
                """);
        }
    }
}
