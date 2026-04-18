using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMercadoPagoPaymentWebhookTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_event_dedup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_event_dedup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferenceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_links_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_status_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_status_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_webhook_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidationOutcome = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_webhook_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_event_dedup_ProviderResourceId_Action",
                table: "payment_event_dedup",
                columns: new[] { "ProviderResourceId", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_links_OrderId",
                table: "payment_links",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_links_PreferenceId",
                table: "payment_links",
                column: "PreferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_events_OrderId",
                table: "payment_status_events",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_events_ProviderResourceId",
                table: "payment_status_events",
                column: "ProviderResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_ProviderResourceId",
                table: "payment_webhook_logs",
                column: "ProviderResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_RequestId",
                table: "payment_webhook_logs",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_event_dedup");

            migrationBuilder.DropTable(
                name: "payment_links");

            migrationBuilder.DropTable(
                name: "payment_status_events");

            migrationBuilder.DropTable(
                name: "payment_webhook_logs");
        }
    }
}
