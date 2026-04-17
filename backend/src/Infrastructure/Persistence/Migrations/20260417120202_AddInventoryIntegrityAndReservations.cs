using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryIntegrityAndReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIntentKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReservationExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleasedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_reservations_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_stocks",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_stocks", x => x.ProductId);
                    table.CheckConstraint("CK_inventory_stocks_reserved_le_total", "\"ReservedQuantity\" <= \"TotalQuantity\"");
                    table.CheckConstraint("CK_inventory_stocks_reserved_non_negative", "\"ReservedQuantity\" >= 0");
                    table.CheckConstraint("CK_inventory_stocks_total_non_negative", "\"TotalQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_inventory_stocks_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustment_audits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    BeforeQuantity = table.Column<int>(type: "integer", nullable: false),
                    AfterQuantity = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    AdjustedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_adjustment_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_adjustment_audits_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_OrderIntentKey_ProductId",
                table: "inventory_reservations",
                columns: new[] { "OrderIntentKey", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_ProductId",
                table: "inventory_reservations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_audits_ProductId",
                table: "stock_adjustment_audits",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_reservations");

            migrationBuilder.DropTable(
                name: "inventory_stocks");

            migrationBuilder.DropTable(
                name: "stock_adjustment_audits");
        }
    }
}
