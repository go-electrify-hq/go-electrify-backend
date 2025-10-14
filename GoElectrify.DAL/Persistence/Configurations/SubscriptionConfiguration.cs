using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoElectrify.DAL.Persistence.Configurations
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> b)
        {
            b.ToTable("Subscriptions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();

            b.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.TotalKwh).HasPrecision(12, 4).IsRequired();
            b.Property(x => x.DurationDays).IsRequired();

            b.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd().IsRequired();
        }
    }
}
