using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddChargeAbly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ably_channel",
                table: "Chargers",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dock_secret_hash",
                table: "Chargers",
                type: "character varying(256)",
                unicode: false,
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dock_status",
                table: "Chargers",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_connected_at",
                table: "Chargers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 400,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 401,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 402,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 403,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 404,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 410,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 411,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 420,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 421,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });

            migrationBuilder.UpdateData(
                table: "Chargers",
                keyColumn: "id",
                keyValue: 422,
                columns: new[] { "ably_channel", "dock_secret_hash", "dock_status", "last_connected_at" },
                values: new object[] { null, null, "DISCONNECTED", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ably_channel",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "dock_secret_hash",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "dock_status",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "last_connected_at",
                table: "Chargers");
        }
    }
}
