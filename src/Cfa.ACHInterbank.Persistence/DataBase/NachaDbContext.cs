using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public class NachaDbContext : DbContext
{
    public NachaDbContext(DbContextOptions<NachaDbContext> options) : base(options) { }

    public DbSet<NachaHeader> NachaHeaders { get; set; }
    public DbSet<BatchHeader> BatchHeaders { get; set; }
    public DbSet<EntryDetail> EntryDetails { get; set; }
    public DbSet<AddendaRecord> AddendaRecords { get; set; }
    public DbSet<BatchControl> BatchControls { get; set; }
    public DbSet<FileControl> FileControls { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NachaDbContext).Assembly);
    }
}
