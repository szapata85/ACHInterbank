namespace Cfa.ACHInterbank.Api.Configuration;

public sealed class ApiRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 1;
    public int QueueLimit { get; set; } = 2;

    public static ApiRateLimitingOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new ApiRateLimitingOptions();
        configuration.GetSection(SectionName).Bind(options);

        if (options.PermitLimit <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:PermitLimit debe ser mayor que cero.");
        }

        if (options.WindowSeconds <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:WindowSeconds debe ser mayor que cero.");
        }

        if (options.QueueLimit < 0)
        {
            throw new InvalidOperationException($"{SectionName}:QueueLimit no puede ser negativo.");
        }

        return options;
    }
}
