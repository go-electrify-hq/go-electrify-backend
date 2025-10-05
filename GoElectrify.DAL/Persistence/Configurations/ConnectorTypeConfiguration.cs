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

            b.Property(x => x.CreatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
