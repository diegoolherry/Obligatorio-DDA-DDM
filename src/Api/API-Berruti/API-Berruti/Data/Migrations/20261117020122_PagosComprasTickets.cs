using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class PagosComprasTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad");

            migrationBuilder.AddCheckConstraint(
                name: "ck_contador_prefijo",
                table: "contador_entidad",
                sql: "prefijo IN ('US-', 'RO-', 'SA-', 'IC-', 'PG-', 'CO-', 'CS-', 'TI-')");

            migrationBuilder.InsertData(
                table: "contador_entidad",
                columns: new[] { "prefijo", "ultimo" },
                values: new object[,]
                {
                    { "PG-", 0 },
                    { "CO-", 0 },
                    { "CS-", 0 },
                    { "TI-", 0 }
                });

            migrationBuilder.CreateTable(
                name: "pago",
                columns: table => new
                {
                    pago_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    intento_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    importe_esperado = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    importe_pagado = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    estado = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    clave_creacion = table.Column<Guid>(type: "char(36)", nullable: false),
                    clave_devolucion = table.Column<Guid>(type: "char(36)", nullable: true),
                    orden_proveedor_id = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    checkout_url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true),
                    creado_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    actualizado_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ultima_verificacion_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    devolucion_solicitada_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    devolucion_confirmada_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pago", x => x.pago_id);
                    table.UniqueConstraint("ak_pago_intento", x => new { x.pago_id, x.intento_id });
                    table.CheckConstraint("ck_pago_estado", "estado IN ('Pendiente', 'Desconocido', 'Aprobado', 'Rechazado', 'DevolucionPendiente', 'Devuelto')");
                    table.CheckConstraint("ck_pago_id", "pago_id REGEXP '^PG-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'");
                    table.CheckConstraint("ck_pago_importes", "importe_esperado > 0 AND importe_pagado >= 0 AND importe_pagado <= importe_esperado");
                    table.CheckConstraint("ck_pago_moneda", "moneda = 'UYU'");
                    table.ForeignKey(
                        name: "fk_pago_intento",
                        column: x => x.intento_id,
                        principalTable: "intento_compra",
                        principalColumn: "intento_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "compra",
                columns: table => new
                {
                    compra_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    intento_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    pago_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    importe_total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    confirmado_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra", x => x.compra_id);
                    table.UniqueConstraint("ak_compra_intento_pago", x => new { x.compra_id, x.intento_id, x.pago_id });
                    table.CheckConstraint("ck_compra_id", "compra_id REGEXP '^CO-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'");
                    table.CheckConstraint("ck_compra_importe", "importe_total > 0");
                    table.ForeignKey(
                        name: "fk_compra_intento",
                        column: x => x.intento_id,
                        principalTable: "intento_compra",
                        principalColumn: "intento_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_compra_pago_intento",
                        columns: x => new { x.pago_id, x.intento_id },
                        principalTable: "pago",
                        principalColumns: new[] { "pago_id", "intento_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tramo_pago",
                columns: table => new
                {
                    pago_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    intento_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    importe = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tramo_pago", x => new { x.pago_id, x.orden });
                    table.CheckConstraint("ck_tramo_pago_importe", "importe >= 0");
                    table.CheckConstraint("ck_tramo_pago_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "fk_tramo_pago_pago_intento",
                        columns: x => new { x.pago_id, x.intento_id },
                        principalTable: "pago",
                        principalColumns: new[] { "pago_id", "intento_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tramo_pago_seleccion",
                        columns: x => new { x.intento_id, x.orden },
                        principalTable: "tramo_intento_compra",
                        principalColumns: new[] { "intento_id", "orden" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "compra_servicio",
                columns: table => new
                {
                    compra_servicio_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    compra_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    intento_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    pago_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    importe = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra_servicio", x => x.compra_servicio_id);
                    table.CheckConstraint("ck_compra_servicio_id", "compra_servicio_id REGEXP '^CS-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'");
                    table.CheckConstraint("ck_compra_servicio_importe", "importe >= 0");
                    table.CheckConstraint("ck_compra_servicio_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "fk_compra_servicio_compra_intento_pago",
                        columns: x => new { x.compra_id, x.intento_id, x.pago_id },
                        principalTable: "compra",
                        principalColumns: new[] { "compra_id", "intento_id", "pago_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_compra_servicio_tramo_pago",
                        columns: x => new { x.pago_id, x.orden },
                        principalTable: "tramo_pago",
                        principalColumns: new[] { "pago_id", "orden" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ticket",
                columns: table => new
                {
                    ticket_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    compra_servicio_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    qr_opaco = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    emitido_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket", x => x.ticket_id);
                    table.CheckConstraint("ck_ticket_id", "ticket_id REGEXP '^TI-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'");
                    table.ForeignKey(
                        name: "fk_ticket_compra_servicio",
                        column: x => x.compra_servicio_id,
                        principalTable: "compra_servicio",
                        principalColumn: "compra_servicio_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_compra_pago_id_intento_id",
                table: "compra",
                columns: new[] { "pago_id", "intento_id" });

            migrationBuilder.CreateIndex(
                name: "uq_compra_intento",
                table: "compra",
                column: "intento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_compra_pago",
                table: "compra",
                column: "pago_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compra_servicio_compra_id_intento_id_pago_id",
                table: "compra_servicio",
                columns: new[] { "compra_id", "intento_id", "pago_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compra_servicio_pago_id_orden",
                table: "compra_servicio",
                columns: new[] { "pago_id", "orden" });

            migrationBuilder.CreateIndex(
                name: "uq_compra_servicio_orden",
                table: "compra_servicio",
                columns: new[] { "compra_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pago_intento_creado",
                table: "pago",
                columns: new[] { "intento_id", "creado_utc" });

            migrationBuilder.CreateIndex(
                name: "uq_pago_clave_creacion",
                table: "pago",
                column: "clave_creacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_pago_clave_devolucion",
                table: "pago",
                column: "clave_devolucion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_pago_orden_proveedor",
                table: "pago",
                column: "orden_proveedor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_ticket_compra_servicio",
                table: "ticket",
                column: "compra_servicio_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_ticket_qr_opaco",
                table: "ticket",
                column: "qr_opaco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tramo_pago_intento_id_orden",
                table: "tramo_pago",
                columns: new[] { "intento_id", "orden" });

            migrationBuilder.CreateIndex(
                name: "IX_tramo_pago_pago_id_intento_id",
                table: "tramo_pago",
                columns: new[] { "pago_id", "intento_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("The payment and purchase migration cannot be rolled back automatically because issued PG-, CO-, CS- and TI- identities must be preserved.");
        }
    }
}
