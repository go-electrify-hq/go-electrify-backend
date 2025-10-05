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
    public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
    {
        public void Configure(EntityTypeBuilder<Incident> b)
        {
            b.ToTable("Incidents");
            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(128).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1024);
            b.Property(x => x.Priority).HasMaxLength(16);
            b.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("OPEN").IsRequired();
            b.Property(x => x.Response).HasMaxLength(1024);

            b.ToTable(t => t.HasCheckConstraint("CK_Incidents_Status_UPPER", "Status = UPPER(Status)"));

            b.HasOne(x => x.Station)
             .WithMany(s => s.Incidents)
             .HasForeignKey(x => x.StationId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.ReportedBy)
             .WithMany(ss => ss.IncidentsReported)
             .HasForeignKey(x => x.ReportedByStationStaffId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.StationId, x.Status });

            b.Property(x => x.CreatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
