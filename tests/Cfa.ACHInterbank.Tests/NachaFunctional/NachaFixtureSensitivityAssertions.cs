using System.Text.RegularExpressions;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests.NachaFunctional;

internal static class NachaFixtureSensitivityAssertions
{
    private static readonly string[] ForbiddenTerms =
    [
        "PRODUCCION",
        "PRODUCTION",
        "REAL CLIENT",
        "CLIENTE REAL",
        "DATOS REALES",
        "@"
    ];

    public static void ShouldNotContainSensitivePlaceholderViolations(string content, string path)
    {
        foreach (var term in ForbiddenTerms)
        {
            content.Contains(term, StringComparison.OrdinalIgnoreCase)
                .Should().BeFalse($"el fixture {path} no debe contener datos productivos o sensibles obvios");
        }

        Regex.IsMatch(content, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)
            .Should().BeFalse($"el fixture {path} no debe contener correos");
        Regex.IsMatch(content, @"\b3\d{9}\b")
            .Should().BeFalse($"el fixture {path} no debe contener telefonos celulares colombianos");
    }
}
