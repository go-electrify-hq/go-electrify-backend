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
