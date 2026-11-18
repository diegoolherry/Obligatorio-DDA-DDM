using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuitarSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sesion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sesion",
                columns: table => new
                {
                    huella = table.Column<string>(type: "char(64)", nullable: false),
                    creada_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revocada_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    usuario_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    vence_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_sesion_usuario_id",
                table: "sesion",
                column: "usuario_id");
        }
    }
}
