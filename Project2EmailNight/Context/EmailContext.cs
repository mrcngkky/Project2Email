using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project2EmailNight.Entities;

namespace Project2EmailNight.Context
{
    public class EmailContext : IdentityDbContext<AppUser>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;initial catalog=Project2EmailNightDb;integrated security=true;TrustServerCertificate=True");
        }
        public DbSet<Message> Messages { get; set; }

        public DbSet<MessageAttachment> MessageAttachments { get; set; }
    }
}
