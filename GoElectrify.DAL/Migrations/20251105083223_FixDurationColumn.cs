using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixDurationColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_charging_sessions_duration_non_negative",
                table: "ChargingSessions");

            migrationBuilder.RenameColumn(
                name: "duration_minutes",
                table: "ChargingSessions",
                newName: "duration_seconds");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charging_sessions_duration_non_negative",
                table: "ChargingSessions",
                sql: "duration_seconds >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_charging_sessions_duration_non_negative",
                table: "ChargingSessions");

            migrationBuilder.RenameColumn(
                name: "duration_seconds",
                table: "ChargingSessions",
                newName: "duration_minutes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charging_sessions_duration_non_negative",
                table: "ChargingSessions",
                sql: "duration_minutes >= 0");
        }
    }
}
