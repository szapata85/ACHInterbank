namespace Cfa.ACHInterbank.Application.External.Connections;

public interface IConnectionAuthScoped
{
    Task<string> StartWCF(string data, string requestType = "WCFBM");
}
