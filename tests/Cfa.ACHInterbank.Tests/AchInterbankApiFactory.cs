using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchInterbankApiFactory : WebApplicationFactory<Program>
{
    public Mock<IProcesarRespuestaAchUseCase> ProcesarMock { get; } = new();
    public Mock<INotificarRespuestaAchUseCase> NotificarMock { get; } = new();
    public Mock<IAchResponseRepository> ResponseRepoMock { get; } = new();
    public Mock<IAchResponseNotificationAttemptRepository> AttemptRepoMock { get; } = new();
    public Mock<IAchResponseStatusMappingRepository> MappingRepoMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cb) =>
        {
            cb.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Postgres",
                ["Database:ApplyMigrations"] = "false",
                ["EnableHttpsRedirection"] = "false",
                ["ConnectionStrings:PostgresConnection"] = "Host=localhost;Port=5432;Database=ach_test;Username=test;Password=test",
                ["appSettings:tokenManager:secretKetJwt"] = "una_clave_de_prueba_larga_y_segura_1234567890",
                ["appSettings:tokenManager:issuerJwt"] = "test-issuer",
                ["appSettings:tokenManager:audienceJwt"] = "test-audience",
                ["Cors:Origins:0"] = "http://localhost"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProcesarRespuestaAchUseCase>();
            services.RemoveAll<INotificarRespuestaAchUseCase>();
            services.RemoveAll<IAchResponseRepository>();
            services.RemoveAll<IAchResponseNotificationAttemptRepository>();
            services.RemoveAll<IAchResponseStatusMappingRepository>();

            services.AddSingleton(ProcesarMock.Object);
            services.AddSingleton(NotificarMock.Object);
            services.AddSingleton(ResponseRepoMock.Object);
            services.AddSingleton(AttemptRepoMock.Object);
            services.AddSingleton(MappingRepoMock.Object);
        });
    }
}
