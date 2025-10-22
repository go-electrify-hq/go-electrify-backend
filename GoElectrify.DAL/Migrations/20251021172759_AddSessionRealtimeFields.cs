using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRealtimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ably_channel",
                table: "ChargingSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_soc",
                table: "ChargingSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "join_code",
                table: "ChargingSessions",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_charging_sessions_join_code",
                table: "ChargingSessions",
                column: "join_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_charging_sessions_join_code",
                table: "ChargingSessions");

            migrationBuilder.DropColumn(
                name: "ably_channel",
                table: "ChargingSessions");

            migrationBuilder.DropColumn(
                name: "final_soc",
                table: "ChargingSessions");

            migrationBuilder.DropColumn(
                name: "join_code",
                table: "ChargingSessions");
        }
    }
}
