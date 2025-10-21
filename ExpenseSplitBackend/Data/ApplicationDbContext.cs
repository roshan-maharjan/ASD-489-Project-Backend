using ExpenseSplitBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseSplitBackend.Data
{
    // Inherit from IdentityDbContext, specifying your ApplicationUser
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // The DbSets for Users, Roles, etc. are all managed by the base IdentityDbContext
        // You no longer need: public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity handles its own schema, including unique emails.
            // You no longer need the manual index creation.
        }
    }
}