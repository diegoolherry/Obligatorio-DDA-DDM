using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class EsquemaOperativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parada",
                columns: table => new
                {
                    parada_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parada", x => x.parada_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tramo",
                columns: table => new
                {
                    tramo_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    origen_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    destino_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tramo", x => x.tramo_id);
                    table.ForeignKey(
                        name: "FK_tramo_parada_destino_id",
                        column: x => x.destino_id,
                        principalTable: "parada",
                        principalColumn: "parada_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tramo_parada_origen_id",
                        column: x => x.origen_id,
                        principalTable: "parada",
                        principalColumn: "parada_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "servicio",
                columns: table => new
                {
                    servicio_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    tramo_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    fecha = table.Column<DateTime>(type: "date", nullable: false),
                    hora_salida = table.Column<TimeSpan>(type: "time", nullable: false),
                    hora_llegada = table.Column<TimeSpan>(type: "time", nullable: false),
                    capacidad = table.Column<int>(type: "int", nullable: false),
                    politica_asiento = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicio", x => x.servicio_id);
                    table.ForeignKey(
                        name: "FK_servicio_tramo_tramo_id",
                        column: x => x.tramo_id,
                        principalTable: "tramo",
                        principalColumn: "tramo_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tarifa",
                columns: table => new
                {
                    tarifa_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    tramo_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    vigente_desde = table.Column<DateTime>(type: "date", nullable: false),
                    vigente_hasta = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarifa", x => x.tarifa_id);
                    table.ForeignKey(
                        name: "FK_tarifa_tramo_tramo_id",
                        column: x => x.tramo_id,
                        principalTable: "tramo",
                        principalColumn: "tramo_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_servicio_tramo_id",
                table: "servicio",
                column: "tramo_id");

            migrationBuilder.CreateIndex(
                name: "IX_tarifa_tramo_id",
                table: "tarifa",
                column: "tramo_id");

            migrationBuilder.CreateIndex(
                name: "IX_tramo_destino_id",
                table: "tramo",
                column: "destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_tramo_origen_id",
                table: "tramo",
                column: "origen_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servicio");

            migrationBuilder.DropTable(
                name: "tarifa");

            migrationBuilder.DropTable(
                name: "tramo");

            migrationBuilder.DropTable(
                name: "parada");
        }
    }
}
