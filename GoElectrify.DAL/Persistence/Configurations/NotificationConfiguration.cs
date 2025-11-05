using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> b)
        {
            b.ToTable("Notifications");
            b.HasKey(n => n.Id);

            b.Property(n => n.UserId).IsRequired();

            // Marker
            b.Property(n => n.IsMarker).HasDefaultValue(false).IsRequired();
            b.Property(n => n.MarkerKind).HasMaxLength(64);
            b.Property(n => n.MarkerValueUtc);

            // State theo NotifKey (KHÔNG IsRequired để cho phép marker rows)
            b.Property(n => n.NotifKey).HasMaxLength(128);
            b.Property(n => n.ReadAtUtc);

            b.HasOne(n => n.User)
             .WithMany()
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // 1) Mỗi user chỉ có 1 marker theo MarkerKind
            b.HasIndex(n => new { n.UserId, n.MarkerKind })
             .HasFilter("is_marker = TRUE AND marker_kind IS NOT NULL") 
             .IsUnique();

            // 2) Mỗi user chỉ có 1 state-row cho mỗi NotifKey
            b.HasIndex(n => new { n.UserId, n.NotifKey })
             .HasFilter("is_marker = FALSE AND notif_key IS NOT NULL") 
             .IsUnique();

            // 3) Đếm/tải unread nhanh
            b.HasIndex(n => new { n.UserId, n.ReadAtUtc })
             .HasFilter("is_marker = FALSE AND read_at_utc IS NULL"); 

        }
    }
}
