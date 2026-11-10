using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Berruti.Data.Migrations
{
    /// <inheritdoc />
    public partial class HorarioServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los servicios existentes se convierten sin perderse: primero columnas nulas, luego el cálculo.
            // Si la llegada no es posterior a la salida, se interpreta como llegada del día siguiente.
            migrationBuilder.AddColumn<DateTime>(
                name: "salida",
                table: "servicio",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "llegada",
                table: "servicio",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql("UPDATE servicio SET salida = TIMESTAMP(fecha, hora_salida), llegada = TIMESTAMP(fecha, hora_llegada) + INTERVAL IF(hora_llegada <= hora_salida, 1, 0) DAY;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "salida",
                table: "servicio",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "llegada",
                table: "servicio",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "fecha",
                table: "servicio");

            migrationBuilder.DropColumn(
                name: "hora_llegada",
                table: "servicio");

            migrationBuilder.DropColumn(
                name: "hora_salida",
                table: "servicio");

            migrationBuilder.AddCheckConstraint(
                name: "ck_servicio_llegada",
                table: "servicio",
                sql: "llegada > salida");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_servicio_llegada",
                table: "servicio");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha",
                table: "servicio",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "hora_salida",
                table: "servicio",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "hora_llegada",
                table: "servicio",
                type: "time",
                nullable: true);

            migrationBuilder.Sql("UPDATE servicio SET fecha = DATE(salida), hora_salida = TIME(salida), hora_llegada = TIME(llegada);");

            migrationBuilder.AlterColumn<DateTime>(
                name: "fecha",
                table: "servicio",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "hora_salida",
                table: "servicio",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "hora_llegada",
                table: "servicio",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "llegada",
                table: "servicio");

            migrationBuilder.DropColumn(
                name: "salida",
                table: "servicio");
        }
    }
}
