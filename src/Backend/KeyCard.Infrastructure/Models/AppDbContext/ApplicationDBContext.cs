using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.Models.AppDbContext
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options) { }

        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>();
            base.OnModelCreating(modelBuilder);

        }
    }
}
