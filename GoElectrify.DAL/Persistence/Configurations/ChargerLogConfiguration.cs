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
    public class ChargerLogConfiguration : IEntityTypeConfiguration<ChargerLog>
    {
        public void Configure(EntityTypeBuilder<ChargerLog> b)
        {
            b.ToTable("ChargerLogs");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Charger)
             .WithMany(c => c.ChargerLogs)
             .HasForeignKey(x => x.ChargerId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Property(x => x.SampleAt).IsRequired();

            b.Property(x => x.Voltage).HasPrecision(12, 4);
            b.Property(x => x.Current).HasPrecision(12, 4);
            b.Property(x => x.PowerKw).HasPrecision(12, 4);
            b.Property(x => x.SessionEnergyKwh).HasPrecision(12, 4);
            b.Property(x => x.State).HasMaxLength(32);
            b.Property(x => x.ErrorCode).HasMaxLength(64);

            b.HasIndex(x => new { x.ChargerId, x.SampleAt }).IsUnique();

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
