using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntentosCompraHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad");

            migrationBuilder.CreateTable(
                name: "intento_compra",
                columns: table => new
                {
                    intento_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    usuario_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    creado_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    hold_vence_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intento_compra", x => x.intento_id);
                    table.CheckConstraint("ck_intento_compra_plazo", "hold_vence_utc > creado_utc");
                    table.ForeignKey(
                        name: "fk_intento_compra_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tramo_intento_compra",
                columns: table => new
                {
                    intento_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    servicio_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    tramo_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    origen_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    origen_nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    destino_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    destino_nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    salida = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    llegada = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    politica_asiento = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    numero_asiento = table.Column<int>(type: "int", nullable: true),
                    salida_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true),
                    orden_embarque = table.Column<int>(type: "int", nullable: true),
                    orden_desembarque = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tramo_intento_compra", x => new { x.intento_id, x.orden });
                    table.CheckConstraint("ck_tramo_intento_asiento", "(numero_asiento IS NULL AND salida_id IS NULL AND orden_embarque IS NULL AND orden_desembarque IS NULL) OR (numero_asiento IS NOT NULL AND salida_id IS NOT NULL AND orden_embarque IS NOT NULL AND orden_desembarque IS NOT NULL AND orden_embarque < orden_desembarque)");
                    table.CheckConstraint("ck_tramo_intento_importe", "importe >= 0");
                    table.CheckConstraint("ck_tramo_intento_llegada", "llegada > salida");
                    table.CheckConstraint("ck_tramo_intento_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "fk_tramo_intento_intento",
                        column: x => x.intento_id,
                        principalTable: "intento_compra",
                        principalColumn: "intento_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tramo_intento_salida",
                        column: x => x.salida_id,
                        principalTable: "salida",
                        principalColumn: "salida_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tramo_intento_servicio",
                        column: x => x.servicio_id,
                        principalTable: "servicio",
                        principalColumn: "servicio_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "hold_asiento",
                columns: table => new
                {
                    intento_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    salida_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    numero_asiento = table.Column<int>(type: "int", nullable: false),
                    orden_embarque = table.Column<int>(type: "int", nullable: false),
                    orden_desembarque = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hold_asiento", x => new { x.intento_id, x.orden });
                    table.CheckConstraint("ck_hold_asiento_intervalo", "orden_embarque >= 1 AND orden_embarque < orden_desembarque");
                    table.CheckConstraint("ck_hold_asiento_numero", "numero_asiento >= 1");
                    table.ForeignKey(
                        name: "fk_hold_asiento_desembarque",
                        columns: x => new { x.salida_id, x.orden_desembarque },
                        principalTable: "parada_salida",
                        principalColumns: new[] { "salida_id", "orden" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hold_asiento_embarque",
                        columns: x => new { x.salida_id, x.orden_embarque },
                        principalTable: "parada_salida",
                        principalColumns: new[] { "salida_id", "orden" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hold_asiento_inventario",
                        columns: x => new { x.salida_id, x.numero_asiento },
                        principalTable: "asiento_salida",
                        principalColumns: new[] { "salida_id", "numero_asiento" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hold_asiento_seleccion",
                        columns: x => new { x.intento_id, x.orden },
                        principalTable: "tramo_intento_compra",
                        principalColumns: new[] { "intento_id", "orden" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad",
                sql: "prefijo IN ('US-', 'RO-', 'SA-', 'IC-')");

            migrationBuilder.InsertData(
                table: "contador_entidad",
                columns: new[] { "prefijo", "ultimo" },
                values: new object[] { "IC-", 0 });

            migrationBuilder.CreateIndex(
                name: "ix_hold_asiento_inventario",
                table: "hold_asiento",
                columns: new[] { "salida_id", "numero_asiento" });

            migrationBuilder.CreateIndex(
                name: "IX_hold_asiento_salida_id_orden_desembarque",
                table: "hold_asiento",
                columns: new[] { "salida_id", "orden_desembarque" });

            migrationBuilder.CreateIndex(
                name: "IX_hold_asiento_salida_id_orden_embarque",
                table: "hold_asiento",
                columns: new[] { "salida_id", "orden_embarque" });

            migrationBuilder.CreateIndex(
                name: "IX_intento_compra_usuario_id",
                table: "intento_compra",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_tramo_intento_compra_salida_id",
                table: "tramo_intento_compra",
                column: "salida_id");

            migrationBuilder.CreateIndex(
                name: "ix_tramo_intento_servicio",
                table: "tramo_intento_compra",
                column: "servicio_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("The purchase-attempt migration cannot be rolled back automatically because issued IC identities must be preserved.");
        }
    }
}
