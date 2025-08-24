using Cfa.OpenData.Domain.Models.Response;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.Application.External.JwksService;

public interface IJwksService
{
    Response<Keys> GetPublicJwks();
}
