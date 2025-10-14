﻿using System;
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

            // CHECK constraints cho Postgres + snake_case
            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_status_upper",
                "status = UPPER(status)"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_status_allowed",
                "status IN ('RUNNING','STOPPED','COMPLETED','FAILED')"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_timespan",
                "ended_at IS NULL OR ended_at >= started_at"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_duration_non_negative",
                "duration_minutes >= 0"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_parking_non_negative",
                "parking_minutes IS NULL OR parking_minutes >= 0"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_soc_range",
                "soc_start BETWEEN 0 AND 100 AND (soc_end IS NULL OR soc_end BETWEEN 0 AND 100)"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_energy_non_negative",
                "energy_kwh >= 0"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_avg_power_non_negative",
                "avg_power_kw IS NULL OR avg_power_kw >= 0"));

            b.ToTable(t => t.HasCheckConstraint("ck_charging_sessions_cost_non_negative",
                "cost IS NULL OR cost >= 0"));


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
            b.HasIndex(x => x.BookingId).IsUnique();
            b.HasIndex(x => x.ChargerId)
            .HasFilter("ended_at IS NULL")
            .IsUnique()
            .HasDatabaseName("ux_charging_sessions_active_per_charger");

            // Audit (INSERT mặc định do DB gán)
            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }

    }
}
