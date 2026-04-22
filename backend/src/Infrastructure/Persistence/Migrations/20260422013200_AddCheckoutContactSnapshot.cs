using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260422013200_AddCheckoutContactSnapshot")]
    public partial class AddCheckoutContactSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE orders
                ADD COLUMN IF NOT EXISTS "CustomerName" character varying(256) NULL,
                ADD COLUMN IF NOT EXISTS "CustomerEmail" character varying(256) NULL,
                ADD COLUMN IF NOT EXISTS "CustomerDiscord" character varying(128) NULL,
                ADD COLUMN IF NOT EXISTS "PaymentMethod" character varying(32) NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE orders
                DROP COLUMN IF EXISTS "PaymentMethod",
                DROP COLUMN IF EXISTS "CustomerDiscord",
                DROP COLUMN IF EXISTS "CustomerEmail",
                DROP COLUMN IF EXISTS "CustomerName";
                """);
        }
    }
}
