using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventarioSalidaDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad");

            migrationBuilder.AddColumn<string>(
                name: "salida_id",
                table: "servicio",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "salida",
                columns: table => new
                {
                    salida_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salida", x => x.salida_id);
                    table.CheckConstraint("ck_salida_id", "salida_id REGEXP '^SA-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "asiento_salida",
                columns: table => new
                {
                    salida_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    numero_asiento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asiento_salida", x => new { x.salida_id, x.numero_asiento });
                    table.CheckConstraint("ck_asiento_salida_numero", "numero_asiento >= 1");
                    table.ForeignKey(
                        name: "fk_asiento_salida_salida",
                        column: x => x.salida_id,
                        principalTable: "salida",
                        principalColumn: "salida_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_servicio_salida_id",
                table: "servicio",
                column: "salida_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad",
                sql: "prefijo IN ('US-', 'RO-', 'SA-')");

            migrationBuilder.AddForeignKey(
                name: "fk_servicio_salida",
                table: "servicio",
                column: "salida_id",
                principalTable: "salida",
                principalColumn: "salida_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_servicio_salida",
                table: "servicio");

            migrationBuilder.DropTable(
                name: "asiento_salida");

            migrationBuilder.DropTable(
                name: "salida");

            migrationBuilder.DropIndex(
                name: "IX_servicio_salida_id",
                table: "servicio");

            migrationBuilder.DropCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad");

            migrationBuilder.DropColumn(
                name: "salida_id",
                table: "servicio");

            migrationBuilder.AddCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad",
                sql: "prefijo IN ('US-', 'RO-')");
        }
    }
}
