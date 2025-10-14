using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class ConnectorTypeConfiguration : IEntityTypeConfiguration<ConnectorType>
    {
        public void Configure(EntityTypeBuilder<ConnectorType> b)
        {
            b.ToTable("ConnectorTypes");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();

            b.Property(x => x.Description).HasMaxLength(256);
            b.Property(x => x.MaxPowerKw).IsRequired();

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
