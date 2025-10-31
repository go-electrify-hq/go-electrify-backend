using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnLastPingAtOfCharger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_ping_at",
                table: "Chargers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 400,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 401,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 402,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 403,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 404,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 410,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 411,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 420,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 421,
                column: "last_ping_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 422,
                column: "last_ping_at",
                value: null);

            migrationBuilder.CreateIndex(
                name: "ix_chargers_last_ping_at",
                table: "Chargers",
                column: "last_ping_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chargers_last_ping_at",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "last_ping_at",
                table: "Chargers");
        }
    }
}
