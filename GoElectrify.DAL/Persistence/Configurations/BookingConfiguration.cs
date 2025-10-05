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
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> b)
        {
            b.ToTable("Bookings");
            b.HasKey(x => x.Id);

            b.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("PENDING").IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_Bookings_Status_UPPER", "Status = UPPER(Status)"));

            b.Property(x => x.ScheduledStart).IsRequired();

            b.HasOne(x => x.User)
             .WithMany(u => u.Bookings)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Station)
             .WithMany(s => s.Bookings)
             .HasForeignKey(x => x.StationId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ConnectorType)
             .WithMany(ct => ct.Bookings)
             .HasForeignKey(x => x.ConnectorTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.VehicleModel)
             .WithMany(vm => vm.Bookings)
             .HasForeignKey(x => x.VehicleModelId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.EstimatedCost)
            .HasPrecision(18, 2);
            // 1-1 Booking <-> ChargingSession (FK nằm ở ChargingSession)
            b.HasOne(x => x.ChargingSession)
             .WithOne(cs => cs.Booking)
             .HasForeignKey<ChargingSession>(cs => cs.BookingId);

            b.HasIndex(x => new { x.UserId, x.ScheduledStart });
            b.HasIndex(x => new { x.StationId, x.ScheduledStart });

            b.Property(x => x.CreatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
