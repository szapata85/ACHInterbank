using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cfa.ACHInterbank.Tests;

public sealed class ThirdPartyMasterDataModelTests
{
    [Fact]
    public void CanonicalMaster_NormalizationAndAdministrativeDefaults_AreChamberAgnostic()
    {
        var thirdParty = new ThirdParty
        {
            PersonType = " pn ",
            DocumentType = " nit ",
            DocumentNumberNormalized = " ab-123 ",
            DisplayName = "  Receptor Uno  "
        };
        var account = new ThirdPartyAccount
        {
            AccountNumberNormalized = " 0012345 "
        };

        thirdParty.PersonType.Should().Be("PN");
        thirdParty.DocumentType.Should().Be("NIT");
        thirdParty.DocumentNumberNormalized.Should().Be("AB-123");
        thirdParty.DisplayName.Should().Be("Receptor Uno");
        account.AccountNumberNormalized.Should().Be("0012345");
        thirdParty.Status.Should().Be(ThirdPartyAdministrativeStatus.Active);
        account.Status.Should().Be(ThirdPartyAdministrativeStatus.Active);
    }

    [Fact]
    public void EfModel_DefinesCanonicalNaturalKeys_WithoutClearingHouseOrValidationDimensions()
    {
        using var context = CreateContext();

        var thirdParty = context.Model.FindEntityType(typeof(ThirdParty));
        thirdParty.Should().NotBeNull();
        var thirdPartyEntity = thirdParty!;
        thirdPartyEntity.GetTableName().Should().Be("ThirdParties");
        AssertUniqueIndex(
            thirdPartyEntity,
            "UX_ThirdParties_Customer_Document",
            nameof(ThirdParty.CustomerId),
            nameof(ThirdParty.DocumentType),
            nameof(ThirdParty.DocumentNumberNormalized));

        var account = context.Model.FindEntityType(typeof(ThirdPartyAccount));
        account.Should().NotBeNull();
        var accountEntity = account!;
        accountEntity.GetTableName().Should().Be("ThirdPartyAccounts");
        AssertUniqueIndex(
            accountEntity,
            "UX_ThirdPartyAccounts_ThirdParty_Institution_Type_Number",
            nameof(ThirdPartyAccount.ThirdPartyId),
            nameof(ThirdPartyAccount.FinancialInstitutionId),
            nameof(ThirdPartyAccount.AccountType),
            nameof(ThirdPartyAccount.AccountNumberNormalized));

        accountEntity.FindProperty(nameof(ThirdPartyAccount.AccountType))!.ClrType.Should().Be(typeof(AccountTypeEnum));

        var forbiddenFragments = new[]
        {
            "ClearingHouse",
            "TransactionNature",
            "ServiceType",
            "Prenotification",
            "Cycle",
            "Deadline",
            "Sla",
            "ReturnCode"
        };

        foreach (var entityType in new[] { thirdPartyEntity, accountEntity })
        {
            var names = entityType.GetProperties().Select(x => x.Name).ToArray();
            foreach (var fragment in forbiddenFragments)
            {
                names.Should().NotContain(name => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void EfModel_KeepsLegacyCustomerThirdPartyAlongsideCanonicalMaster()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(CustomerThirdParty)).Should().NotBeNull();
        context.Model.FindEntityType(typeof(CustomerThirdParty))!.GetTableName().Should().Be("CustomerThirdParties");
        context.Model.FindEntityType(typeof(ThirdParty)).Should().NotBeNull();
        context.Model.FindEntityType(typeof(ThirdPartyAccount)).Should().NotBeNull();
    }

    private static AchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new AchDbContext(options);
    }

    private static void AssertUniqueIndex(IEntityType entityType, string databaseName, params string[] propertyNames)
    {
        var index = entityType.GetIndexes()
            .Single(x => x.GetDatabaseName() == databaseName);

        index.IsUnique.Should().BeTrue();
        index.Properties.Select(x => x.Name).Should().Equal(propertyNames);
    }
}
