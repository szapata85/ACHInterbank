using System.Text.RegularExpressions;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public class S1TraceabilityMatrixTests
{
    private static readonly string RepoRoot = FindRepositoryRoot();
    private static readonly string MatrixPath = Path.Combine(RepoRoot, "docs", "audits", "s1-requirement-norm-code-test-evidence-closure-matrix-current.md");

    [Fact]
    public void TraceabilityMatrix_ShouldExist_AndReferenceNormativeSources()
    {
        File.Exists(MatrixPath).Should().BeTrue();

        var ach = Path.Combine(RepoRoot, "docs", "normativa", "md", "ACH-Colombia-V32.md");
        var dsp = Path.Combine(RepoRoot, "docs", "normativa", "md", "CENIT-DSP-152-Anexo-2.md");
        var anexoA = Path.Combine(RepoRoot, "docs", "normativa", "md", "CENIT-Anexo-A-Causales-Devolucion.md");
        var anexoB = Path.Combine(RepoRoot, "docs", "normativa", "md", "CENIT-Anexo-B-Causales-Rechazo.md");

        File.Exists(ach).Should().BeTrue();
        File.Exists(dsp).Should().BeTrue();
        File.Exists(anexoA).Should().BeTrue();
        File.Exists(anexoB).Should().BeTrue();

        var content = Read(MatrixPath);
        content.Should().Contain("docs/normativa/md/ACH-Colombia-V32.md");
        content.Should().Contain("docs/normativa/md/CENIT-DSP-152-Anexo-2.md");
        content.Should().Contain("docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md");
        content.Should().Contain("docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldContainAntiInferenceRules()
    {
        var content = Read(MatrixPath);
        content.Should().Contain("No se declara cumplimiento sin fuente localizada");
        content.Should().Contain("No se declara cierre sin prueba localizada");
        content.Should().Contain("No se declara cierre UAT sin evidencia/aprobación humana");
        content.Should().Contain("No se declara cierre productivo sin evidencias externas/operativas exigidas");
        content.Should().Contain("ACH Colombia y CENIT se separan cuando aplique");
        content.Should().Contain("Evidencia técnica automatizada no equivale a aprobación humana");
        content.Should().Contain("Evidencia UAT asistida por IA no equivale a firma de Operaciones/Negocio/Compliance");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldClassifyEvidenceTypes()
    {
        var content = Read(MatrixPath);
        content.Should().Contain("Prueba automatizada");
        content.Should().Contain("Evidencia técnica reproducible");
        content.Should().Contain("Evidencia UAT asistida por IA");
        content.Should().Contain("Evidencia UAT humana");
        content.Should().Contain("Evidencia externa/oficial");
        content.Should().Contain("Acta/aprobación formal");
        content.Should().Contain("Evidencia operacional productiva");
        content.Should().Contain("evidencia técnica automatizada por sí sola no habilita producción");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldNotDeclareAnyDomainAsFullyClosed()
    {
        var section = ExtractSection(Read(MatrixPath), "## 6. Matriz consolidada S1 por dominio y cámara", "## 7.");
        var s1Rows = section.Split('\n').Where(l => l.StartsWith("| S1-")).ToList();
        s1Rows.Should().NotBeEmpty();
        s1Rows.Should().OnlyContain(r => !r.Contains("| Cerrado trazablemente |"));
    }

    [Fact]
    public void TraceabilityMatrix_ShouldContainAllS1Domains()
    {
        var content = Read(MatrixPath);
        for (var i = 1; i <= 20; i++)
        {
            content.Should().Contain($"| S1-{i:00} |", $"falta fila de dominio S1-{i:00}");
        }
    }

    [Fact]
    public void TraceabilityMatrix_ShouldKeepCriticalDomainsBlocked()
    {
        var section = ExtractSection(Read(MatrixPath), "## 6. Matriz consolidada S1 por dominio y cámara", "## 7.");

        AssertBlockedRow(section, "S1-10", new[] { "Bloqueado", "NO-GO", "Neteo", "E2E", "CENIT" });
        AssertBlockedRow(section, "S1-11", new[] { "Bloqueado", "NO-GO", "Liquidez", "CUD" });
        AssertBlockedRow(section, "S1-12", new[] { "Bloqueado", "NO-GO", "Naming externo" });
        AssertBlockedRow(section, "S1-13", new[] { "Bloqueado", "NO-GO", "Sobre digital", "interoperabilidad", "externa" });
        AssertBlockedRow(section, "S1-20", new[] { "Bloqueado", "NO-GO", "UAT", "runbooks" });
    }

    [Fact]
    public void TraceabilityMatrix_ShouldSeparateAchAndCenitViews()
    {
        var content = Read(MatrixPath);
        content.Should().Contain("### 7.1 ACH Colombia");
        content.Should().Contain("### 7.2 CENIT");
        content.Should().Contain("### 7.3 Transversal");

        var achSection = ExtractSection(content, "### 7.1 ACH Colombia", "### 7.2");
        achSection.Should().Contain("ACH-Colombia-V32.md");
        achSection.Should().Contain("Parser/Builder NACHA-M");
        achSection.Should().Contain("Devoluciones");
        achSection.Should().Contain("ROR");
        achSection.Should().Contain("Ciclos ACH");
        achSection.Should().Contain("Naming externo");
        achSection.Should().Contain("Sobre/certificados");

        var cenitSection = ExtractSection(content, "### 7.2 CENIT", "### 7.3");
        cenitSection.Should().Contain("CENIT-DSP-152-Anexo-2.md");
        cenitSection.Should().Contain("CENIT-Anexo-A-Causales-Devolucion.md");
        cenitSection.Should().Contain("Causales");
        cenitSection.Should().Contain("Ciclos CENIT");
        cenitSection.Should().Contain("Neteo");
        cenitSection.Should().Contain("Liquidez/CUD boundary");
        cenitSection.Should().Contain("Naming CENIT");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldKeepAchAndCenitNormativeSourcesDistinct()
    {
        var content = Read(MatrixPath);
        var sources = ExtractSection(content, "## 3. Fuentes normativas localizadas", "## 4.");

        sources.Should().Contain("| ACH Colombia V3.2 | `docs/normativa/md/ACH-Colombia-V32.md` | ACH Colombia");
        sources.Should().Contain("| CENIT DSP-152 Anexo 2 | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | CENIT / Banco de la República");

        sources.Should().NotContain("ACH-Colombia-V32.md` | CENIT");
        sources.Should().NotContain("CENIT-DSP-152-Anexo-2.md` | ACH Colombia");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldKeepProductionNoGoVerdict()
    {
        var content = Read(MatrixPath);
        content.Should().Contain("GO técnico matriz: **Sí, limitado/controlado**");
        content.Should().Contain("GO UAT matriz: **Parcial/controlado**");
        content.Should().Contain("GO productivo: **NO**");
        content.Should().Contain("NO-GO productivo vigente");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldContainExitCriteriaForPoint11()
    {
        var content = Read(MatrixPath);
        content.Should().Contain("Todas las filas S1 con fuente localizada o brecha explícita");
        content.Should().Contain("Todas las filas S1 con código o “No aplica/No encontrado”");
        content.Should().Contain("Todas las filas S1 con prueba o brecha explícita");
        content.Should().Contain("Todas las filas S1 con evidencia clasificada");
        content.Should().Contain("Todas las filas S1 con cámara identificada");
        content.Should().Contain("ACH y CENIT no mezclados sin evidencia");
        content.Should().Contain("Checklists UAT referenciados");
        content.Should().Contain("Dueño/RACI referenciado");
        content.Should().Contain("Scorecard actualizado");
        content.Should().Contain("NO-GO productivo conservado si hay P0");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldReferenceRelatedUatChecklists()
    {
        var content = Read(MatrixPath);
        content.Should().Contain("docs/uat/nacha-records-acceptance-checklist.md");
        content.Should().Contain("docs/uat/cause-code-acceptance-checklist.md");
        content.Should().Contain("docs/uat/naming-returns-ror-acceptance-checklist.md");
        content.Should().Contain("docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md");
        content.Should().Contain("docs/uat/digital-envelope-certificate-acceptance-checklist.md");
        content.Should().Contain("docs/uat/accounting-review-reconciliation-acceptance-checklist.md");
    }

    [Fact]
    public void Scorecard_ShouldReferencePoint11ConsolidatedMatrix()
    {
        var scorecard = Read(Path.Combine(RepoRoot, "docs", "audits", "go-nogo-scorecard-funcional-normativo-2026-04-26.md"));
        scorecard.Should().Contain("Punto 11");
        scorecard.Should().Contain("docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md");
        scorecard.Should().Contain("GO técnico controlado");
        scorecard.Should().Contain("Estado productivo: NO-GO");
        scorecard.Should().Contain("S1-10, S1-11, S1-12, S1-13, S1-20");
    }

    [Fact]
    public void MasterMatrix_ShouldReferencePoint11ConsolidatedMatrix()
    {
        var master = Read(Path.Combine(RepoRoot, "docs", "audits", "s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md"));
        master.Should().Contain("Referencia consolidada punto 11");
        master.Should().Contain("docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md");
        master.Should().Contain("No cambia NO-GO productivo");
        master.Should().Contain("Ningún dominio se declara cerrado trazablemente si falta evidencia completa");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldKeepEvidenceAiAssistedSeparateFromHumanApproval()
    {
        var content = Read(MatrixPath);
        content.Should().Contain("Evidencia UAT asistida por IA");
        content.Should().ContainAny("no equivale a firma", "no equivale a aprobación humana");
        content.Should().Contain("Evidencia UAT humana");
        content.Should().Contain("Acta/aprobación formal");
    }

    [Fact]
    public void TraceabilityMatrix_ShouldNotUseForbiddenProductionClosureLanguage()
    {
        var content = Read(MatrixPath);
        var scope = ExtractSection(content, "## 6. Matriz consolidada S1 por dominio y cámara", "## 12.");

        scope.Should().NotContain("GO productivo: Sí");
        scope.Should().NotContain("Estado productivo: GO");
        scope.Should().NotContain("Cerrado productivo");
        scope.Should().NotContain("Aprobado productivo");
        scope.Should().NotContain("Cierre productivo completo");
        scope.Should().NotContain("Cumplimiento normativo completo");
        scope.Should().Contain("GO productivo: **NO**");
    }

    private static void AssertBlockedRow(string matrixSection, string domain, IEnumerable<string> mustContain)
    {
        var row = matrixSection.Split('\n').FirstOrDefault(l => l.StartsWith($"| {domain} |"));
        row.Should().NotBeNull($"no se encontró fila para {domain}");
        foreach (var token in mustContain)
            row!.Should().Contain(token);
    }

    private static string Read(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

    private static string ExtractSection(string content, string startHeading, string nextHeadingPrefix)
    {
        var start = content.IndexOf(startHeading, StringComparison.Ordinal);
        start.Should().BeGreaterOrEqualTo(0, $"no se encontró sección {startHeading}");

        var tail = content[start..];
        var next = tail.IndexOf("\n" + nextHeadingPrefix, StringComparison.Ordinal);
        return next >= 0 ? tail[..next] : tail;
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ACHInterbank.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio (ACHInterbank.sln).");
    }
}
