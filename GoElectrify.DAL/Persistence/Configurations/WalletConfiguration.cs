using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> b)
        {
            b.ToTable("Wallets");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
             .WithOne(u => u.Wallet)
             .HasForeignKey<Wallet>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.UserId).IsUnique();

            b.Property(x => x.Balance).HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();

            b.HasMany(x => x.Transactions)
             .WithOne(t => t.Wallet)
             .HasForeignKey(t => t.WalletId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.WalletSubscriptions)
             .WithOne(ws => ws.Wallet)
             .HasForeignKey(ws => ws.WalletId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.TopupIntents)
             .WithOne(ti => ti.Wallet)
             .HasForeignKey(ti => ti.WalletId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
