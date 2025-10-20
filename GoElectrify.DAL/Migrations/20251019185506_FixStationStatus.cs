using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixStationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Stations_Status_UPPER",
                table: "Stations");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "Stations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.UpdateData(
                table: "Stations",
                keyColumn: "id",
                keyValue: 300,
                column: "status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Stations",
                keyColumn: "id",
                keyValue: 301,
                column: "status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Stations",
                keyColumn: "id",
                keyValue: 302,
                column: "status",
                value: "Active");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stations_status_values",
                table: "Stations",
                sql: "status IN ('Active','Inactive','Maintenance')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stations_status_values",
                table: "Stations");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "Stations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Active");

            migrationBuilder.UpdateData(
                table: "Stations",
                keyColumn: "id",
                keyValue: 300,
                column: "status",
                value: "ACTIVE");

            migrationBuilder.UpdateData(
                table: "Stations",
                keyColumn: "id",
                keyValue: 301,
                column: "status",
                value: "ACTIVE");

            migrationBuilder.UpdateData(
                table: "Stations",
                keyColumn: "id",
                keyValue: 302,
                column: "status",
                value: "ACTIVE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stations_Status_UPPER",
                table: "Stations",
                sql: "status = UPPER(status)");
        }
    }
}
