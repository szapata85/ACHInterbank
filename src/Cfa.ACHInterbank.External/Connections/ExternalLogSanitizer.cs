namespace Cfa.ACHInterbank.External.Connections;

internal static class ExternalLogSanitizer
{
    public static string Endpoint(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri.GetLeftPart(UriPartial.Path);
    }

    public static string Endpoint(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? Endpoint(uri) : "invalid-endpoint";
}
