using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260422055500_AddProductionReadIndexes")]
public partial class AddProductionReadIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_products_IsHidden_CategorySlug"
            ON products ("IsHidden", "CategorySlug");

            CREATE INDEX IF NOT EXISTS "IX_products_IsHidden_CreatedAtUtc"
            ON products ("IsHidden", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_orders_IsHidden_CreatedAtUtc"
            ON orders ("IsHidden", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_orders_IsHidden_Status"
            ON orders ("IsHidden", "Status");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_orders_IsHidden_Status";
            DROP INDEX IF EXISTS "IX_orders_IsHidden_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_products_IsHidden_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_products_IsHidden_CategorySlug";
            """);
    }
}
