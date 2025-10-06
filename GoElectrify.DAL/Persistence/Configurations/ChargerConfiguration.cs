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
