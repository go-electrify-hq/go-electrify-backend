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
            b.Property(x => x.Description).HasMaxLength(2048);

            b.Property(x => x.Severity).HasMaxLength(16).IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.ToTable(t => t.HasCheckConstraint("CK_Incidents_Severity_UPPER", "Severity = UPPER(Severity)"));
            b.ToTable(t => t.HasCheckConstraint("CK_Incidents_Status_UPPER", "Status = UPPER(Status)"));

            b.Property(x => x.ReportedAt).IsRequired();

            b.HasOne(x => x.Station).WithMany().HasForeignKey(x => x.StationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Charger).WithMany().HasForeignKey(x => x.ChargerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReportedByUser).WithMany().HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.StationId, x.ReportedAt });
            b.HasIndex(x => x.ChargerId);

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
        }
    }
}
