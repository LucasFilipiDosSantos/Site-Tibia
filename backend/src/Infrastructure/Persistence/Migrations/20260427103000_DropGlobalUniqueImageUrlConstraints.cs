using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260427103000_DropGlobalUniqueImageUrlConstraints")]
public partial class DropGlobalUniqueImageUrlConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            DECLARE
                index_name text;
                constraint_name text;
            BEGIN
                FOR index_name IN
                    SELECT indexname
                    FROM pg_indexes
                    WHERE schemaname = current_schema()
                      AND tablename = 'products'
                      AND indexdef ILIKE 'CREATE UNIQUE INDEX%'
                      AND indexdef ILIKE '%("ImageUrl")%'
                LOOP
                    EXECUTE format('DROP INDEX IF EXISTS %I', index_name);
                END LOOP;

                FOR constraint_name IN
                    SELECT tc.constraint_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.constraint_column_usage ccu
                      ON tc.constraint_name = ccu.constraint_name
                     AND tc.table_schema = ccu.table_schema
                    WHERE tc.table_schema = current_schema()
                      AND tc.table_name = 'products'
                      AND tc.constraint_type = 'UNIQUE'
                      AND ccu.column_name = 'ImageUrl'
                LOOP
                    EXECUTE format('ALTER TABLE products DROP CONSTRAINT IF EXISTS %I', constraint_name);
                END LOOP;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
