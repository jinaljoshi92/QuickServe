using Microsoft.EntityFrameworkCore;

namespace QuickServe.Data
{
    public class QuickServeDbContext : DbContext
    {
        public QuickServeDbContext(DbContextOptions<QuickServeDbContext> options) : base(options)
        {
        }

        public DbSet<Models.User> Users { get; set; }
        public DbSet<Models.Wallet> Wallet { get; set; }
        public DbSet<Models.Transaction> Transactions { get; set; }
        public DbSet<Models.ServiceRequest> ServiceRequests { get; set; }

    }
}
