using System;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.External.Connections;

public interface IHttpClientServiceScoped
{
    Task<T> SendPostRequestAsync<T>(string url, object content, HttpMethod method, TypeBody typeBody, string token = null);
}
