using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GoElectrify.DAL.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<WalletSubscription> WalletSubscriptions => Set<WalletSubscription>();
        public DbSet<Station> Stations => Set<Station>();
        public DbSet<ConnectorType> ConnectorTypes => Set<ConnectorType>();
        public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
        public DbSet<VehicleModelConnectorType> VehicleModelConnectorTypes => Set<VehicleModelConnectorType>();
        public DbSet<Charger> Chargers => Set<Charger>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<ChargingSession> ChargingSessions => Set<ChargingSession>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<StationStaff> StationStaff => Set<StationStaff>();
        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<ChargerLog> ChargerLogs => Set<ChargerLog>();
        public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
        public DbSet<TopupIntent> TopupIntents => Set<TopupIntent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        }
    }
}
