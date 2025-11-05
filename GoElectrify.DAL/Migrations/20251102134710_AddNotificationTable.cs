using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    is_marker = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    marker_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    marker_value_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notif_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_marker_kind",
                table: "Notifications",
                columns: new[] { "user_id", "marker_kind" },
                unique: true,
                filter: "is_marker = TRUE AND marker_kind IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_notif_key",
                table: "Notifications",
                columns: new[] { "user_id", "notif_key" },
                unique: true,
                filter: "is_marker = FALSE AND notif_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_read_at_utc",
                table: "Notifications",
                columns: new[] { "user_id", "read_at_utc" },
                filter: "is_marker = FALSE AND read_at_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
