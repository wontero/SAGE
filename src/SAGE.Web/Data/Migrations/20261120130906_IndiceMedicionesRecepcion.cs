using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceMedicionesRecepcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Mediciones_DispositivoId_TsServidor_Id",
                table: "Mediciones",
                columns: new[] { "DispositivoId", "TsServidor", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mediciones_DispositivoId_TsServidor_Id",
                table: "Mediciones");
        }
    }
}
