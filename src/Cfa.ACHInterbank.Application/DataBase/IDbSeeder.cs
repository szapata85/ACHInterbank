namespace Cfa.ACHInterbank.Application.DataBase;

public interface IDbSeeder
{
    int Order { get; }
    Task SeedAsync();
}
