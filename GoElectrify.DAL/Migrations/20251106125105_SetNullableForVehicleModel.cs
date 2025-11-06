using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SetNullableForVehicleModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "vehicle_model_id",
                table: "Bookings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "vehicle_model_id",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
