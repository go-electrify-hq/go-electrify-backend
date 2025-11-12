using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddChargerIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "charger_id",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_charger_id_scheduled_start",
                table: "Bookings",
                columns: new[] { "charger_id", "scheduled_start" },
                unique: true,
                filter: "charger_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_chargers_charger_id",
                table: "Bookings",
                column: "charger_id",
                principalTable: "Chargers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_chargers_charger_id",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_charger_id_scheduled_start",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "charger_id",
                table: "Bookings");
        }
    }
}
