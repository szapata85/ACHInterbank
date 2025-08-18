using AutoMapper;
using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Logging;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.Application.Configuration;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<Key, KeyMap>();
        CreateMap<ServicesIntegration, ServerCache>();
    }
}

public static class MapperBootstrapper
{
    private static IMapper _instance;
    public static IMapper Instance => _instance;

    public static void Configure(ILoggerFactory loggerFactory)
    {
        if (_instance == null)
        {
            var configExpr = new MapperConfigurationExpression();
            configExpr.AddProfile<MapperProfile>();

            var config = new MapperConfiguration(configExpr, loggerFactory);
            _instance = config.CreateMapper();
        }
    }
}

