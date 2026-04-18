namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaType7AliasMap
{
    string Normalize(string value);
    string GetCanonicalKey(string keyOrAlias);
    IReadOnlyCollection<string> GetAliases(string canonicalKey);
}
