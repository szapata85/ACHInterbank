using Cfa.ACHInterbank.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public class DataBaseService : DbContext
{
    public DataBaseService(DbContextOptions<DataBaseService> options) : base(options)
    {

    }
    public async Task<bool> SaveAsync()
    {
        return await SaveChangesAsync() > 0;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NachaHeaderConfiguration());
        modelBuilder.ApplyConfiguration(new BatchHeaderConfiguration());
        modelBuilder.ApplyConfiguration(new EntryDetailConfiguration());
        modelBuilder.ApplyConfiguration(new AddendaRecordConfiguration());
        modelBuilder.ApplyConfiguration(new BatchControlConfiguration());
        modelBuilder.ApplyConfiguration(new FileControlConfiguration());
    }

    private void EntityConfiguation(ModelBuilder modelBuilder)
    {

    }

}
