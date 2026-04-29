using Domain.Catalog;
using Domain.Identity;
using Domain.Inventory;
using Infrastructure.Identity.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedAdminUserAsync(dbContext, cancellationToken);

        if (dbContext.Categories.Any() || dbContext.Products.Any())
        {
            await RepairExistingCatalogAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var gold = new Category("Gold", "gold", "Gold por mundo com entrega segura.");
        var coin = new Category("Coin", "coin", "Pacotes de coin por mundo.");
        var items = new Category("Itens", "items", "Itens raros e equipamentos.");
        var characters = new Category("Personagens", "characters", "Personagens prontos para evoluir ou war.");
        var tibiaCoins = new Category("Tibia Coins", "tibia-coins", "Tibia Coins oficiais e transferencias.");
        var scripts = new Category("Scripts", "scripts", "Scripts e automacoes.");
        var macros = new Category("Macros", "macros", "Macros para gameplay assistida.");
        var services = new Category("Servicos", "services", "Servicos manuais e assistidos.");

        await dbContext.Categories.AddRangeAsync([gold, coin, items, characters, tibiaCoins, scripts, macros, services], cancellationToken);

        var products = new[]
        {
            new Product("Gold Aurera 100kk", "gold-aurera-100kk", "Entrega rapida de 100kk de gold no mundo Aurera.", 89.90m, gold.Id, gold.Slug, "Aurera", rating: 4.9m, salesCount: 1240),
            new Product("Coin Aurera 100kk", "coin-aurera-100kk", "Entrega rapida de 100kk de coin no servidor Aurera.", 89.90m, coin.Id, coin.Slug, "Aurera", rating: 4.9m, salesCount: 1240),
            new Product("Coin Aurera 50kk", "coin-aurera-50kk", "Entrega rapida de 50kk de coin no servidor Aurera.", 49.90m, coin.Id, coin.Slug, "Aurera", rating: 4.8m, salesCount: 890),
            new Product("Itens Aurera - Magic Sword", "itens-aurera-magic-sword", "Magic Sword no servidor Aurera para hunts e colecao.", 49.90m, items.Id, items.Slug, "Aurera", rating: 4.7m, salesCount: 320),
            new Product("Personagem Aurera - Elite Knight 500", "personagem-aurera-elite-knight-500", "Personagem no mundo Aurera pronto para endgame.", 1599.90m, characters.Id, characters.Slug, "Aurera", rating: 5.0m, salesCount: 45),
            new Product("Personagem Aurera - Master Sorcerer 350", "personagem-aurera-master-sorcerer-350", "Personagem no servidor Aurera pronto para hunts e war.", 899.90m, characters.Id, characters.Slug, "Aurera", rating: 4.7m, salesCount: 120),
            new Product("Servico Aurera - Annihilator", "servico-aurera-annihilator", "Servico assistido para Annihilator no servidor Aurera.", 59.90m, services.Id, services.Slug, "Aurera", rating: 4.8m, salesCount: 530),
            new Product("Servico Aurera - Acessos", "servico-aurera-acessos", "Servico de quests e acessos no servidor Aurera.", 79.90m, services.Id, services.Slug, "Aurera", rating: 4.8m, salesCount: 530),
            new Product("Macro Free", "macro-free", "Macro gratuito para gameplay assistida.", 0m, macros.Id, macros.Slug, "Nao informado", rating: 4.4m, salesCount: 1400),
            new Product("Encomenda de Macro", "encomenda-de-macro", "Encomenda personalizada de macro.", 19.90m, macros.Id, macros.Slug, "Nao informado", rating: 4.5m, salesCount: 1800),
            new Product("Encomenda de Script 100% AFK OTC", "encomenda-script-100-afk-otc", "Script sob encomenda para OTC com fluxo 100% AFK.", 79.90m, scripts.Id, scripts.Slug, "Nao informado", rating: 4.8m, salesCount: 950),
        };

        await dbContext.Products.AddRangeAsync(products, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        await dbContext.InventoryStocks.AddRangeAsync(
            products.Select(product => new InventoryStock(product.Id, totalQuantity: 50, reservedQuantity: 0, now)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task RepairExistingCatalogAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var categories = new[]
        {
            new Category("Gold", "gold", "Gold por mundo com entrega segura."),
            new Category("Coin", "coin", "Pacotes de coin por mundo."),
            new Category("Itens", "items", "Itens raros e equipamentos."),
            new Category("Personagens", "characters", "Personagens prontos para evoluir ou war."),
            new Category("Tibia Coins", "tibia-coins", "Tibia Coins oficiais e transferencias."),
            new Category("Scripts", "scripts", "Scripts e automacoes."),
            new Category("Macros", "macros", "Macros para gameplay assistida."),
            new Category("Servicos", "services", "Servicos manuais e assistidos.")
        };

        foreach (var category in categories)
        {
            var exists = await dbContext.Categories.AnyAsync(existing => existing.Slug == category.Slug, cancellationToken);
            if (!exists)
            {
                await dbContext.Categories.AddAsync(category, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var productsWithoutStock = await dbContext.Products
            .Where(product => !dbContext.InventoryStocks.Any(stock => stock.ProductId == product.Id))
            .ToListAsync(cancellationToken);

        await dbContext.InventoryStocks.AddRangeAsync(
            productsWithoutStock.Select(product => new InventoryStock(product.Id, totalQuantity: 50, reservedQuantity: 0, now)),
            cancellationToken);
    }

    private static async Task SeedAdminUserAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var passwordHasher = new PasswordHasherService();
        var admins = new[]
        {
            new
            {
                Name = "Admin Lootera",
                Email = "admin@lootera.com",
                Password = "Admin123!"
            },
            new
            {
                Name = "Suporte Lootera",
                Email = "suporte@lootera.com",
                Password = "LooteraAdmin2026!"
            }
        };

        foreach (var adminSeed in admins)
        {
            var normalizedEmail = UserAccount.NormalizeEmail(adminSeed.Email);
            var hasAdmin = await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
            if (hasAdmin)
            {
                continue;
            }

            var admin = new UserAccount(
                adminSeed.Name,
                normalizedEmail,
                passwordHasher.HashPassword(adminSeed.Password),
                UserRole.Admin);

            admin.MarkEmailVerified();
            await dbContext.Users.AddAsync(admin, cancellationToken);
        }
    }
}
