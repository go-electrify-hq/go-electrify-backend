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
    public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
    {
        public void Configure(EntityTypeBuilder<ExternalLogin> b)
        {
            b.ToTable("ExternalLogins");
            b.HasKey(x => x.Id);

            b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            b.Property(x => x.ProviderUserId).HasMaxLength(128).IsRequired();
            b.Property(x => x.Email).HasMaxLength(255);

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();

            b.HasOne(x => x.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
            b.HasIndex(x => x.UserId);
        }
    }
}
