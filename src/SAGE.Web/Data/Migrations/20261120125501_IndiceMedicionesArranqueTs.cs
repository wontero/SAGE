using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceMedicionesArranqueTs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Mediciones_DispositivoId_BootId_Ts",
                table: "Mediciones",
                columns: new[] { "DispositivoId", "BootId", "Ts" })
                .Annotation("SqlServer:Include", new[] { "WhSolAcum", "WhRedAcum" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mediciones_DispositivoId_BootId_Ts",
                table: "Mediciones");
        }
    }
}
