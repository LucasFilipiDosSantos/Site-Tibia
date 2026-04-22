using Domain.Catalog;

namespace Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.Categories.Any() || dbContext.Products.Any())
        {
            return;
        }

        var gold = new Category("Moedas", "gold", "Pacotes de gold para Tibia.");
        var items = new Category("Itens", "items", "Itens raros e equipamentos.");
        var characters = new Category("Personagens", "characters", "Personagens prontos para evoluir ou war.");
        var scripts = new Category("Scripts", "scripts", "Scripts e automacoes.");
        var macros = new Category("Macros", "macros", "Macros para gameplay assistida.");
        var services = new Category("Servicos", "services", "Servicos manuais e assistidos.");

        await dbContext.Categories.AddRangeAsync([gold, items, characters, scripts, macros, services], cancellationToken);

        var products = new[]
        {
            new Product("100kk Gold Coins", "100kk-gold-coins", "Entrega rapida de 100kk de gold.", 89.90m, gold.Id, gold.Slug),
            new Product("Magic Sword", "magic-sword", "Espada magica para hunts e colecao.", 49.90m, items.Id, items.Slug),
            new Product("Elite Knight 500", "elite-knight-500", "Personagem pronto para endgame.", 1599.90m, characters.Id, characters.Slug),
            new Product("Bot Script Premium", "bot-script-premium", "Script premium com atualizacoes.", 39.90m, scripts.Id, scripts.Slug),
            new Product("Macro de Cura", "macro-de-cura", "Macro configuravel para cura.", 19.90m, macros.Id, macros.Slug),
            new Product("Annihilator Service", "annihilator-service", "Servico assistido para Annihilator.", 59.90m, services.Id, services.Slug),
        };

        await dbContext.Products.AddRangeAsync(products, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
