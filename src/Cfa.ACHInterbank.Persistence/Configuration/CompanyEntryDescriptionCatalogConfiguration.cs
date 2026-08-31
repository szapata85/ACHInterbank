using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CompanyEntryDescriptionCatalogConfiguration : IEntityTypeConfiguration<CompanyEntryDescriptionCatalog>
{
    public void Configure(EntityTypeBuilder<CompanyEntryDescriptionCatalog> builder)
    {
        builder.ToTable("CompanyEntryDescription");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Term)
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StandardEntryClassCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.Term)
            .IsUnique();

        builder.HasData(GetSeedData());
    }

    private static IEnumerable<CompanyEntryDescriptionCatalog> GetSeedData()
    {
        (string Term, string Description, string Sec)[] terms =
        [
            ("ADMONES", "Pagos de administración.", "PPD"),
            ("AHORROS", "Fondos destinados a ahorro.", "PPD"),
            ("APORTES", "Pagos de aportes.", "PPD"),
            ("ARRIENDOS", "Pagos de arrendamientos o arriendos.", "PPD"),
            ("BEEPERS", "Pagos de servicios de buscapersonas.", "PPD"),
            ("CEDULAS CP", "Pagos de cédulas de capitalización (Ajustado al límite de 10 caracteres).", "PPD"),
            ("CELULARES", "Pagos de servicios de telefonía móvil.", "PPD"),
            ("CESANTIAS", "Pagos de fondos de cesantías.", "PPD"),
            ("CLUBS", "Pagos de cuotas de clubes.", "PPD"),
            ("COLEGIOS", "Pagos de matrículas o pensiones escolares.", "PPD"),
            ("COMISIONES", "Pagos de comisiones.", "PPD"),
            ("CONTRATISTS", "Pagos a contratistas.", "PPD"),
            ("DIVIDENDOS", "Pagos de dividendos o acciones.", "PPD"),
            ("DONACIONES", "Pagos por concepto de donaciones.", "PPD"),
            ("HONORARIOS", "Pagos de servicios profesionales u honorarios.", "PPD"),
            ("IMPUESTOS", "Pagos de obligaciones tributarias.", "PPD"),
            ("INTERESES", "Pagos de rendimientos financieros o intereses.", "PPD"),
            ("NOMINAS", "Pagos de nóminas de empleados.", "PPD"),
            ("OTROS", "Otros tipos de pagos no categorizados.", "PPD"),
            ("PENSIONES", "Pagos de mesadas pensionales.", "PPD"),
            ("PREPAGADAS", "Pagos de servicios de medicina prepagada.", "PPD"),
            ("PRESTAMOS", "Pagos de cuotas de créditos o préstamos.", "PPD"),
            ("PROVEEDORS", "Pagos a proveedores (Ajustado al límite de 10 caracteres).", "PPD"),
            ("RENDIMIENTS", "Pagos de rendimientos financieros.", "PPD"),
            ("RIESGOS P", "Pagos de Riesgos Profesionales.", "PPD"),
            ("SEGUROS", "Pagos de primas de seguros.", "PPD"),
            ("SERVS PUBL", "Pagos de servicios públicos (Ajustado al límite de 10 caracteres).", "PPD"),
            ("SUSCRIPCIS", "Pagos de suscripciones a revistas o servicios.", "PPD"),
            ("TARCREDITS", "Pagos de cuotas de tarjetas de crédito.", "PPD"),
            ("TRASLADOS", "Transferencias de fondos entre cuentas (Obligatorio para personas naturales).", "PPD"),
            ("TVS X CABL", "Pagos de servicios de televisión por cable.", "PPD"),
            ("TVS SATEL", "Pagos de servicios de televisión satelital.", "PPD"),
            ("UNIVERSIDAS", "Pagos de matrículas o pensiones universitarias.", "PPD"),
            ("MULTICREDITS", "Nuevo concepto para transacciones crédito generadas por PSE.", "CCD"),
            ("PAGOS PSE", "Transacciones de comercio electrónico a través del botón de pagos.", "CCD"),
            ("COBROS PSE", "Recaudos originados por el sistema PSE.", "CCD"),
            ("PAGOS DIAN", "Pagos de impuestos a la Dirección de Impuestos y Aduanas Nacionales.", "CCD"),
            ("COBROS SSS", "Pagos y recaudos del Sistema de Seguridad Social", "CCD"),
            ("CORPORATE", "Pagos corporativos CENIT con múltiples adendas.", "CTX")
        ];

        var seed = new List<CompanyEntryDescriptionCatalog>();
        var id = 1;

        foreach (var term in terms)
        {
            seed.Add(new CompanyEntryDescriptionCatalog
            {
                Id = id++,
                Term = term.Term,
                Description = term.Description,
                StandardEntryClassCode = term.Sec,
                IsActive = true
            });
        }

        return seed;
    }
}
