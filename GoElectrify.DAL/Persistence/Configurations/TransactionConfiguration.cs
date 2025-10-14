using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> b)
        {
            b.ToTable("Transactions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.Note).HasMaxLength(1024);

            b.ToTable(t => t.HasCheckConstraint("CK_Transactions_Type_UPPER", "type = UPPER(type)"));
            b.ToTable(t => t.HasCheckConstraint("CK_Transactions_Status_UPPER", "status = UPPER(status)"));
            b.ToTable(t => t.HasCheckConstraint("CK_Transactions_Amount_NonNegative", "amount >= 0"));

            b.HasOne(x => x.Wallet)
             .WithMany(w => w.Transactions)
             .HasForeignKey(x => x.WalletId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.ChargingSession)
             .WithMany(cs => cs.Transactions)
             .HasForeignKey(x => x.ChargingSessionId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.WalletId, x.CreatedAt });
            b.HasIndex(x => x.ChargingSessionId);

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
