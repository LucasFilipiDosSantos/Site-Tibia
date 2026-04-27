using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandProductRatingPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "products",
                type: "numeric(5,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "products",
                type: "numeric(3,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,3)");
        }
    }
}
