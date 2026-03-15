using AutoMapper;
using Cfa.ACHInterbank.Domain.Entities.Servers;
using Cfa.ACHInterbank.Domain.Models.Configurations;
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

    public static void Configure()
    {
        if (_instance == null)
        {
            var configurationExpression = new MapperConfigurationExpression();
            configurationExpression.AddProfile<MapperProfile>();

            var config = new MapperConfiguration(configurationExpression);
            _instance = config.CreateMapper();
        }
    }
}
