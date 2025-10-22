using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
    {
        public void Configure(EntityTypeBuilder<SystemSetting> b)
        {
            b.ToTable("SystemSettings");
            b.HasKey(x => x.Id);
            b.Property(x => x.Key).IsRequired().HasMaxLength(100);
            b.Property(x => x.Value).IsRequired().HasMaxLength(200);
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => x.Key).IsUnique();
        }
    }
}
