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

            b.Property(x => x.CreatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
