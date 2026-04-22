using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260422005200_RepairOrderNotificationColumns")]
    public partial class RepairOrderNotificationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE orders
                ADD COLUMN IF NOT EXISTS "NotificationAvailable" boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS "NotificationFailedReason" text NULL,
                ADD COLUMN IF NOT EXISTS "NotificationPhone" text NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE orders
                DROP COLUMN IF EXISTS "NotificationAvailable",
                DROP COLUMN IF EXISTS "NotificationFailedReason",
                DROP COLUMN IF EXISTS "NotificationPhone";
                """);
        }
    }
}
