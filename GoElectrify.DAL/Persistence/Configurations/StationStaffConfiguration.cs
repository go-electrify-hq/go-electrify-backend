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

            //b.Property(x => x.Role).HasMaxLength(16).IsRequired();
            //b.ToTable(t => t.HasCheckConstraint("CK_StationStaff_Role_UPPER", "Role = UPPER(Role)"));

            b.Property(x => x.AssignedAt).IsRequired();

            b.HasOne(x => x.Station).WithMany(s => s.StationStaff).HasForeignKey(x => x.StationId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.StationId, x.UserId }).IsUnique();

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
        }
    }
}
