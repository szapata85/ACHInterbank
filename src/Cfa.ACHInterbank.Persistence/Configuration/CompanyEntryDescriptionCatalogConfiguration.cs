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
            .HasMaxLength(11)
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
            ("ADMON", "Pago de administración.", "PPD"),
            ("AHORROS", "Ahorros.", "PPD"),
            ("APORTES", "Pago de aportes.", "PPD"),
            ("ARRIENDOS", "Pago de arriendos.", "PPD"),
            ("ARRIENDO", "Pago de arriendos.", "PPD"),
            ("CELULAR", "Pago de telefonía móvil.", "PPD"),
            ("CELULARES", "Pago de telefonía móvil.", "PPD"),
            ("CESANTIAS", "Pago de cesantías.", "PPD"),
            ("CLUB", "Pago cuota club.", "PPD"),
            ("COLEGIO", "Pago de matrículas escolares.", "PPD"),
            ("COMISIONES", "Pago de comisiones.", "PPD"),
            ("COMISION", "Pago de comisiones.", "PPD"),
            ("CONTRATIST", "Pago de contratistas.", "PPD"),
            ("DIVIDENDOS", "Pago de dividendos – acciones.", "PPD"),
            ("DONACION", "Pago de donaciones.", "PPD"),
            ("HONORARIO", "Pago de honorarios.", "PPD"),
            ("IMPUESTOS", "Pago de impuestos.", "PPD"),
            ("INTERESES", "Pago de intereses.", "PPD"),
            ("NOMINA", "Pago de nómina.", "PPD"),
            ("PROVEEDOR", "Pago a proveedores.", "PPD"),
            ("PENSIONES", "Pago de pensiones.", "PPD"),
            ("PREPAGADA", "Pago medicina prepagada.", "PPD"),
            ("PRESTAMOS", "Pago de cuota de préstamos.", "PPD"),
            ("RENDIMIENT", "Pago de rendimientos.", "PPD"),
            ("RIESGOS P", "Riesgos Profesionales.", "PPD"),
            ("SEGUROS", "Pago de seguros.", "PPD"),
            ("SEGURO", "Pago de seguros.", "PPD"),
            ("SERV PUBLI", "Pago de servicios públicos.", "PPD"),
            ("SUSCRIPCI", "Pago de suscripciones.", "PPD"),
            ("TARCREDITO", "Pago de tarjeta de crédito.", "PPD"),
            ("TRASLADO", "Traslado de fondos.", "PPD"),
            ("TRASLADOS", "Traslado de fondos.", "PPD"),
            ("TV X CABL", "Pago televisión por cable.", "PPD"),
            ("TV SATELIT", "Pago de televisión satelital.", "PPD"),
            ("UNIVERSIDA", "Pago matrículas universitarias.", "PPD"),
            ("OTROS", "Cualquier otro tipo de pago no categorizado.", "PPD"),
            ("PAGOS PSE", "Para transacciones de comercio electrónico.", "CCD"),
            ("MULTICREDIT", "Nuevo concepto incluido en la v32 para transacciones crédito generadas por PSE.", "CCD"),
            ("COBROS PSE", "Para el recaudo de transacciones PSE.", "CCD"),
            ("PAGOS DIAN", "Para pagos tributarios.", "CCD"),
            ("SSS", "Para Seguridad Social.", "CCD"),
            ("COBROS SSS", "Para Seguridad Social.", "CCD")
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
