using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.External.Connections;

public interface IHttpClientService
{
    Task<T> SendPostRequestAsync<T>(string url, object content, HttpMethod method, TypeBody typeBody, string? token = null);
}
