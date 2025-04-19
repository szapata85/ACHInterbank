using Cfa.ACHInterbank.Application.DataBase;
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
        base.OnModelCreating(modelBuilder);
    }

    private void EntityConfiguation(ModelBuilder modelBuilder)
    {
        
    }

}
