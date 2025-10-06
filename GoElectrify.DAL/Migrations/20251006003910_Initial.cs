using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GoElectrify.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectorTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    max_power_kw = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connector_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    image_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stations", x => x.id);
                    table.CheckConstraint("CK_Stations_Status_UPPER", "status = UPPER(status)");
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_kwh = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    model_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    max_power_kw = table.Column<int>(type: "integer", nullable: false),
                    battery_capacity_kwh = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_models", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    full_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chargers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<int>(type: "integer", nullable: false),
                    connector_type_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    power_kw = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    price_per_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chargers", x => x.id);
                    table.CheckConstraint("CK_Chargers_Status_UPPER", "status = UPPER(status)");
                    table.ForeignKey(
                        name: "fk_chargers_connector_types_connector_type_id",
                        column: x => x.connector_type_id,
                        principalTable: "ConnectorTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chargers_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "Stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelConnectorTypes",
                columns: table => new
                {
                    vehicle_model_id = table.Column<int>(type: "integer", nullable: false),
                    connector_type_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_model_connector_types", x => new { x.vehicle_model_id, x.connector_type_id });
                    table.ForeignKey(
                        name: "fk_vehicle_model_connector_types_connector_types_connector_type_id",
                        column: x => x.connector_type_id,
                        principalTable: "ConnectorTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vehicle_model_connector_types_vehicle_models_vehicle_model_id",
                        column: x => x.vehicle_model_id,
                        principalTable: "VehicleModels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    station_id = table.Column<int>(type: "integer", nullable: false),
                    connector_type_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_model_id = table.Column<int>(type: "integer", nullable: false),
                    scheduled_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    initial_soc = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "PENDING"),
                    code = table.Column<string>(type: "text", nullable: false),
                    estimated_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.CheckConstraint("CK_Bookings_Status_UPPER", "status = UPPER(status)");
                    table.ForeignKey(
                        name: "fk_bookings_connector_types_connector_type_id",
                        column: x => x.connector_type_id,
                        principalTable: "ConnectorTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "Stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bookings_vehicle_models_vehicle_model_id",
                        column: x => x.vehicle_model_id,
                        principalTable: "VehicleModels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_logins", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StationStaff",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    revoked_reason = table.Column<string>(type: "text", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_station_staff", x => x.id);
                    table.ForeignKey(
                        name: "fk_station_staff_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "Stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_station_staff_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallets", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargerLogs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    charger_id = table.Column<int>(type: "integer", nullable: false),
                    sample_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    voltage = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    current = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    power_kw = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    session_energy_kwh = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    soc_percent = table.Column<int>(type: "integer", nullable: true),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    error_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_charger_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_charger_logs_chargers_charger_id",
                        column: x => x.charger_id,
                        principalTable: "Chargers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargingSessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<int>(type: "integer", nullable: true),
                    charger_id = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    soc_start = table.Column<int>(type: "integer", nullable: false),
                    soc_end = table.Column<int>(type: "integer", nullable: true),
                    parking_minutes = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "RUNNING"),
                    energy_kwh = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    avg_power_kw = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_charging_sessions", x => x.id);
                    table.CheckConstraint("ck_charging_sessions_avg_power_non_negative", "avg_power_kw IS NULL OR avg_power_kw >= 0");
                    table.CheckConstraint("ck_charging_sessions_cost_non_negative", "cost IS NULL OR cost >= 0");
                    table.CheckConstraint("ck_charging_sessions_duration_non_negative", "duration_minutes >= 0");
                    table.CheckConstraint("ck_charging_sessions_energy_non_negative", "energy_kwh >= 0");
                    table.CheckConstraint("ck_charging_sessions_parking_non_negative", "parking_minutes IS NULL OR parking_minutes >= 0");
                    table.CheckConstraint("ck_charging_sessions_soc_range", "soc_start BETWEEN 0 AND 100 AND (soc_end IS NULL OR soc_end BETWEEN 0 AND 100)");
                    table.CheckConstraint("ck_charging_sessions_status_allowed", "status IN ('RUNNING','STOPPED','COMPLETED','FAILED')");
                    table.CheckConstraint("ck_charging_sessions_status_upper", "status = UPPER(status)");
                    table.CheckConstraint("ck_charging_sessions_timespan", "ended_at IS NULL OR ended_at >= started_at");
                    table.ForeignKey(
                        name: "fk_charging_sessions_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "Bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_charging_sessions_chargers_charger_id",
                        column: x => x.charger_id,
                        principalTable: "Chargers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<int>(type: "integer", nullable: false),
                    charger_id = table.Column<int>(type: "integer", nullable: true),
                    reported_by_station_staff_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "OPEN"),
                    response = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incidents", x => x.id);
                    table.CheckConstraint("CK_Incidents_Status_UPPER", "status = UPPER(status)");
                    table.ForeignKey(
                        name: "fk_incidents_chargers_charger_id",
                        column: x => x.charger_id,
                        principalTable: "Chargers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_incidents_station_staff_reported_by_station_staff_id",
                        column: x => x.reported_by_station_staff_id,
                        principalTable: "StationStaff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incidents_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "Stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopupIntents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wallet_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    qr_content = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    raw_webhook = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_topup_intents", x => x.id);
                    table.CheckConstraint("CK_TopupIntents_Amount_NonNegative", "amount >= 0");
                    table.CheckConstraint("CK_TopupIntents_Status_UPPER", "status = UPPER(status)");
                    table.ForeignKey(
                        name: "fk_topup_intents_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "Wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletSubscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wallet_id = table.Column<int>(type: "integer", nullable: false),
                    subscription_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "ACTIVE"),
                    remaining_kwh = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 0m),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_subscriptions", x => x.id);
                    table.CheckConstraint("CK_WalletSubscriptions_DateRange", "end_date >= start_date");
                    table.CheckConstraint("CK_WalletSubscriptions_RemainingKwh_NonNegative", "remaining_kwh >= 0");
                    table.CheckConstraint("CK_WalletSubscriptions_Status_UPPER", "status = UPPER(status)");
                    table.ForeignKey(
                        name: "fk_wallet_subscriptions_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "Subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wallet_subscriptions_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "Wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wallet_id = table.Column<int>(type: "integer", nullable: false),
                    charging_session_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.CheckConstraint("CK_Transactions_Amount_NonNegative", "amount >= 0");
                    table.CheckConstraint("CK_Transactions_Status_UPPER", "status = UPPER(status)");
                    table.CheckConstraint("CK_Transactions_Type_UPPER", "type = UPPER(type)");
                    table.ForeignKey(
                        name: "fk_transactions_charging_sessions_charging_session_id",
                        column: x => x.charging_session_id,
                        principalTable: "ChargingSessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_transactions_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "Wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ConnectorTypes",
                columns: new[] { "id", "created_at", "description", "max_power_kw", "name", "updated_at" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DC fast (Combo 1)", 200, "CSS1", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DC fast (Combo 2)", 350, "CSS2", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SAE J1772 (AC)", 7, "Type1-AC", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "IEC 62196-2 Type 2 (Mennekes)", 22, "Type2-AC", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DC fast (legacy/JDM)", 62, "CHAdeMO", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "id", "created_at", "name", "updated_at" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Driver", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Staff", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Stations",
                columns: new[] { "id", "address", "created_at", "description", "image_url", "latitude", "longitude", "name", "status", "updated_at" },
                values: new object[,]
                {
                    { 300, "7 Đ. D1, Long Thạnh Mỹ, Thủ Đức, Hồ Chí Minh 700000, Việt Nam", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nạp đầy năng lượng cho xe, sẵn sàng cho việc học! Trạm sạc xe điện hiện đại ngay trong khuôn viên Đại học FPT. Dành cho sinh viên, giảng viên và khách tham quan, giúp bạn sạc pin tiện lợi, an toàn trong giờ học và làm việc. Lựa chọn xanh cho một khuôn viên đại học thông minh.", null, 10.84167829167107m, 106.81083314772492m, "FPT University", "ACTIVE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 301, "Số 1 Lưu Hữu Phước, Đông Hoà, Dĩ An, Hồ Chí Minh, Việt Nam", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Điểm sạc lý tưởng cho cộng đồng sinh viên năng động! Trạm sạc xe điện được đặt ngay tại Nhà Văn hóa Sinh viên TP.HCM. Bạn có thể an tâm sạc đầy pin trong khi tham gia các hoạt động, học nhóm hay uống cà phê. Nhanh chóng, an toàn và cực kỳ tiện lợi.", null, 10.876244851905408m, 106.80600195446553m, "Nhà Văn hóa Sinh viên TP.HCM", "ACTIVE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 302, "TTTM Vincom Mega Mall Grand Park, 88 Phước Thiện, Long Bình, Thủ Đức, Hồ Chí Minh, Việt Nam", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mua sắm thả ga, không lo hết pin! Trạm sạc xe điện hiện đại nay đã có mặt tại Vincom Mega Mall Grand Park. Hãy sạc đầy pin cho xe trong lúc bạn và gia đình thỏa sức mua sắm, ăn uống và giải trí. Trải nghiệm tiện ích nhân đôi, cho chuyến đi thêm trọn vẹn.", null, 10.843429972631098m, 106.84260840302923m, "Vincom Mega Mall Grand Park", "ACTIVE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "id", "created_at", "duration_days", "name", "price", "total_kwh", "updated_at" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Go Spark – Basic", 360000m, 100m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Go Pulse - Family", 690000m, 200m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Go Drive – Pro", 3990000m, 1200m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Go Flow – Flexible", 190000m, 50m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "VehicleModels",
                columns: new[] { "id", "battery_capacity_kwh", "created_at", "max_power_kw", "model_name", "updated_at" },
                values: new object[,]
                {
                    { 200, 42.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "VinFast VF e34", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 201, 19.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 32, "VinFast VF 3 Eco", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 202, 22.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 37, "VinFast VF 3 Plus", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 203, 37.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 100, "VinFast VF 5 Plus", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 204, 59.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 150, "VinFast VF 6 Standard", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 205, 59.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 160, "VinFast VF 6 Plus", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 206, 75.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "VinFast VF 7 Standard", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 207, 75.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 200, "VinFast VF 7 Plus", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 208, 87.7m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 150, "VinFast VF 8 Eco", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 209, 92.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 170, "VinFast VF 8 Plus", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 210, 92.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 200, "VinFast VF 9 Eco", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 211, 123.0m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 220, "VinFast VF 9 Plus", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Chargers",
                columns: new[] { "id", "code", "connector_type_id", "created_at", "power_kw", "price_per_kwh", "station_id", "status", "updated_at" },
                values: new object[,]
                {
                    { 400, "FU-DC1", 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 150, 6500.0000m, 300, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 401, "FU-AC1", 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 22, 4500.0000m, 300, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 402, "FU-DC2", 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 150, 6500.0000m, 300, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 403, "FU-DC3", 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 150, 6500.0000m, 300, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 404, "FU-AC2", 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 22, 4500.0000m, 300, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 410, "SC-DC1", 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 200, 6500.0000m, 301, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 411, "SC-AC1", 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 22, 4500.0000m, 301, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 420, "GP-DC1", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 120, 6500.0000m, 302, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 421, "GP-CHA1", 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 50, 6000.0000m, 302, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 422, "GP-AC1", 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 22, 4500.0000m, 302, "ONLINE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "VehicleModelConnectorTypes",
                columns: new[] { "connector_type_id", "vehicle_model_id" },
                values: new object[,]
                {
                    { 1, 200 },
                    { 3, 200 },
                    { 1, 201 },
                    { 3, 201 },
                    { 1, 202 },
                    { 3, 202 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_connector_type_id",
                table: "Bookings",
                column: "connector_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_station_id_scheduled_start",
                table: "Bookings",
                columns: new[] { "station_id", "scheduled_start" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_user_id_scheduled_start",
                table: "Bookings",
                columns: new[] { "user_id", "scheduled_start" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_vehicle_model_id",
                table: "Bookings",
                column: "vehicle_model_id");

            migrationBuilder.CreateIndex(
                name: "ix_charger_logs_charger_id_sample_at",
                table: "ChargerLogs",
                columns: new[] { "charger_id", "sample_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chargers_connector_type_id",
                table: "Chargers",
                column: "connector_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_chargers_station_id_code",
                table: "Chargers",
                columns: new[] { "station_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_charging_sessions_booking_id",
                table: "ChargingSessions",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_charging_sessions_charger_id_started_at",
                table: "ChargingSessions",
                columns: new[] { "charger_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_charging_sessions_status",
                table: "ChargingSessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_connector_types_name",
                table: "ConnectorTypes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_provider_provider_user_id",
                table: "ExternalLogins",
                columns: new[] { "provider", "provider_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_user_id",
                table: "ExternalLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_charger_id",
                table: "Incidents",
                column: "charger_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_reported_by_station_staff_id",
                table: "Incidents",
                column: "reported_by_station_staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_station_id_status",
                table: "Incidents",
                columns: new[] { "station_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_token_hash",
                table: "RefreshTokens",
                columns: new[] { "user_id", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "Roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stations_latitude_longitude",
                table: "Stations",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "ix_stations_status_name",
                table: "Stations",
                columns: new[] { "status", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_station_staff_station_id",
                table: "StationStaff",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_station_staff_user_id_station_id",
                table: "StationStaff",
                columns: new[] { "user_id", "station_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_name",
                table: "Subscriptions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_topup_intents_wallet_id_created_at",
                table: "TopupIntents",
                columns: new[] { "wallet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_charging_session_id",
                table: "Transactions",
                column: "charging_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_wallet_id_created_at",
                table: "Transactions",
                columns: new[] { "wallet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role_id",
                table: "Users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_model_connector_types_connector_type_id",
                table: "VehicleModelConnectorTypes",
                column: "connector_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_model_name",
                table: "VehicleModels",
                column: "model_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallets_user_id",
                table: "Wallets",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallet_subscriptions_subscription_id",
                table: "WalletSubscriptions",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_subscriptions_wallet_id",
                table: "WalletSubscriptions",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_subscriptions_wallet_id_end_date",
                table: "WalletSubscriptions",
                columns: new[] { "wallet_id", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_subscriptions_wallet_id_start_date",
                table: "WalletSubscriptions",
                columns: new[] { "wallet_id", "start_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargerLogs");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "TopupIntents");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "VehicleModelConnectorTypes");

            migrationBuilder.DropTable(
                name: "WalletSubscriptions");

            migrationBuilder.DropTable(
                name: "StationStaff");

            migrationBuilder.DropTable(
                name: "ChargingSessions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Chargers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VehicleModels");

            migrationBuilder.DropTable(
                name: "ConnectorTypes");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
