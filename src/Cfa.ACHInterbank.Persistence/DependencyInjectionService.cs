using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Simpl;
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

        services.AddDbContext<AchDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("SqlConnection")));

        services.AddQuartz(q =>
        {
            // Quartz usará el contenedor de DI para crear los Jobs
            q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();
        });

        services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });

        // Servicio que sincroniza DB → Quartz
        services.AddHostedService<SchedulerSyncService>();



        


        //Injection
        List<Type> types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract).ToList();

        foreach (var implementationType in types)
        {
            Type? interfaceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => i.Name.Contains(implementationType.Name) || i == typeof(ITaskHandler) || i == typeof(IDbSeeder));

            if (interfaceType == null) continue;

            if (implementationType.GetCustomAttribute<SingletonAttribute>() != null)
            {
                services.AddSingleton(interfaceType, implementationType);
            }
            else if (implementationType.GetCustomAttribute<ScopedAttribute>() != null)
            {
                services.AddScoped(interfaceType, implementationType);
            }
            else
            {
                services.AddTransient(interfaceType, implementationType);
            }
        }


        // Ejecuta seeders al inicio    
        //_ = DbInitializer.SeedAllAsync(services.BuildServiceProvider());

        return services;
    }
}
