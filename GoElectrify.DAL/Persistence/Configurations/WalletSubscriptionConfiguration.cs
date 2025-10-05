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
    public class WalletSubscriptionConfiguration : IEntityTypeConfiguration<WalletSubscription>
    {
        public void Configure(EntityTypeBuilder<WalletSubscription> b)
        {
            b.ToTable("WalletSubscriptions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("ACTIVE").IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_WalletSubscriptions_Status_UPPER", "Status = UPPER(Status)"));

            b.Property(x => x.RemainingKwh).HasPrecision(12, 4).HasDefaultValue(0m).IsRequired();

            b.Property(x => x.StartDate).HasColumnType("date").IsRequired();
            b.Property(x => x.EndDate).HasColumnType("date").IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_WalletSubscriptions_DateRange", "[EndDate] >= [StartDate]"));
            b.ToTable(t => t.HasCheckConstraint("CK_WalletSubscriptions_RemainingKwh_NonNegative", "[RemainingKwh] >= 0"));

            b.HasOne(x => x.Wallet)
             .WithMany(w => w.WalletSubscriptions)
             .HasForeignKey(x => x.WalletId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Subscription)
             .WithMany(s => s.WalletSubscriptions)
             .HasForeignKey(x => x.SubscriptionId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.StartDate });
            b.HasIndex(x => new { x.WalletId, x.EndDate });

            b.Property(x => x.CreatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
