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

            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.OrderCode).HasMaxLength(128);

            b.ToTable(t => t.HasCheckConstraint("CK_TopupIntents_Status_UPPER", "status = UPPER(status)"));
            b.ToTable(t => t.HasCheckConstraint("CK_TopupIntents_Amount_NonNegative", "amount >= 0"));

            b.HasOne(x => x.Wallet)
             .WithMany(w => w.TopupIntents)
             .HasForeignKey(x => x.WalletId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.WalletId, x.CreatedAt });

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
