using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixConsumed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_charger_id_scheduled_start",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_charger_id_scheduled_start",
                table: "Bookings",
                columns: new[] { "charger_id", "scheduled_start" },
                unique: true,
                filter: "charger_id IS NOT NULL AND status IN ('PENDING','CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_charger_id_scheduled_start",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_charger_id_scheduled_start",
                table: "Bookings",
                columns: new[] { "charger_id", "scheduled_start" },
                unique: true,
                filter: "charger_id IS NOT NULL AND status IN ('PENDING','CONFIRMED','CONSUMED')");
        }
    }
}
