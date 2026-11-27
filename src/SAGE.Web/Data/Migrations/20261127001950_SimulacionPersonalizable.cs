using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimulacionPersonalizable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Detalle",
                table: "Simulaciones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Instaladoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BsPorKwp = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MantenimientoBsAnio = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instaladoras", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Instaladoras");

            migrationBuilder.DropColumn(
                name: "Detalle",
                table: "Simulaciones");
        }
    }
}
