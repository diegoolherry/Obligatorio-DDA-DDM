using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class EsquemaAutenticacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contador_entidad",
                columns: table => new
                {
                    prefijo = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    ultimo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contador_entidad", x => x.prefijo);
                    table.CheckConstraint("ck_contador_prefijo", "prefijo IN ('US-', 'RO-')");
                    table.CheckConstraint("ck_contador_rango", "ultimo BETWEEN 0 AND 50000");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    rol_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    nombre = table.Column<string>(type: "varchar(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.rol_id);
                    table.CheckConstraint("ck_rol_id", "rol_id REGEXP '^RO-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'");
                    table.CheckConstraint("ck_rol_nombre", "nombre IN ('Pasajero', 'Cobrador', 'Administrador')");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    usuario_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    identificador_publico = table.Column<string>(type: "varchar(43)", maxLength: 43, nullable: false),
                    correo = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false),
                    contrasena = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.usuario_id);
                    table.CheckConstraint("ck_usuario_activo", "activo IN (0, 1)");
                    table.CheckConstraint("ck_usuario_id", "usuario_id REGEXP '^US-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sesion",
                columns: table => new
                {
                    huella = table.Column<string>(type: "char(64)", nullable: false),
                    usuario_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    creada_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    vence_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revocada_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesion", x => x.huella);
                    table.CheckConstraint("ck_sesion_vence", "vence_utc = DATE_ADD(creada_utc, INTERVAL 7 DAY)");
                    table.ForeignKey(
                        name: "fk_sesion_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                columns: table => new
                {
                    usuario_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    rol_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_rol", x => new { x.usuario_id, x.rol_id });
                    table.ForeignKey(
                        name: "fk_usuario_rol_rol",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "rol_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_rol_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "uq_rol_nombre",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sesion_usuario_id",
                table: "sesion",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "uq_usuario_correo",
                table: "usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_usuario_publico",
                table: "usuario",
                column: "identificador_publico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_rol_id",
                table: "usuario_rol",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contador_entidad");

            migrationBuilder.DropTable(
                name: "sesion");

            migrationBuilder.DropTable(
                name: "usuario_rol");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
