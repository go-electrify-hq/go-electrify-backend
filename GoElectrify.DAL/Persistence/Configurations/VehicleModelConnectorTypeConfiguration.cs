using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class VehicleModelConnectorTypeConfiguration : IEntityTypeConfiguration<VehicleModelConnectorType>
    {
        public void Configure(EntityTypeBuilder<VehicleModelConnectorType> b)
        {
            b.ToTable("VehicleModelConnectorTypes");
            b.HasKey(x => new { x.VehicleModelId, x.ConnectorTypeId });

            b.HasOne(x => x.VehicleModel)
             .WithMany(vm => vm.VehicleModelConnectorTypes)
             .HasForeignKey(x => x.VehicleModelId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.ConnectorType)
             .WithMany(ct => ct.VehicleModelConnectorTypes)
             .HasForeignKey(x => x.ConnectorTypeId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.ConnectorTypeId);
        }
    }
}
