using System.Text.RegularExpressions;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public class AngularSpaUatReadinessCharacterizationTests
{
    private static readonly string RepoRoot = FindRepositoryRoot();
    private static readonly string SpaRoot = Path.Combine(RepoRoot, "web", "ach-interbank-ui");
    private static readonly string SpaMatrixPath = Path.Combine(RepoRoot, "docs", "audits", "spa-angular-backend-uat-alignment-gap-matrix-current.md");

    [Fact]
    public void AngularSpa_ShouldExist_AtAuditedLocation()
    {
        Directory.Exists(SpaRoot).Should().BeTrue();
        File.Exists(Path.Combine(SpaRoot, "angular.json")).Should().BeTrue();
        var packageJsonPath = Path.Combine(SpaRoot, "package.json");
        File.Exists(packageJsonPath).Should().BeTrue();
        Read(packageJsonPath).Should().MatchRegex("@angular/(core|)", RegexOptions.IgnoreCase);

        var matrix = Read(SpaMatrixPath);
        matrix.Should().Contain("web/ach-interbank-ui");
    }

    [Fact]
    public void SpaGapMatrix_ShouldKeepPartialReadiness_AndNoGoProductivo()
    {
        var matrix = Read(SpaMatrixPath);
        matrix.Should().Contain("SPA readiness: **Parcial**");
        matrix.Should().Contain("12D: **Sí con restricciones**");
        matrix.Should().Contain("12E: **No debe ejecutarse como UAT 100% SPA-only**");
        matrix.Should().Contain("GO productivo: **NO**");
        matrix.Should().Contain("NO-GO productivo vigente");

        matrix.Should().NotContain("SPA readiness: **Sí**");
        matrix.Should().NotContain("SPA listo total");
        matrix.Should().NotContain("GO productivo: **Sí**");
        matrix.Should().NotContain("aprobado productivo");
        matrix.Should().NotContain("producción aprobada");
    }

    [Fact]
    public void AngularSpa_ShouldNotDeclareGoProductivo()
    {
        var forbidden = new[]
        {
            "GO productivo: Sí", "GO productivo: SI", "Aprobado productivo", "Producción aprobada",
            "Listo para producción", "Productivo aprobado", "Aprobación productiva", "Listo para productivo"
        };

        foreach (var file in EnumerateSpaFiles())
        {
            var lines = ReadLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("GO productivo: NO", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("NO-GO productivo", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var phrase in forbidden)
                {
                    line.Contains(phrase, StringComparison.OrdinalIgnoreCase)
                        .Should().BeFalse($"forbidden phrase '{phrase}' in {ToRelative(file)}:{i + 1}");
                }
            }
        }
    }

    [Fact]
    public void AngularSpa_ShouldNotExposeAccountingReview_AsLedgerOrPosting()
    {
        var accountingTriggers = new[] { "accounting-review", "AccountingReview", "revisión contable", "revision contable", "reconciliation", "conciliación", "conciliacion" };
        var forbidden = new[] { "ledger", "journal", "posting", "asiento", "asientos", "contabilizar", "contabilizado", "contabilidad oficial", "AccountingPosted", "JournalEntry", "LedgerEntry", "PostingId", "AccountingEntryId" };
        var negations = new[] { "no", "sin", "not", "does not", "without" };

        foreach (var file in EnumerateSpaFiles())
        {
            var lines = ReadLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!ContainsAny(line, accountingTriggers)) continue;

                foreach (var bad in forbidden)
                {
                    if (!line.Contains(bad, StringComparison.OrdinalIgnoreCase)) continue;
                    ContainsAny(line, negations).Should().BeTrue($"line must negate accounting-posting semantics: {ToRelative(file)}:{i + 1}: {line}");
                }
            }
        }
    }

    [Fact]
    public void AngularSpa_ShouldNotTreatSimulatedLiquidity_AsRealCudBalance()
    {
        foreach (var file in EnumerateSpaFiles())
        {
            var lines = ReadLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lower = line.ToLowerInvariant();
                var simulated = lower.Contains("simulatedliquidity") || lower.Contains("liquidez simulada") || lower.Contains("simulated");
                var realCud = lower.Contains("saldo real cud") || (lower.Contains("saldo cud") && lower.Contains("real"));
                if (simulated && realCud)
                {
                    lower.Contains("no equivale").Should().BeTrue($"simulated liquidity cannot be treated as real CUD balance: {ToRelative(file)}:{i + 1}");
                }
            }
        }

        Read(SpaMatrixPath).Should().Contain("Liquidez simulada no equivale a saldo real CUD.");
    }

    [Fact]
    public void AngularSpa_ShouldNotExposeCertificateSecrets()
    {
        var forbidden = new[] { "privateKey", "private key", "clave privada", "pemPrivateKey", "pfxPassword", "password certificado", "certificatePassword", "PFX password", "SecretRef crudo visible no enmascarado", "secretRef crudo visible no enmascarado" };
        var allow = new[] { "secretRefMasked", "masked", "enmascarado", "certificado sin llave privada", "sin llave privada" };

        foreach (var file in EnumerateSpaFiles())
        {
            var lines = ReadLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                foreach (var bad in forbidden)
                {
                    if (!line.Contains(bad, StringComparison.OrdinalIgnoreCase)) continue;
                    ContainsAny(line, allow).Should().BeTrue($"secret-like token without masking/negative context at {ToRelative(file)}:{i + 1}");
                }
            }
        }
    }

    [Fact]
    public void SpaGapMatrix_ShouldDocumentAccountingReviewExportAsGap()
    {
        var matrix = Read(SpaMatrixPath);
        matrix.Should().Contain("POST /api/reports/accounting-review/export");
        matrix.Should().Contain("Parcial");
        matrix.Should().MatchRegex("(Consumo SPA no confirmado|No confirmado)", RegexOptions.IgnoreCase);
        matrix.Should().MatchRegex("Exponer flujo explícito en UI|equivalente", RegexOptions.IgnoreCase);
    }

    [Fact]
    public void SpaGapMatrix_ShouldDocumentNoIntegralUatModule()
    {
        var matrix = Read(SpaMatrixPath);
        matrix.Should().Contain("Falta módulo UAT integral");
        matrix.Should().Contain("evidencias");
        matrix.Should().Contain("defectos");
        matrix.Should().Contain("aprobadores");
        matrix.Should().Contain("scorecard UAT");
        matrix.Should().Contain("12B/12C");
        matrix.Should().Contain("Excel/PDF");
        matrix.Should().Contain("documental/manual");

        matrix.Should().NotContain("módulo UAT integral implementado");
        matrix.Should().NotContain("UAT 100% SPA");
        matrix.Should().NotContain("SPA listo total");
    }

    [Fact]
    public void AngularSpa_ProdEnvironmentHardcodedIp_ShouldRemainDocumentedAsP2()
    {
        var envProd = Path.Combine(SpaRoot, "src", "environments", "environment.prod.ts");
        if (!File.Exists(envProd)) return;

        var content = Read(envProd);
        var hasPrivateIp = Regex.IsMatch(content, @"https?://(192\.|10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.)", RegexOptions.IgnoreCase);
        if (!hasPrivateIp) return;

        var matrix = Read(SpaMatrixPath);
        matrix.Should().Contain("environment.prod.ts");
        matrix.Should().Contain("IP fija");
        matrix.Should().Contain("P2");
    }

    [Fact]
    public void AngularSpa_CriticalRoutes_ShouldBePresentOrDocumentedAsGap()
    {
        var all = string.Join("\n", EnumerateSpaFiles().Select(Read));
        all.Should().Contain("reports");
        all.Should().Contain("cenit");
        all.Should().Contain("nacha-security");
        all.Should().Contain("transactions");
        (all.Contains("audit-logs") || all.Contains("audit")).Should().BeTrue();

        var matrix = Read(SpaMatrixPath);
        matrix.Should().Contain("accounting-review export");
        matrix.Should().Contain("UAT package/downloads");
        matrix.Should().Contain("CUD evidence boundary");
        matrix.Should().Contain("naming externo");
    }

    [Fact]
    public void SpaGapMatrix_ShouldDocumentFineGrainedUatRolesAsPartial()
    {
        var matrix = Read(SpaMatrixPath);
        matrix.Should().Contain("Tesorería");
        matrix.Should().Contain("Seguridad");
        matrix.Should().Contain("Riesgo/Compliance");
        matrix.Should().Contain("Tecnología/QA");
        matrix.Should().Contain("Parcial");
        matrix.Should().MatchRegex("Falta separación fina|No explícito por dominio|Granularidad de negocio limitada", RegexOptions.IgnoreCase);
    }

    [Fact]
    public void UatDocuments_ShouldReferenceSpaGapMatrix_For12DRestriction()
    {
        var docs = new[]
        {
            Path.Combine(RepoRoot, "docs", "uat", "real-data-uat-execution-protocol.md"),
            Path.Combine(RepoRoot, "docs", "uat", "operator-guides", "uat-operator-execution-guide.md"),
            Path.Combine(RepoRoot, "docs", "audits", "go-nogo-scorecard-funcional-normativo-2026-04-26.md"),
            Path.Combine(RepoRoot, "docs", "governance", "current-vs-historical-matrix-policy.md")
        };

        foreach (var doc in docs)
        {
            var content = Read(doc);
            content.Should().Contain("docs/audits/spa-angular-backend-uat-alignment-gap-matrix-current.md", $"missing matrix reference in {ToRelative(doc)}");
            ContainsAny(content, new[] { "12D", "restricción", "restricciones", "NO-GO productivo" }).Should().BeTrue($"missing 12D/restriction token in {ToRelative(doc)}");
        }
    }

    [Fact]
    public void UatGeneratedPdfExcel_ShouldNotBeVersioned()
    {
        Directory.GetFiles(Path.Combine(RepoRoot, "docs", "uat", "operator-guides", "exports"), "*.pdf").Should().BeEmpty();
        Directory.GetFiles(Path.Combine(RepoRoot, "docs", "uat", "operator-guides", "exports"), "*.xlsx").Should().BeEmpty();

        var gitignore = Read(Path.Combine(RepoRoot, ".gitignore"));
        gitignore.Should().Contain("docs/uat/operator-guides/exports/*.pdf");
        gitignore.Should().Contain("docs/uat/operator-guides/exports/*.xlsx");
    }

    [Fact]
    public void UatDeliverableGenerator_ShouldExist_AndDeclareNoGoSafeguards()
    {
        var path = Path.Combine(RepoRoot, "tools", "uat", "generate_uat_operator_deliverables.py");
        File.Exists(path).Should().BeTrue();

        var content = Read(path);
        content.Should().Contain("UAT_ACHInterbank_Guia_Operativa_Usuarios.pdf");
        content.Should().Contain("UAT_ACHInterbank_Set_Pruebas_Operativas.xlsx");
        content.Should().Contain("GO productivo");
        content.Should().Contain("NO");
        content.Should().Contain("NO-GO productivo");
        content.Should().MatchRegex("Vigente|vigente");

        foreach (var sheet in new[] { "Instrucciones", "Casos_UAT", "Evidencias", "Defectos", "Aprobadores", "Resumen_Ejecucion", "Scorecard_UAT" })
            content.Should().Contain(sheet);
    }

    [Fact]
    public void SpaGapMatrix_ShouldKeepP1P2GapsVisible()
    {
        var matrix = Read(SpaMatrixPath);
        foreach (var p1 in new[]
        {
            "Falta módulo UAT integral", "Falta consumo confirmado de accounting-review export", "Roles UAT finos no evidentes",
            "Trazabilidad directa parcial/no confirmada", "CUD evidence boundary sin flujo UI integral"
        }) matrix.Should().Contain(p1);

        foreach (var p2 in new[]
        {
            "Semántica CUD/liquidez debe reforzarse", "environment.prod.ts con IP fija", "Falta ayuda contextual y links a guías 12B"
        }) matrix.Should().Contain(p2);
    }

    [Fact]
    public void SpaGapMatrix_ShouldNotClaimFullSpaReadiness_WithoutRemovingRestrictions()
    {
        var matrix = Read(SpaMatrixPath);
        var hasForbidden = new[] { "SPA readiness: **Sí**", "SPA listo total", "12E: **Sí**", "UAT 100% SPA-only" }
            .Any(x => matrix.Contains(x, StringComparison.OrdinalIgnoreCase));

        hasForbidden.Should().BeFalse("La matriz SPA no puede declarar readiness total mientras conserve restricciones 12D/12E y brechas P1/P2. Actualizar brechas, tests y scorecard de forma explícita.");
    }

    [Fact]
    public void GoNoGoScorecard_ShouldReferencePoint13_AsRestrictedSpaReadiness()
    {
        var scorecard = Read(Path.Combine(RepoRoot, "docs", "audits", "go-nogo-scorecard-funcional-normativo-2026-04-26.md"));
        scorecard.Should().Contain("Punto 13");
        scorecard.Should().Contain("SPA parcial");
        scorecard.Should().Contain("12D puede iniciar solo con restricciones");
        scorecard.Should().Contain("GO productivo: NO");
        scorecard.Should().Contain("NO-GO productivo vigente");
    }

    [Fact]
    public void AngularSpa_UatModulePresence_ShouldMatchGapMatrix()
    {
        var tokens = new[] { "uat", "evidence", "evidencias", "defects", "defectos", "approvers", "aprobadores", "scorecard" };
        var hits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in EnumerateSpaFiles())
        {
            var lower = Read(file).ToLowerInvariant();
            foreach (var t in tokens)
            {
                if (lower.Contains(t))
                    hits[t] = hits.TryGetValue(t, out var count) ? count + 1 : 1;
            }
        }

        var integralSignals = new[] { "uat", "evidence", "defects", "approvers", "scorecard" }.Count(k => hits.ContainsKey(k)) >= 5
            || new[] { "uat", "evidencias", "defectos", "aprobadores", "scorecard" }.Count(k => hits.ContainsKey(k)) >= 5;

        var matrix = Read(SpaMatrixPath);
        if (!integralSignals)
        {
            matrix.Should().Contain("Falta módulo UAT integral");
            return;
        }

        Assert.True(false, "Se detectan señales de módulo UAT integral en SPA; actualizar matriz, scorecard y guías antes de cambiar readiness.");
    }

    private static IEnumerable<string> EnumerateSpaFiles()
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "node_modules", "dist", "build", "coverage", "bin", "obj", ".angular", ".cache" };

        return Directory.EnumerateFiles(SpaRoot, "*.*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var rel = Path.GetRelativePath(SpaRoot, f).Replace('\\', '/');
                return !rel.Split('/').Any(excluded.Contains);
            })
            .Where(f => f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".scss", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string text, IEnumerable<string> needles)
        => needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static string[] ReadLines(string path) => File.ReadAllLines(path);

    private static string Read(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

    private static string ToRelative(string path) => Path.GetRelativePath(RepoRoot, path).Replace('\\', '/');

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ACHInterbank.sln"))) return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio (ACHInterbank.sln).");
    }
}
