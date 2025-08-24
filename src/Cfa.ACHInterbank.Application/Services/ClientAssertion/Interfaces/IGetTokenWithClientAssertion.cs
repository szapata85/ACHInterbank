using Cfa.ACHInterbank.Application.Services.ClientAssertion.Model;
using Cfa.OpenData.Domain.Models.Response;

namespace Cfa.ACHInterbank.Application.Services.ClientAssertion.Interfaces;

public interface IGetTokenWithClientAssertion
{
    Task<Response<TokenClientAssertion>> GenerateClientAssertion();
}
