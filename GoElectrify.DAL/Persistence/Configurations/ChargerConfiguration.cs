using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class ChargerConfiguration : IEntityTypeConfiguration<Charger>
    {
        public void Configure(EntityTypeBuilder<Charger> b)
        {
            b.ToTable("Chargers");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.Property(x => x.PowerKw).IsRequired();

            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_Chargers_Status_UPPER", "status = UPPER(status)"));

            b.Property(x => x.PricePerKwh).HasPrecision(18, 4);

            b.Property(c => c.DockSecretHash)
           .HasMaxLength(256)
           .IsUnicode(false);

            b.Property(c => c.AblyChannel)
            .HasMaxLength(128)
            .IsUnicode(false);

            b.Property(c => c.DockStatus)
            .HasMaxLength(20)
            .IsUnicode(false);

            b.Property(c => c.LastConnectedAt);

            b.HasOne(x => x.Station)
             .WithMany(s => s.Chargers)
             .HasForeignKey(x => x.StationId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.ConnectorType)
             .WithMany(ct => ct.Chargers)
             .HasForeignKey(x => x.ConnectorTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.StationId, x.Code }).IsUnique();
            b.HasIndex(x => x.ConnectorTypeId);

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
