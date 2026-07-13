using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Cfa.ACHInterbank.Persistence.DataBase;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ProcTransaccionesProductionEvidenceTests
{
    private const string ExpectedLogSha256 = "36CF1C99C118EEFD90DB2FD93FCC4CA98F0811944E606239C22A814713984C6C";

    [Fact]
    public void ProductionLog_OnlyCorrelatedR96BlocksConfirmTheObservedSignature()
    {
        var path = Environment.GetEnvironmentVariable("PROC_TRANSACCIONES_PRODUCTION_LOG_PATH");
        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.True(File.Exists(path));
        Assert.Equal(ExpectedLogSha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path!))));

        var text = File.ReadAllText(path!);
        var blocks = Regex.Matches(
            text,
            @"INICIO\s+Proc_Transacciones(?<body>.*?)FIN\s+Proc_Transacciones:\s*R96",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.Equal(1576, blocks.Count);

        var present = new[]
        {
            "TIPTRAN", "BCORECEP", "BCOORIG", "NORIG", "NCTAORIG", "IDORIG", "DESTRAN",
            "FECEFEC", "NCTARECEP", "MONTO", "NRECEP", "IDRECEP", "DISCRE", "INFPAG",
            "IDTRAN", "IDLOTE", "IREVER", "IDCAMCOMPE", "ILR"
        };
        var absent = new[] { "TREG", "CONV", "PROD", "REGLOTE", "LIBRE", "DIRECCIONIP", "LIBRE1" };

        foreach (Match block in blocks)
        {
            var body = block.Groups["body"].Value;
            Assert.All(present, name => Assert.Matches($@"<{name}>.*?</{name}>", body));
            Assert.All(absent, name => Assert.DoesNotMatch($@"<{name}>.*?</{name}>", body));
            Assert.Matches(@"<IDTRAN>\d{7}</IDTRAN>", body);
            Assert.Matches(@"<IDLOTE>\d{6}</IDLOTE>", body);
            Assert.Matches(@"<FECEFEC>\d{8}</FECEFEC>", body);
        }
    }

    [Fact]
    public void SoapBody_OmitsEmptyLegacyFieldsAndInternalMethodMarkers()
    {
        using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var mapper = new ProcTransaccionesRequestMapper(context);
        var body = mapper.BuildSoapBody(new ProcTransaccionesRequestContract(
            new Dictionary<string, string>
            {
                ["TIPTRAN"] = "32",
                ["NCTAORIG"] = string.Empty,
                ["TREG"] = string.Empty,
                ["METODO"] = "Proc_Transacciones",
                ["ILR"] = "A"
            },
            new Dictionary<string, string>()));

        Assert.Contains("<tem:TIPTRAN>32</tem:TIPTRAN>", body);
        Assert.DoesNotContain("NCTAORIG", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TREG", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<METODO>", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ILR", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Contrapartidas", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLValidarUsuarioBV", body, StringComparison.OrdinalIgnoreCase);
    }
}
