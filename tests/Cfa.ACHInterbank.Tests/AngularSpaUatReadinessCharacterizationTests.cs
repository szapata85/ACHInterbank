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
        ShouldMatchRegexIgnoreCase(Read(packageJsonPath), "@angular/(core|)", "package.json debe declarar dependencias Angular");

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
        AssertNoAffirmativeDangerousLine(matrix, "SPA listo total", "no debe declararse readiness total SPA");
        AssertNoAffirmativeDangerousLine(matrix, "GO productivo: **Sí**", "no debe declararse GO productivo");
        AssertNoAffirmativeDangerousLine(matrix, "aprobado productivo", "no debe declararse aprobación productiva");
        AssertNoAffirmativeDangerousLine(matrix, "producción aprobada", "no debe declararse producción aprobada");
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
                    HasSafeNegativeBoundary(line).Should().BeTrue($"simulated liquidity cannot be treated as real CUD balance: {ToRelative(file)}:{i + 1}");
                }
            }
        }

        Read(SpaMatrixPath).Should().Contain("Liquidez simulada no equivale a saldo real CUD.");
    }

    [Fact]
    public void AngularSpa_ShouldNotExposeCertificateSecrets()
    {
        var visibleSecretTokens = new[] { "private key", "clave privada", "pemPrivateKey", "pfxPassword", "password certificado", "certificatePassword", "PFX password", "SecretRef crudo visible no enmascarado", "secretRef crudo visible no enmascarado" };
        var rawSecretRefTokens = new[] { "secretRef" };

        foreach (var file in EnumerateSpaFiles())
        {
            var lines = ReadLines(file);
            var isTemplate = IsTemplateFile(file);
            var isModel = IsModelFile(file);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (isTemplate)
                {
                    foreach (var token in visibleSecretTokens)
                    {
                        line.Contains(token, StringComparison.OrdinalIgnoreCase)
                            .Should().BeFalse($"secret token visible in template at {ToRelative(file)}:{i + 1}");
                    }

                    if (ContainsAny(line, rawSecretRefTokens) && !line.Contains("secretRefMasked", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Fail($"raw secretRef visible in template at {ToRelative(file)}:{i + 1}: {line}");
                    }
                }

                if (!isModel && !isTemplate)
                {
                    var lower = line.ToLowerInvariant();
                    var referencesSecret = ContainsAny(line, visibleSecretTokens) || line.Contains("privateKey", StringComparison.OrdinalIgnoreCase) || line.Contains("password", StringComparison.OrdinalIgnoreCase) || line.Contains("secretRef", StringComparison.OrdinalIgnoreCase);
                    if (!referencesSecret) continue;

                    var hasUnsafeStorage = lower.Contains("localstorage") || lower.Contains("sessionstorage");
                    var hasUnsafeLog = lower.Contains("console.log") || lower.Contains("console.error") || lower.Contains("console.warn") || lower.Contains("console.debug");
                    (hasUnsafeStorage || hasUnsafeLog).Should().BeFalse($"secrets must not be logged/stored in SPA code: {ToRelative(file)}:{i + 1}");
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
        ShouldMatchRegexIgnoreCase(matrix, "(Expuesto en SPA|Implementado en UI)", "la matriz debe documentar que el consumo SPA del export accounting-review ya está implementado");
        ShouldMatchRegexIgnoreCase(matrix, "pendiente validación UAT operativa|pendiente validación UAT", "la matriz debe mantener la compuerta de validación UAT operativa");
        ShouldMatchRegexIgnoreCase(matrix, "Mantener frontera no-contable|frontera no-contable", "la matriz debe mantener explícita la frontera no-contable");
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
        AssertNoAffirmativeDangerousLine(matrix, "UAT 100% SPA", "la matriz no debe declarar UAT 100% SPA afirmativo");
        AssertNoAffirmativeDangerousLine(matrix, "SPA listo total", "la matriz no debe declarar SPA listo total afirmativo");
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
    public void AngularSpa_ProdEnvironment_ShouldNotPointToLocalhost()
    {
        var envProd = Path.Combine(SpaRoot, "src", "environments", "environment.prod.ts");
        File.Exists(envProd).Should().BeTrue();

        var content = Read(envProd);
        content.Should().NotMatchRegex(@"https?://(localhost|127\.0\.0\.1|\[::1\])(:\d+)?", "production builds must not target a local API endpoint");
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
        ShouldContainIgnoreCase(matrix, "accounting-review export", "la matriz debe documentar la brecha de accounting-review export");
        ShouldContainIgnoreCase(matrix, "UAT package/downloads", "la matriz debe documentar brecha de paquete/descargas UAT");
        ShouldContainIgnoreCase(matrix, "CUD evidence boundary", "la matriz debe documentar la frontera de evidencia CUD");
        ShouldContainIgnoreCase(matrix, "naming externo", "la matriz debe documentar la brecha de naming externo");
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
        ShouldMatchRegexIgnoreCase(matrix, "Falta separación fina|No explícito por dominio|Granularidad de negocio limitada", "la matriz debe dejar explícita la parcialidad de granularidad de roles UAT");
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
        ShouldMatchRegexIgnoreCase(content, "Vigente|vigente", "el generador debe declarar vigencia de resguardos NO-GO");

        foreach (var sheet in new[] { "Instrucciones", "Casos_UAT", "Evidencias", "Defectos", "Aprobadores", "Resumen_Ejecucion", "Scorecard_UAT" })
            content.Should().Contain(sheet);
    }

    [Fact]
    public void SpaGapMatrix_ShouldKeepP1P2GapsVisible()
    {
        var matrix = NormalizeMarkdownInlineCode(Read(SpaMatrixPath));
        foreach (var p1 in new[]
        {
            "Falta módulo UAT integral", "Accounting-review export expuesto en SPA; pendiente validación UAT con usuarios y evidencias", "Roles UAT finos no evidentes",
            "Trazabilidad directa parcial/no confirmada", "CUD evidence boundary sin flujo UI integral"
        }) matrix.Should().Contain(p1);

        foreach (var p2 in new[]
        {
            "Semántica CUD/liquidez reforzada en SPA; pendiente validación UAT operativa con usuarios", "environment.prod.ts con IP fija", "Falta ayuda contextual y links a guías 12B"
        }) matrix.Should().Contain(p2);
    }

    [Fact]
    public void SpaGapMatrix_ShouldNotClaimFullSpaReadiness_WithoutRemovingRestrictions()
    {
        var matrix = Read(SpaMatrixPath);
        AssertNoAffirmativeDangerousLine(matrix, "SPA readiness: **Sí**", "La matriz SPA no puede declarar readiness total mientras conserve restricciones 12D/12E y brechas P1/P2");
        AssertNoAffirmativeDangerousLine(matrix, "SPA listo total", "La matriz SPA no puede declarar readiness total mientras conserve restricciones 12D/12E y brechas P1/P2");
        AssertNoAffirmativeDangerousLine(matrix, "12E: **Sí**", "La matriz SPA no puede declarar readiness total mientras conserve restricciones 12D/12E y brechas P1/P2");
        AssertNoAffirmativeDangerousLine(matrix, "UAT 100% SPA-only", "La matriz SPA no puede declarar readiness total mientras conserve restricciones 12D/12E y brechas P1/P2");
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

        Assert.Fail("Se detectan señales de módulo UAT integral en SPA; actualizar matriz, scorecard y guías antes de cambiar readiness.");
    }

    [Fact]
    public void AngularSpa_ShouldExposeAccountingReviewExportEndpointInReportsUi()
    {
        var allSpa = string.Join("\n", EnumerateSpaFiles().Select(Read));
        ShouldContainIgnoreCase(allSpa, "api/reports/accounting-review/export", "SPA debe consumir endpoint de exportación accounting-review");
        ShouldContainIgnoreCase(allSpa, "exportAccountingReview", "SPA debe exponer método de exportación accounting-review");
        ShouldContainIgnoreCase(allSpa, "'pdf'", "SPA debe soportar formato PDF");
        ShouldContainIgnoreCase(allSpa, "'csv'", "SPA debe soportar formato CSV");
        (allSpa.Contains("'excel'", StringComparison.OrdinalIgnoreCase) || allSpa.Contains("'xlsx'", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("SPA debe soportar formato Excel/XLSX");
        ShouldContainIgnoreCase(allSpa, "responseType: 'blob'", "SPA debe descargar archivo como blob");
        ShouldContainIgnoreCase(allSpa, "observe: 'response'", "SPA debe leer headers de respuesta");
    }

    [Fact]
    public void AngularSpa_AccountingReviewExportUi_ShouldDisplayNonAccountingDisclaimer()
    {
        var reportsUi = Read(Path.Combine(SpaRoot, "src", "app", "features", "reports", "components", "accounting-review-export.component.html"));
        ShouldContainIgnoreCase(reportsUi, "NO contabiliza", "UI debe indicar frontera no contable");
        (reportsUi.Contains("no genera asientos", StringComparison.OrdinalIgnoreCase) || reportsUi.Contains("no genera asiento", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("UI debe indicar que no genera asientos");
        (reportsUi.Contains("operativo", StringComparison.OrdinalIgnoreCase) || reportsUi.Contains("revisión", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("UI debe describir su propósito operativo/revisión");
        reportsUi.Contains("ledger", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        reportsUi.Contains("journal", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        reportsUi.Contains("posting", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }

    [Fact]
    public void AngularSpa_AccountingReviewExportUi_ShouldRespectCudBoundary()
    {
        var reportsUi = Read(Path.Combine(SpaRoot, "src", "app", "features", "reports", "components", "accounting-review-export.component.html"));
        ShouldContainIgnoreCase(reportsUi, "Evidencia CUD", "UI debe mencionar evidencia CUD");
        (reportsUi.Contains("operacional", StringComparison.OrdinalIgnoreCase) || reportsUi.Contains("manual", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("UI debe describir evidencia CUD como operacional/manual");
        ShouldContainIgnoreCase(reportsUi, "No representa API CUD", "UI debe aclarar que no representa API CUD");
        ShouldContainIgnoreCase(reportsUi, "no equivale a saldo real CUD", "UI debe negar equivalencia con saldo real CUD");
    }

    [Fact]
    public void AngularSpa_CenitUi_ShouldDisplayLiquidityCudBoundaryDisclaimer()
    {
        var cenitUi = Read(Path.Combine(SpaRoot, "src", "app", "features", "cenit", "components", "cenit-operation-page.component.html"));
        ShouldContainIgnoreCase(cenitUi, "Liquidez simulada no equivale a saldo real CUD");
        ShouldContainIgnoreCase(cenitUi, "internas y operativas");
        ShouldContainIgnoreCase(cenitUi, "no representan liquidación firme");
        ShouldContainIgnoreCase(cenitUi, "rechazo oficial CUD");
        (cenitUi.Contains("no representa rechazo oficial CUD", StringComparison.OrdinalIgnoreCase)
            || cenitUi.Contains("no representan rechazo oficial CUD", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("la advertencia CENIT/CUD debe negar explícitamente interpretación de rechazo oficial CUD");
    }

    [Fact]
    public void AngularSpa_CenitUi_ShouldTranslateOperationalLiquidityLabelsToSpanish()
    {
        var tsPath = Path.Combine(SpaRoot, "src", "app", "features", "cenit", "components", "cenit-operation-page.component.ts");
        var htmlPath = Path.Combine(SpaRoot, "src", "app", "features", "cenit", "components", "cenit-operation-page.component.html");
        var cenitContent = Read(tsPath) + "\n" + Read(htmlPath);
        ShouldContainIgnoreCase(cenitContent, "Liquidez simulada");
        ShouldContainIgnoreCase(cenitContent, "Posición neta");
        ShouldContainIgnoreCase(cenitContent, "Decisión interna de liquidez");
        ShouldContainIgnoreCase(cenitContent, "Evidencia CUD");
        (cenitContent.Contains("Diferido por liquidez", StringComparison.OrdinalIgnoreCase)
            || cenitContent.Contains("Rechazado internamente por liquidez", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public void AngularSpa_CenitUi_ShouldNotExposeDangerousCudAssertions()
    {
        var cenitRoot = Path.Combine(SpaRoot, "src", "app", "features", "cenit");
        var dangerous = new[] { "saldo real CUD", "liquidado CUD", "liquidación firme", "rechazo oficial CUD", "API CUD bancaria", "contabilizado", "asiento", "ledger", "journal", "posting" };

        foreach (var file in Directory.EnumerateFiles(cenitRoot, "*.*", SearchOption.AllDirectories)
                     .Where(f => f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".html", StringComparison.OrdinalIgnoreCase)))
        {
            var lines = ReadLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                foreach (var phrase in dangerous)
                {
                    if (!line.Contains(phrase, StringComparison.OrdinalIgnoreCase)) continue;
                    IsNegatedLine(line).Should().BeTrue($"frase riesgosa sin negación clara en {ToRelative(file)}:{i + 1}: {line}");
                }
            }
        }
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

    private static void ShouldMatchRegexIgnoreCase(string content, string pattern, string because = "")
    {
        Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            .Should().BeTrue(because);
    }

    private static bool IsNegatedLine(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.Contains("no ")
            || lower.Contains("no debe")
            || lower.Contains("no declara")
            || lower.Contains("sin ")
            || lower.Contains("not ")
            || lower.Contains("without ");
    }

    private static bool HasSafeNegativeBoundary(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.Contains("no equivale")
            || lower.Contains("no representa")
            || lower.Contains("no representan")
            || lower.Contains("sin equivalencia");
    }

    private static void AssertNoAffirmativeDangerousLine(string content, string dangerousPhrase, string because)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.Contains(dangerousPhrase, StringComparison.OrdinalIgnoreCase))
                continue;

            IsNegatedLine(line).Should().BeTrue($"{because}. Línea peligrosa sin negación clara: {i + 1}: {line}");
        }
    }

    private static void ShouldContainIgnoreCase(string content, string expected, string because = "")
    {
        content.Contains(expected, StringComparison.OrdinalIgnoreCase)
            .Should().BeTrue(because);
    }

    private static string NormalizeMarkdownInlineCode(string content) => content.Replace("`", "");

    private static bool IsModelFile(string file) => ToRelative(file).Contains("/models/", StringComparison.OrdinalIgnoreCase);

    private static bool IsTemplateFile(string file) => file.EndsWith(".html", StringComparison.OrdinalIgnoreCase);

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
