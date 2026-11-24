using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConsumoTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "usado_utc",
                table: "ticket",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "validado_por_usuario_id",
                table: "ticket",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticket_validado_por_usuario_id",
                table: "ticket",
                column: "validado_por_usuario_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ticket_uso",
                table: "ticket",
                sql: "(usado_utc IS NULL AND validado_por_usuario_id IS NULL) OR (usado_utc IS NOT NULL AND validado_por_usuario_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_validado_por_usuario",
                table: "ticket",
                column: "validado_por_usuario_id",
                principalTable: "usuario",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ticket_validado_por_usuario",
                table: "ticket");

            migrationBuilder.DropIndex(
                name: "IX_ticket_validado_por_usuario_id",
                table: "ticket");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ticket_uso",
                table: "ticket");

            migrationBuilder.DropColumn(
                name: "usado_utc",
                table: "ticket");

            migrationBuilder.DropColumn(
                name: "validado_por_usuario_id",
                table: "ticket");
        }
    }
}
