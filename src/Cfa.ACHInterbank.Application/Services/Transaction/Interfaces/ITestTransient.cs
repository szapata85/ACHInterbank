using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Application.Services.Transaction.Interfaces;

public interface ITestTransient
{
    void Get(HttpRequest request, string data);
    
}
