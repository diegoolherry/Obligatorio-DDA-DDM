using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class UbicacionDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cobrador_servicio",
                columns: table => new
                {
                    usuario_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    servicio_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobrador_servicio", x => new { x.usuario_id, x.servicio_id });
                    table.ForeignKey(
                        name: "fk_cobrador_servicio_servicio",
                        column: x => x.servicio_id,
                        principalTable: "servicio",
                        principalColumn: "servicio_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cobrador_servicio_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "posicion_recibida",
                columns: table => new
                {
                    posicion_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    usuario_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    servicio_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    latitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    longitud = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    recibida_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posicion_recibida", x => x.posicion_id);
                    table.CheckConstraint("ck_posicion_latitud", "latitud BETWEEN -90 AND 90");
                    table.CheckConstraint("ck_posicion_longitud", "longitud BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "fk_posicion_asignacion",
                        columns: x => new { x.usuario_id, x.servicio_id },
                        principalTable: "cobrador_servicio",
                        principalColumns: new[] { "usuario_id", "servicio_id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_cobrador_servicio_servicio_id",
                table: "cobrador_servicio",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "IX_posicion_recibida_usuario_id_servicio_id",
                table: "posicion_recibida",
                columns: new[] { "usuario_id", "servicio_id" });

            migrationBuilder.CreateIndex(
                name: "ix_posicion_servicio_recibida",
                table: "posicion_recibida",
                columns: new[] { "servicio_id", "recibida_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "posicion_recibida");

            migrationBuilder.DropTable(
                name: "cobrador_servicio");
        }
    }
}
