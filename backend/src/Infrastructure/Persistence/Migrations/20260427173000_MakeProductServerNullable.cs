using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.Migrations;

/// <summary>
/// Ensures product server is a real persisted optional field instead of a globally defaulted placeholder.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260427173000_MakeProductServerNullable")]
public partial class MakeProductServerNullable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE products
            ADD COLUMN IF NOT EXISTS "Server" character varying(64) NULL;

            ALTER TABLE products
            ALTER COLUMN "Server" DROP NOT NULL;

            ALTER TABLE products
            ALTER COLUMN "Server" DROP DEFAULT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE products
            SET "Server" = 'Nao informado'
            WHERE "Server" IS NULL;

            ALTER TABLE products
            ALTER COLUMN "Server" SET DEFAULT 'Nao informado';

            ALTER TABLE products
            ALTER COLUMN "Server" SET NOT NULL;
            """);
    }
}
