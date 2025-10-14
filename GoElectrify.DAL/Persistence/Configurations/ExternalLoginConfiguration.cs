using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
    {
        public void Configure(EntityTypeBuilder<ExternalLogin> b)
        {
            b.ToTable("ExternalLogins");
            b.HasKey(x => x.Id);

            b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            b.Property(x => x.ProviderUserId).HasMaxLength(128).IsRequired();

            b.HasOne(x => x.User)
             .WithMany(u => u.ExternalLogins)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
