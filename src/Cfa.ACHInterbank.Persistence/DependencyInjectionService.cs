using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Cfa.ACHInterbank.Persistence;

public static class DependencyInjectionService
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddDbContext<DataBaseService>(options => options.UseAseClient(configuration.GetConnectionString("SybaseConnection")));

        //using (var connection = new AseConnection(""))
        //using (var commmand = connection.CreateCommand())
        //{
        //    connection.Open();
        //    //command stuff
        //}

        //services.AddDbContext<DataBaseService>(options => options.UseSqlServer(configuration.GetConnectionString("SqlConnection")));

        var sqlconnection = configuration.GetConnectionString("SqlConnection");

        services.AddDbContext<AchDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("SqlConnection")));

        //using (var scope = app.Services.CreateScope())
        //{
        //    BarContext Context = scope.ServiceProvider.GetRequiredService<BarContext>();
        //    Context.Database.Migrate();
        //}


        //Injection
        var lst = Assembly.GetExecutingAssembly().GetTypes()
                  .Where(t => t.IsClass && !t.IsAbstract) // Filtra solo las clases concretas
                  .SelectMany(implementationType => implementationType.GetInterfaces().Where(o => o.Name.Contains(implementationType.Name))
                      .Select(interfacetype => new { Interface = interfacetype, Implementation = implementationType }))
                  .ToList();
                lst.ForEach(pair =>
                {
                    switch (true)
                    {
                        case var _ when pair.Implementation.Name.Contains("Singleton"):
                            services.AddSingleton(pair.Interface, pair.Implementation);
                            break;
                        case var _ when pair.Implementation.Name.Contains("Scoped"):
                            services.AddScoped(pair.Interface, pair.Implementation);
                            break;
                        default:
                            services.AddTransient(pair.Interface, pair.Implementation);
                            break;
                    }
                });

        return services;
    }
}
