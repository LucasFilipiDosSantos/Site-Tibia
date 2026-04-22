using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(x => x.Price)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.CategoryId)
            .IsRequired();

        builder.Property(x => x.CategorySlug)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Server)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.Rating)
            .HasColumnType("numeric(3,2)")
            .IsRequired();

        builder.Property(x => x.SalesCount)
            .IsRequired();

        builder.Property(x => x.IsHidden)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasIndex(x => new { x.IsHidden, x.CategorySlug });

        builder.HasIndex(x => new { x.IsHidden, x.CreatedAtUtc });

        builder.HasIndex(x => x.CategoryId);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
