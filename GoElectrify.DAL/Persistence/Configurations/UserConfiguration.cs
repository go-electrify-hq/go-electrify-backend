using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.ToTable("Users");
            b.HasKey(x => x.Id);

            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.FullName).HasMaxLength(128);

            b.HasOne(x => x.Role)
             .WithMany() // hoặc .WithMany(r => r.Users) nếu đã khai báo
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.CreatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
