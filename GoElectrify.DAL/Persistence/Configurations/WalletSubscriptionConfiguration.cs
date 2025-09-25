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

            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_WalletSubscriptions_Status_UPPER", "Status = UPPER(Status)"));

            b.Property(x => x.RemainingKwh).HasPrecision(12, 4).IsRequired();
            b.Property(x => x.StartDate).IsRequired();
            b.Property(x => x.EndDate).IsRequired();

            b.HasOne(x => x.Wallet)
                .WithMany()
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Subscription)
                .WithMany(s => s.WalletSubscriptions)
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.WalletId);
            b.HasIndex(x => new { x.WalletId, x.Status });
            b.HasIndex(x => x.EndDate);
        }
    }
}
