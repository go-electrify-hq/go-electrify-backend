using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetSocField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "target_soc",
                table: "ChargingSessions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "target_soc",
                table: "ChargingSessions");
        }
    }
}
