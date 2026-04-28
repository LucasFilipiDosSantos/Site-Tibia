using Domain.Checkout;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationTests.Checkout;

public sealed class LegacyOrderCustomerLinkRepairServiceTests
{
    [Fact]
    public async Task RepairAsync_RelinksOrdersWhoseEmailMatchesAnExistingUser()
    {
        await using var fixture = await SqliteRepairFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var expectedCustomerId = Guid.NewGuid();
        var staleCustomerId = Guid.NewGuid();

        db.Users.Add(new UserAccount(expectedCustomerId, "Linked User", "linked@test.com", "hash"));

        var order = new Order(Guid.NewGuid(), staleCustomerId, "support-relink-1");
        order.SetCustomerContact("Linked User", "linked@test.com", null, "pix");
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sut = new LegacyOrderCustomerLinkRepairService(
            db,
            NullLogger<LegacyOrderCustomerLinkRepairService>.Instance);

        var result = await sut.RepairAsync();

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RelinkedCount);
        Assert.Equal(0, result.AlreadyLinkedCount);
        Assert.Equal(0, result.UnmatchedCount);

        var storedOrder = await db.Orders.SingleAsync();
        Assert.Equal(expectedCustomerId, storedOrder.CustomerId);
    }

    [Fact]
    public async Task RepairAsync_LeavesOrdersUntouchedWhenNoMatchingUserExists()
    {
        await using var fixture = await SqliteRepairFixture.CreateAsync();
        await using var db = fixture.CreateDbContext();
        var originalCustomerId = Guid.NewGuid();

        var order = new Order(Guid.NewGuid(), originalCustomerId, "support-relink-2");
        order.SetCustomerContact("Guest User", "guest@test.com", null, "pix");
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sut = new LegacyOrderCustomerLinkRepairService(
            db,
            NullLogger<LegacyOrderCustomerLinkRepairService>.Instance);

        var result = await sut.RepairAsync();

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(0, result.RelinkedCount);
        Assert.Equal(0, result.AlreadyLinkedCount);
        Assert.Equal(1, result.UnmatchedCount);

        var storedOrder = await db.Orders.SingleAsync();
        Assert.Equal(originalCustomerId, storedOrder.CustomerId);
    }

    private sealed class SqliteRepairFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        private SqliteRepairFixture(SqliteConnection connection, DbContextOptions<AppDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<SqliteRepairFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var setup = new AppDbContext(options);
            await setup.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            await setup.Database.EnsureCreatedAsync();

            return new SqliteRepairFixture(connection, options);
        }

        public AppDbContext CreateDbContext()
        {
            var context = new AppDbContext(_options);
            context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
            return context;
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
