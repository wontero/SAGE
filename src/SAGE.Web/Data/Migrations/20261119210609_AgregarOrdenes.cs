using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrdenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ordenes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispositivoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Creada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Entregada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resuelta = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ordenes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ordenes_Dispositivos_DispositivoId",
                        column: x => x.DispositivoId,
                        principalTable: "Dispositivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_OrdenId",
                table: "Eventos",
                column: "OrdenId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_DispositivoId_Estado",
                table: "Ordenes",
                columns: new[] { "DispositivoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_UsuarioId",
                table: "Ordenes",
                column: "UsuarioId");

            // Antes de esta migración no existían órdenes: cualquier OrdenId
            // guardado en Eventos no apunta a nada y rompería la FK.
            migrationBuilder.Sql(
                "UPDATE Eventos SET OrdenId = NULL WHERE OrdenId IS NOT NULL;");

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Ordenes_OrdenId",
                table: "Eventos",
                column: "OrdenId",
                principalTable: "Ordenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Ordenes_OrdenId",
                table: "Eventos");

            migrationBuilder.DropTable(
                name: "Ordenes");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_OrdenId",
                table: "Eventos");
        }
    }
}
