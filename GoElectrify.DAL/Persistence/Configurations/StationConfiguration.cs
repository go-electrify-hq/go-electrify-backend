using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class StationConfiguration : IEntityTypeConfiguration<Station>
    {
        public void Configure(EntityTypeBuilder<Station> b)
        {
            b.ToTable("Stations");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1024);
            b.Property(x => x.Address).HasMaxLength(256).IsRequired();
            b.Property(x => x.ImageUrl).HasMaxLength(512);

            b.Property(x => x.Latitude).HasPrecision(10, 6).IsRequired();
            b.Property(x => x.Longitude).HasPrecision(10, 6).IsRequired();

            b.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

            b.ToTable(t => t.HasCheckConstraint(
                "ck_stations_status_values",
                "status IN ('ACTIVE','INACTIVE','MAINTENANCE')"
            ));

            b.HasIndex(x => new { x.Status, x.Name });
            b.HasIndex(x => new { x.Latitude, x.Longitude });

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
