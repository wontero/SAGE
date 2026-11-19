using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarParametrosDispositivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParametrosJson",
                table: "Dispositivos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParametrosVersion",
                table: "Dispositivos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParametrosVersionConfirmada",
                table: "Dispositivos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParametrosJson",
                table: "Dispositivos");

            migrationBuilder.DropColumn(
                name: "ParametrosVersion",
                table: "Dispositivos");

            migrationBuilder.DropColumn(
                name: "ParametrosVersionConfirmada",
                table: "Dispositivos");
        }
    }
}
