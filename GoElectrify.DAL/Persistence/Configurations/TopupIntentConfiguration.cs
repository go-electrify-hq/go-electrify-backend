using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class TopupIntentConfiguration : IEntityTypeConfiguration<TopupIntent>
    {
        public void Configure(EntityTypeBuilder<TopupIntent> b)
        {
            b.ToTable("TopupIntents");
            b.HasKey(x => x.Id);

            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            b.Property(x => x.ProviderRef).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();

            b.Property(x => x.QrContent).HasMaxLength(1024);
            b.Property(x => x.RawWebhook).HasColumnType("nvarchar(max)");

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();

            b.HasOne(x => x.Wallet)
                .WithMany() // không cần collection ở Wallet
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.Provider, x.ProviderRef }).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreatedAt);
        }
    }
}
