using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChargingSessionStatusConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_charging_sessions_status_allowed",
                table: "ChargingSessions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charging_sessions_status_allowed",
                table: "ChargingSessions",
                sql: "status in ('PENDING','RUNNING','COMPLETED','ABORTED','TIMEOUT')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_charging_sessions_status_allowed",
                table: "ChargingSessions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charging_sessions_status_allowed",
                table: "ChargingSessions",
                sql: "status IN ('RUNNING','STOPPED','COMPLETED','FAILED')");
        }
    }
}
