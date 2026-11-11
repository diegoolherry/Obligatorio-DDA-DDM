using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoHorarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "salida_catalogo_id",
                table: "servicio",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bloque_horario",
                columns: table => new
                {
                    bloque_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    clave = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    empresa = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    vigente_desde = table.Column<DateTime>(type: "date", nullable: true),
                    vigente_hasta = table.Column<DateTime>(type: "date", nullable: true),
                    fuente_url = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bloque_horario", x => x.bloque_id);
                    table.CheckConstraint("ck_bloque_vigencia", "vigente_hasta IS NULL OR vigente_desde IS NULL OR vigente_hasta >= vigente_desde");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "feriado",
                columns: table => new
                {
                    fecha = table.Column<DateTime>(type: "date", nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feriado", x => x.fecha);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "excepcion_calendario",
                columns: table => new
                {
                    excepcion_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    bloque_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    desde = table.Column<DateTime>(type: "date", nullable: false),
                    hasta = table.Column<DateTime>(type: "date", nullable: false),
                    tipo = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    motivo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_excepcion_calendario", x => x.excepcion_id);
                    table.CheckConstraint("ck_excepcion_rango", "hasta >= desde");
                    table.ForeignKey(
                        name: "FK_excepcion_calendario_bloque_horario_bloque_id",
                        column: x => x.bloque_id,
                        principalTable: "bloque_horario",
                        principalColumn: "bloque_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ruta",
                columns: table => new
                {
                    ruta_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    bloque_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    clave = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    familia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ruta", x => x.ruta_id);
                    table.ForeignKey(
                        name: "FK_ruta_bloque_horario_bloque_id",
                        column: x => x.bloque_id,
                        principalTable: "bloque_horario",
                        principalColumn: "bloque_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "parada_ruta",
                columns: table => new
                {
                    ruta_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    parada_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parada_ruta", x => new { x.ruta_id, x.orden });
                    table.CheckConstraint("ck_parada_ruta_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "FK_parada_ruta_parada_parada_id",
                        column: x => x.parada_id,
                        principalTable: "parada",
                        principalColumn: "parada_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parada_ruta_ruta_ruta_id",
                        column: x => x.ruta_id,
                        principalTable: "ruta",
                        principalColumn: "ruta_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "salida_catalogo",
                columns: table => new
                {
                    salida_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    ruta_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    dias = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    codigos = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    nota = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salida_catalogo", x => x.salida_id);
                    table.ForeignKey(
                        name: "FK_salida_catalogo_ruta_ruta_id",
                        column: x => x.ruta_id,
                        principalTable: "ruta",
                        principalColumn: "ruta_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "hora_salida",
                columns: table => new
                {
                    salida_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    hora = table.Column<TimeSpan>(type: "time(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hora_salida", x => new { x.salida_id, x.orden });
                    table.CheckConstraint("ck_hora_salida_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "FK_hora_salida_salida_catalogo_salida_id",
                        column: x => x.salida_id,
                        principalTable: "salida_catalogo",
                        principalColumn: "salida_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_servicio_generado",
                table: "servicio",
                columns: new[] { "salida_catalogo_id", "tramo_id", "salida" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_bloque_clave",
                table: "bloque_horario",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_excepcion_calendario_bloque_id",
                table: "excepcion_calendario",
                column: "bloque_id");

            migrationBuilder.CreateIndex(
                name: "IX_parada_ruta_parada_id",
                table: "parada_ruta",
                column: "parada_id");

            migrationBuilder.CreateIndex(
                name: "IX_ruta_bloque_id",
                table: "ruta",
                column: "bloque_id");

            migrationBuilder.CreateIndex(
                name: "ux_ruta_clave",
                table: "ruta",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salida_catalogo_ruta_id",
                table: "salida_catalogo",
                column: "ruta_id");

            migrationBuilder.AddForeignKey(
                name: "FK_servicio_salida_catalogo_salida_catalogo_id",
                table: "servicio",
                column: "salida_catalogo_id",
                principalTable: "salida_catalogo",
                principalColumn: "salida_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_servicio_salida_catalogo_salida_catalogo_id",
                table: "servicio");

            migrationBuilder.DropTable(
                name: "excepcion_calendario");

            migrationBuilder.DropTable(
                name: "feriado");

            migrationBuilder.DropTable(
                name: "hora_salida");

            migrationBuilder.DropTable(
                name: "parada_ruta");

            migrationBuilder.DropTable(
                name: "salida_catalogo");

            migrationBuilder.DropTable(
                name: "ruta");

            migrationBuilder.DropTable(
                name: "bloque_horario");

            migrationBuilder.DropIndex(
                name: "ux_servicio_generado",
                table: "servicio");

            migrationBuilder.DropColumn(
                name: "salida_catalogo_id",
                table: "servicio");
        }
    }
}
