using Application.Identity.Contracts;
using Application.Identity.Services;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;

namespace UnitTests.Identity;

public sealed class RegisterValidationAndRoleMappingTests
{
    [Fact]
    public async Task Register_InvalidEmail_ThrowsBeforeRepositoryAccess()
    {
        var users = new SpyUserRepository();
        var service = new IdentityService(
            users,
            new InMemoryRefreshSessionRepository(),
            new FakeTokenService(),
            new FakePasswordHasherService(),
            new FixedClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        );

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(new RegisterCommand("Test User", "not-an-email", "ValidPass123!"))
        );

        Assert.Equal(0, users.GetByEmailCallCount);
        Assert.Equal(0, users.AddCallCount);
    }

    [Fact]
    public void Migration_UserAccountsRoleColumn_UsesUserRoleClrType()
    {
        var assembly = typeof(AppDbContext).Assembly;
        var migrationType = assembly.GetType("Infrastructure.Persistence.Migrations.AddUserRoleEnum", throwOnError: true)!;
        var migration = (Migration)Activator.CreateInstance(migrationType)!;

        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var upMethod = migrationType.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!;
        upMethod.Invoke(migration, [migrationBuilder]);

        var createUserAccountsOperation = migrationBuilder.Operations
            .OfType<CreateTableOperation>()
            .Single(operation => operation.Name == "user_accounts");

        var roleColumn = createUserAccountsOperation.Columns
            .Single(column => column.Name == "Role");

        Assert.Equal(typeof(UserRole), roleColumn.ClrType);
    }

    private sealed class SpyUserRepository : IUserRepository
    {
        public int GetByEmailCallCount { get; private set; }
        public int AddCallCount { get; private set; }

        public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            GetByEmailCallCount++;
            return Task.FromResult<UserAccount?>(null);
        }

        public Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserAccount?>(null);
        }

        public Task AddAsync(UserAccount user, CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

