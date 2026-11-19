
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAGE.Web.Data.Migrations
{
    public partial class AdaptarAlertasS23M7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar las nuevas columnas.
            migrationBuilder.AddColumn<long>(
                name: "EventoId",
                table: "Alertas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Fin",
                table: "Alertas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Inicio",
                table: "Alertas",
                type: "datetime2",
                nullable: true);

            // 2. Copiar las fechas del historial existente.
            migrationBuilder.Sql(@"
                UPDATE dbo.Alertas
                SET
                    Inicio = FechaCreacion,
                    Fin = CASE
                        WHEN Activa = 1 THEN NULL
                        ELSE COALESCE(
                            FechaResolucion,
                            FechaCreacion
                        )
                    END;
            ");

            // 3. Índice para la relación con Eventos.
            migrationBuilder.CreateIndex(
                name: "IX_Alertas_EventoId",
                table: "Alertas",
                column: "EventoId");

            // 4. Relación opcional con el evento de origen.
            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_Eventos_EventoId",
                table: "Alertas",
                column: "EventoId",
                principalTable: "Eventos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_Eventos_EventoId",
                table: "Alertas");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_EventoId",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "EventoId",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "Fin",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "Inicio",
                table: "Alertas");
        }
    }
}
