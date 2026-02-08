using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class TransactionCodeCatalogConfiguration : IEntityTypeConfiguration<TransactionCodeCatalog>
{
    public void Configure(EntityTypeBuilder<TransactionCodeCatalog> builder)
    {
        builder.ToTable("TransactionCodes");
        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(255);

        builder.HasData(
            new TransactionCodeCatalog { Code = "21", Name = "Devolución crédito cuenta corriente", Description = "Devolución de crédito a cuenta corriente." },
            new TransactionCodeCatalog { Code = "22", Name = "Crédito cuenta corriente", Description = "Crédito a cuenta corriente." },
            new TransactionCodeCatalog { Code = "23", Name = "Prenotificación crédito cuenta corriente", Description = "Prenotificación crédito a cuenta corriente." },
            new TransactionCodeCatalog { Code = "26", Name = "Devolución débito cuenta corriente", Description = "Devolución de débito a cuenta corriente." },
            new TransactionCodeCatalog { Code = "27", Name = "Débito cuenta corriente", Description = "Débito a cuenta corriente." },
            new TransactionCodeCatalog { Code = "28", Name = "Prenotificación débito cuenta corriente", Description = "Prenotificación débito a cuenta corriente." },
            new TransactionCodeCatalog { Code = "31", Name = "Devolución crédito cuenta ahorros", Description = "Devolución de crédito a cuenta de ahorros." },
            new TransactionCodeCatalog { Code = "32", Name = "Crédito cuenta ahorros", Description = "Crédito a cuenta de ahorros." },
            new TransactionCodeCatalog { Code = "33", Name = "Prenotificación crédito cuenta ahorros", Description = "Prenotificación crédito a cuenta de ahorros." },
            new TransactionCodeCatalog { Code = "36", Name = "Devolución débito cuenta ahorros", Description = "Devolución de débito a cuenta de ahorros." },
            new TransactionCodeCatalog { Code = "37", Name = "Débito cuenta ahorros", Description = "Débito a cuenta de ahorros." },
            new TransactionCodeCatalog { Code = "38", Name = "Prenotificación débito cuenta ahorros", Description = "Prenotificación débito a cuenta de ahorros." },
            new TransactionCodeCatalog { Code = "42", Name = "Crédito cuenta puente", Description = "Código especial para crédito en cuenta puente (p. ej. recaudos PSE)." },
            new TransactionCodeCatalog { Code = "51", Name = "Devolución crédito depósitos electrónicos", Description = "Devolución de crédito a depósitos electrónicos." },
            new TransactionCodeCatalog { Code = "52", Name = "Crédito depósitos electrónicos", Description = "Crédito a depósitos electrónicos." },
            new TransactionCodeCatalog { Code = "53", Name = "Prenotificación crédito depósitos electrónicos", Description = "Prenotificación crédito a depósitos electrónicos." },
            new TransactionCodeCatalog { Code = "55", Name = "Débito depósitos electrónicos", Description = "Débito a depósitos electrónicos." },
            new TransactionCodeCatalog { Code = "56", Name = "Devolución débito depósitos electrónicos", Description = "Devolución de débito a depósitos electrónicos." },
            new TransactionCodeCatalog { Code = "57", Name = "Prenotificación débito depósitos electrónicos", Description = "Prenotificación débito a depósitos electrónicos." }
        );
    }
}
