using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "delivery_instructions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at_utc",
                table: "delivery_instructions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "delivery_instructions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_instructions_Status",
                table: "delivery_instructions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_delivery_instructions_Status",
                table: "delivery_instructions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "delivery_instructions");

            migrationBuilder.DropColumn(
                name: "completed_at_utc",
                table: "delivery_instructions");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "delivery_instructions");
        }
    }
}