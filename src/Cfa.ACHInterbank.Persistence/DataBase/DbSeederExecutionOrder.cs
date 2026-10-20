using Cfa.ACHInterbank.Application.DataBase;

namespace Cfa.ACHInterbank.Persistence.DataBase;

internal static class DbSeederExecutionOrder
{
    public static IOrderedEnumerable<IDbSeeder> Apply(IEnumerable<IDbSeeder> seeders)
    {
        ArgumentNullException.ThrowIfNull(seeders);

        return seeders
            .OrderBy(seeder => seeder.Order)
            .ThenBy(GetStableTypeName, StringComparer.Ordinal);
    }

    private static string GetStableTypeName(IDbSeeder seeder)
    {
        ArgumentNullException.ThrowIfNull(seeder);

        var type = seeder.GetType();
        return type.FullName ?? type.Name;
    }
}
