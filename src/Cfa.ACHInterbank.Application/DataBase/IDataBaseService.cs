namespace Cfa.ACHInterbank.Application.DataBase;

public interface IDataBaseService
{
    Task<bool> SaveAsync();
}
