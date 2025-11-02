using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddStatus : Migration
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
                sql: "status in ('PENDING','RUNNING','COMPLETED','TIMEOUT','FAILED','ABORTED','UNPAID','PAID')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charging_sessions_status_flow",
                table: "ChargingSessions",
                sql: "CASE WHEN ended_at IS NULL THEN status IN ('PENDING','RUNNING') ELSE status IN ('COMPLETED','TIMEOUT','FAILED','ABORTED','UNPAID','PAID') END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_charging_sessions_status_allowed",
                table: "ChargingSessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_charging_sessions_status_flow",
                table: "ChargingSessions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charging_sessions_status_allowed",
                table: "ChargingSessions",
                sql: "status in ('PENDING','RUNNING','COMPLETED','ABORTED','TIMEOUT')");
        }
    }
}
