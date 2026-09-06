using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Models;

namespace AssetManagementApi.Data
{
    public class AppDbContext : DbContext 
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<ResolutionHistory> ResolutionHistories { get; set; }
    }
}