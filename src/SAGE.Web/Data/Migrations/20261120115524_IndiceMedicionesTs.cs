using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceMedicionesTs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Mediciones_DispositivoId_Ts",
                table: "Mediciones",
                columns: new[] { "DispositivoId", "Ts" })
                .Annotation("SqlServer:Include", new[] { "PSolMw", "PRedMw", "WhSolAcum", "WhRedAcum", "BootId", "FuenteSeleccionada" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mediciones_DispositivoId_Ts",
                table: "Mediciones");
        }
    }
}
