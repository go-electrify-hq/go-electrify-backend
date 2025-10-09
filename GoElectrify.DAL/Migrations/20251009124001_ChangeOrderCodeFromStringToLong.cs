using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOrderCodeFromStringToLong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                    ALTER TABLE ""TopupIntents""
                    ALTER COLUMN ""order_code"" TYPE bigint
                    USING CASE
                    WHEN trim(""order_code"") ~ '^[0-9]+$' THEN ""order_code""::bigint
                    ELSE NULL
                    END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "order_code",
                table: "TopupIntents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldMaxLength: 128);
        }
    }
}
