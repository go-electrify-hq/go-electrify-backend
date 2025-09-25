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

            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_Bookings_Status_UPPER", "Status = UPPER(Status)"));

            b.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            b.Property(x => x.StartAt).IsRequired();
            b.Property(x => x.EndAt).IsRequired();

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Station)
                .WithMany()
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Charger)
                .WithMany()
                .HasForeignKey(x => x.ChargerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1-1 Booking <-> ChargingSession (Fk ở ChargingSession)
            b.HasOne(x => x.ChargingSession)
                .WithOne(cs => cs.Booking)
                .HasForeignKey<ChargingSession>(cs => cs.BookingId);

            b.HasIndex(x => new { x.UserId, x.StartAt });
            b.HasIndex(x => new { x.StationId, x.StartAt });
        }
    }
}
