using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChargePoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChargePointId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProtocolVersion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "1.6"),
                    Vendor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Offline"),
                    ConnectorCount = table.Column<int>(type: "integer", nullable: true),
                    HeartbeatInterval = table.Column<int>(type: "integer", nullable: true),
                    MeterType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MeterSerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Iccid = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Imsi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastBootTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConnectorId = table.Column<int>(type: "integer", nullable: false),
                    StartTimestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StopTimestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StopValueTimestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MeterStart = table.Column<double>(type: "double precision", nullable: true),
                    MeterStop = table.Column<double>(type: "double precision", nullable: true),
                    MeterValue = table.Column<double>(type: "double precision", nullable: true),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ParentIdTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChargePointId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_ChargePoints_ChargePointId",
                        column: x => x.ChargePointId,
                        principalTable: "ChargePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Connectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectorId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Available"),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StatusTimestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    MeterValue = table.Column<double>(type: "double precision", nullable: true),
                    ChargePointId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Connectors_ChargePoints_ChargePointId",
                        column: x => x.ChargePointId,
                        principalTable: "ChargePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Connectors_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MeterValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Context = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Measurand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phase = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Location = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeterValues_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_ChargePointId",
                table: "ChargePoints",
                column: "ChargePointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_ProtocolVersion",
                table: "ChargePoints",
                column: "ProtocolVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_SerialNumber",
                table: "ChargePoints",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_Status",
                table: "ChargePoints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Connectors_ChargePointId_ConnectorId",
                table: "Connectors",
                columns: new[] { "ChargePointId", "ConnectorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connectors_Status",
                table: "Connectors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Connectors_TransactionId",
                table: "Connectors",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeterValues_Timestamp",
                table: "MeterValues",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_MeterValues_TransactionId",
                table: "MeterValues",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterValues_TransactionId_Measurand",
                table: "MeterValues",
                columns: new[] { "TransactionId", "Measurand" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ChargePointId",
                table: "Transactions",
                column: "ChargePointId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_IdTag",
                table: "Transactions",
                column: "IdTag");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_StartTimestamp",
                table: "Transactions",
                column: "StartTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status",
                table: "Transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionId",
                table: "Transactions",
                column: "TransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connectors");

            migrationBuilder.DropTable(
                name: "MeterValues");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "ChargePoints");
        }
    }
}
