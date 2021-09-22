using CoreHoraLogadaInfra.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreHoraLogadaInfra.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            this.Database.EnsureCreated();
        }

        /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = ConnectionBuilder.GetConnectionString();

            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            base.OnConfiguring(optionsBuilder);
        }*/

        public DbSet<Role> Role { get; set; }
        public DbSet<Saque> Saque { get; set; }
    }
}