using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public class NachaDbContext : DbContext
{
    public NachaDbContext(DbContextOptions<NachaDbContext> options) : base(options) 
    {
        
    }

    public DbSet<NachaHeader> NachaHeaders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NachaDbContext).Assembly);
    }
}
