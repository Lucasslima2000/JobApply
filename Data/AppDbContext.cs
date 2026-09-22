using JobApply.Models;
using Microsoft.EntityFrameworkCore;

namespace JobApply.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vaga> Vagas { get; set; }
    }
}