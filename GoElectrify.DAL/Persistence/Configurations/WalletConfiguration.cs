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

            b.Property(x => x.Balance)
                .HasPrecision(18, 2)
                .IsRequired();

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();

            b.HasIndex(x => x.UserId).IsUnique(); // 1-1
        }
    }
}
