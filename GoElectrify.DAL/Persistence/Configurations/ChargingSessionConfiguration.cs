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

            // Columns
            b.Property(x => x.StartedAt).IsRequired();
            b.Property(x => x.EndedAt);
            b.Property(x => x.DurationMinutes).IsRequired();
            b.Property(x => x.ParkingMinutes);

            b.Property(x => x.SocStart).IsRequired();
            b.Property(x => x.SocEnd);

            b.Property(x => x.Status)
             .HasMaxLength(32)
             .HasDefaultValue("RUNNING")
             .IsRequired();

            b.Property(x => x.EnergyKwh).HasPrecision(12, 4).IsRequired();
            b.Property(x => x.AvgPowerKw).HasPrecision(12, 4);
            b.Property(x => x.Cost).HasPrecision(18, 2);

            // Check constraints (dữ liệu sạch)
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Status_UPPER", "Status = UPPER(Status)"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Status_Allowed",
                "Status IN ('RUNNING','STOPPED','COMPLETED','FAILED')"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Timespan",
                "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Duration_NonNegative",
                "[DurationMinutes] >= 0"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Parking_NonNegative",
                "[ParkingMinutes] IS NULL OR [ParkingMinutes] >= 0"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_SOC_Range",
                "[SocStart] BETWEEN 0 AND 100 AND ([SocEnd] IS NULL OR [SocEnd] BETWEEN 0 AND 100)"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Energy_NonNegative",
                "[EnergyKwh] >= 0"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_AvgPower_NonNegative",
                "[AvgPowerKw] IS NULL OR [AvgPowerKw] >= 0"));
            b.ToTable(t => t.HasCheckConstraint("CK_ChargingSessions_Cost_NonNegative",
                "[Cost] IS NULL OR [Cost] >= 0"));

            // Relationships
            b.HasOne(x => x.Charger)
             .WithMany(c => c.ChargingSessions)
             .HasForeignKey(x => x.ChargerId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Booking)
             .WithOne(bk => bk.ChargingSession)
             .HasForeignKey<ChargingSession>(cs => cs.BookingId)
             .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            b.HasIndex(x => new { x.ChargerId, x.StartedAt });
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.BookingId).IsUnique().HasFilter("[BookingId] IS NOT NULL");

            // Audit (INSERT mặc định do DB gán)
            b.Property(x => x.CreatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        }

    }
}
