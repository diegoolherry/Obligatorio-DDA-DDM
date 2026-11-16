using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParadasSalidaServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "orden_desembarque",
                table: "servicio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "orden_embarque",
                table: "servicio",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "parada_salida",
                columns: table => new
                {
                    salida_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    parada_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parada_salida", x => new { x.salida_id, x.orden });
                    table.CheckConstraint("ck_parada_salida_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "fk_parada_salida_parada",
                        column: x => x.parada_id,
                        principalTable: "parada",
                        principalColumn: "parada_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_parada_salida_salida",
                        column: x => x.salida_id,
                        principalTable: "salida",
                        principalColumn: "salida_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_servicio_salida_id_orden_desembarque",
                table: "servicio",
                columns: new[] { "salida_id", "orden_desembarque" });

            migrationBuilder.CreateIndex(
                name: "IX_servicio_salida_id_orden_embarque",
                table: "servicio",
                columns: new[] { "salida_id", "orden_embarque" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_servicio_rango_paradas",
                table: "servicio",
                sql: "((orden_embarque IS NULL AND orden_desembarque IS NULL) OR (salida_id IS NOT NULL AND orden_embarque IS NOT NULL AND orden_desembarque IS NOT NULL AND orden_embarque < orden_desembarque))");

            migrationBuilder.CreateIndex(
                name: "IX_parada_salida_parada_id",
                table: "parada_salida",
                column: "parada_id");

            migrationBuilder.AddForeignKey(
                name: "fk_servicio_parada_desembarque",
                table: "servicio",
                columns: new[] { "salida_id", "orden_desembarque" },
                principalTable: "parada_salida",
                principalColumns: new[] { "salida_id", "orden" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_servicio_parada_embarque",
                table: "servicio",
                columns: new[] { "salida_id", "orden_embarque" },
                principalTable: "parada_salida",
                principalColumns: new[] { "salida_id", "orden" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_servicio_parada_desembarque",
                table: "servicio");

            migrationBuilder.DropForeignKey(
                name: "fk_servicio_parada_embarque",
                table: "servicio");

            migrationBuilder.DropTable(
                name: "parada_salida");

            migrationBuilder.DropIndex(
                name: "IX_servicio_salida_id_orden_desembarque",
                table: "servicio");

            migrationBuilder.DropIndex(
                name: "IX_servicio_salida_id_orden_embarque",
                table: "servicio");

            migrationBuilder.DropCheckConstraint(
                name: "ck_servicio_rango_paradas",
                table: "servicio");

            migrationBuilder.DropColumn(
                name: "orden_desembarque",
                table: "servicio");

            migrationBuilder.DropColumn(
                name: "orden_embarque",
                table: "servicio");
        }
    }
}
