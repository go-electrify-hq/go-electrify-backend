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

    public class ChargingSessionConfiguration : IEntityTypeConfiguration<ChargingSession>
    {
        public void Configure(EntityTypeBuilder<ChargingSession> b)
        {
            b.ToTable("ChargingSessions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Status_UPPER", "Status = UPPER(Status)"));

            b.Property(x => x.EnergyKwh).HasPrecision(12, 4).IsRequired();
            b.Property(x => x.AvgPowerKw).HasPrecision(12, 4);
            b.Property(x => x.Cost).HasPrecision(18, 2);

            b.Property(x => x.StartedAt).IsRequired();

            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Station).WithMany().HasForeignKey(x => x.StationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Charger).WithMany(c => c.ChargingSessions).HasForeignKey(x => x.ChargerId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.UserId, x.StartedAt });
            b.HasIndex(x => new { x.StationId, x.StartedAt });
            b.HasIndex(x => x.ChargerId);

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
        }
    }
}
