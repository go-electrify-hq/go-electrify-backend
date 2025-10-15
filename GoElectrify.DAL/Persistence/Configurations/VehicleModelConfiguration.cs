using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
    {
        public void Configure(EntityTypeBuilder<VehicleModel> b)
        {
            b.ToTable("VehicleModels");
            b.HasKey(x => x.Id);

            b.Property(x => x.ModelName).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.ModelName).IsUnique();

            b.Property(x => x.MaxPowerKw).IsRequired();
            b.Property(x => x.BatteryCapacityKwh).HasPrecision(12, 4).IsRequired();

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
