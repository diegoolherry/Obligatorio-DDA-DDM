using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class BajaLogicaCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FIN-008: las filas existentes quedan activas.
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "salida_catalogo",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "ruta",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "bloque_horario",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activo",
                table: "salida_catalogo");

            migrationBuilder.DropColumn(
                name: "activo",
                table: "ruta");

            migrationBuilder.DropColumn(
                name: "activo",
                table: "bloque_horario");
        }
    }
}
