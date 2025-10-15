using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class StationStaffConfiguration : IEntityTypeConfiguration<StationStaff>
    {
        public void Configure(EntityTypeBuilder<StationStaff> b)
        {
            b.ToTable("StationStaff");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
             .WithMany(u => u.StationStaff)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Station)
             .WithMany(s => s.StationStaff)
             .HasForeignKey(x => x.StationId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.UserId, x.StationId }).IsUnique();

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
