using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260422004000_RepairProductPublicCatalogColumns")]
    public partial class RepairProductPublicCatalogColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE products
                ADD COLUMN IF NOT EXISTS "Rating" numeric(3,2) NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS "SalesCount" integer NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS "Server" character varying(64) NOT NULL DEFAULT 'Aurera';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE products
                DROP COLUMN IF EXISTS "Rating",
                DROP COLUMN IF EXISTS "SalesCount",
                DROP COLUMN IF EXISTS "Server";
                """);
        }
    }
}
