using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class TransactionThirdPartyAccountSelectorTests
{
    private const string SourceAccount = "123456789012";
    private const int CompanyEntryDescriptionId = 600;

    [Theory]
    [InlineData(201, "ACH-CYCLE", 1, "ACHCOL", "ACH Colombia", "2026-10-12")]
    [InlineData(202, "CENIT-CYCLE", 2, "CENIT", "CENIT", "2026-10-13")]
    public async Task Selector_ResolvesAchColombiaAndCenitContext(
        int destinationInstitutionId,
        string cycleId,
        int clearingHouseId,
        string clearingHouseCode,
        string clearingHouseName,
        string expectedEffectiveDate)
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var rawAccount = destinationInstitutionId == 201 ? "987654321001" : "987654321002";
        var rawDocument = destinationInstitutionId == 201 ? "10101010" : "900222333";
        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(
                destinationInstitutionId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycleId);
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(
                It.IsAny<ThirdPartyAccountEligibilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThirdPartyAccountEligibilityRequest request, CancellationToken _) =>
                new ThirdPartyAccountEligibilityResult(
                    ThirdPartyAccountEligibilityDecision.Allowed,
                    "ELIGIBLE",
                    $"Elegible {rawAccount} {rawDocument}",
                    null,
                    ThirdPartyAccountValidationStatus.Accepted,
                    request.ThirdPartyAccountId,
                    null));

        var service = CreateService(context, routing.Object, eligibility.Object);
        var response = await service.GetAsync(new TransactionThirdPartyAccountSelectorRequest(
            SourceAccount,
            destinationInstitutionId,
            TransactionTypeEnum.Debit,
            CompanyEntryDescriptionId));

        Assert.Equal(100, response.CustomerId);
        Assert.Equal(destinationInstitutionId, response.DestinationInstitutionId);
        Assert.Equal(clearingHouseId, response.ClearingHouseId);
        Assert.Equal(clearingHouseCode, response.ClearingHouseCode);
        Assert.Equal(clearingHouseName, response.ClearingHouseName);
        Assert.Equal(TransactionNature.Debit, response.TransactionNature);
        Assert.Equal(DateTime.Parse(expectedEffectiveDate), response.EffectiveDate);
        Assert.Equal("PPD", response.ServiceType);
        var item = Assert.Single(response.Items);
        Assert.Equal(destinationInstitutionId, item.DestinationInstitutionId);
        Assert.True(item.IsSelectable);

        var serialized = JsonSerializer.Serialize(response);
        var maskedAccount = destinationInstitutionId == 201 ? "****1001" : "****1002";
        var maskedDocument = destinationInstitutionId == 201 ? "****1010" : "****2333";
        Assert.DoesNotContain(rawAccount, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(rawDocument, serialized, StringComparison.Ordinal);
        Assert.Contains(maskedAccount, serialized, StringComparison.Ordinal);
        Assert.Contains(maskedDocument, serialized, StringComparison.Ordinal);

        eligibility.Verify(x => x.EvaluateAsync(
            It.Is<ThirdPartyAccountEligibilityRequest>(request =>
                request.ClearingHouseId == clearingHouseId
                && request.TransactionNature == TransactionNature.Debit
                && request.EffectiveDate.Date == DateTime.Parse(expectedEffectiveDate)
                && request.ServiceType == "PPD"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Selector_IsolatesCandidatesToCustomerResolvedFromSourceAccount()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);

        var foreignCustomer = new Customer
        {
            Id = 110,
            FirstName = "Otro",
            LastName = "Cliente",
            CompanyName = "Otro Cliente",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "900000110"
        };
        var foreignThirdParty = new ThirdParty
        {
            Id = 311,
            CustomerId = foreignCustomer.Id,
            Customer = foreignCustomer,
            PersonType = "PN",
            DocumentType = "CC",
            DocumentNumberNormalized = "20202020",
            DisplayName = "Proveedor Ajeno",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var foreignAccount = new ThirdPartyAccount
        {
            Id = 411,
            ThirdPartyId = foreignThirdParty.Id,
            ThirdParty = foreignThirdParty,
            FinancialInstitutionId = seed.AchInstitution.Id,
            FinancialInstitution = seed.AchInstitution,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = "555544443333",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        foreignThirdParty.Accounts.Add(foreignAccount);
        context.Customers.Add(foreignCustomer);
        context.ThirdParties.Add(foreignThirdParty);
        context.ThirdPartyAccounts.Add(foreignAccount);
        await context.SaveChangesAsync();

        var routing = CreateRouting(seed.AchInstitution.Id, "ACH-CYCLE");
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(
                It.IsAny<ThirdPartyAccountEligibilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThirdPartyAccountEligibilityRequest request, CancellationToken _) =>
                new ThirdPartyAccountEligibilityResult(
                    ThirdPartyAccountEligibilityDecision.Allowed,
                    "ELIGIBLE",
                    "Elegible",
                    null,
                    ThirdPartyAccountValidationStatus.Accepted,
                    request.ThirdPartyAccountId,
                    null));

        var service = CreateService(context, routing.Object, eligibility.Object);
        var response = await service.GetAsync(new TransactionThirdPartyAccountSelectorRequest(
            SourceAccount,
            seed.AchInstitution.Id,
            TransactionTypeEnum.Debit,
            CompanyEntryDescriptionId));

        Assert.Equal(seed.Customer.Id, response.CustomerId);
        Assert.Single(response.Items);
        eligibility.Verify(x => x.EvaluateAsync(
            It.Is<ThirdPartyAccountEligibilityRequest>(request => request.ThirdPartyAccountId == seed.AchAccount.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        eligibility.Verify(x => x.EvaluateAsync(
            It.Is<ThirdPartyAccountEligibilityRequest>(request => request.ThirdPartyAccountId == foreignAccount.Id),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(TransactionTypeEnum.Credit, TransactionNature.Credit)]
    [InlineData(TransactionTypeEnum.Debit, TransactionNature.Debit)]
    public async Task Selector_PassesCreditDebitNatureToCanonicalEligibility(
        TransactionTypeEnum transactionType,
        TransactionNature expectedNature)
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var routing = CreateRouting(201, "ACH-CYCLE");
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(
                It.IsAny<ThirdPartyAccountEligibilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                transactionType == TransactionTypeEnum.Credit
                    ? ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification
                    : ThirdPartyAccountEligibilityDecision.Allowed,
                transactionType == TransactionTypeEnum.Credit ? "PRENOTIFICATION_OPTIONAL" : "ELIGIBLE",
                "Decisión backend",
                null,
                null,
                null,
                null));

        var service = CreateService(context, routing.Object, eligibility.Object);
        var response = await service.GetAsync(new TransactionThirdPartyAccountSelectorRequest(
            SourceAccount,
            201,
            transactionType,
            CompanyEntryDescriptionId));

        Assert.Equal(expectedNature, response.TransactionNature);
        Assert.True(Assert.Single(response.Items).IsSelectable);
        eligibility.Verify(x => x.EvaluateAsync(
            It.Is<ThirdPartyAccountEligibilityRequest>(request => request.TransactionNature == expectedNature),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Selector_DoesNotUseLegacyActiveToAuthorizeRejectedCanonicalAccount()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        var legacy = new CustomerThirdParty
        {
            Id = 900,
            CustomerId = seed.Customer.Id,
            Customer = seed.Customer,
            DestinationInstitutionId = seed.AchInstitution.Id,
            DestinationInstitution = seed.AchInstitution,
            DestinationAccountNumber = seed.AchAccount.AccountNumberNormalized,
            RecipientIdNumber = seed.AchThirdParty.DocumentNumberNormalized
        };
        legacy.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            null,
            "LEGACY-CYCLE",
            new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc),
            "legacy active",
            "legacy-evidence");
        context.CustomerThirdParties.Add(legacy);
        await context.SaveChangesAsync();

        var routing = CreateRouting(seed.AchInstitution.Id, "ACH-CYCLE");
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(
                It.IsAny<ThirdPartyAccountEligibilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.Rejected,
                "VALIDATION_REJECTED",
                "La última validación fue rechazada.",
                null,
                ThirdPartyAccountValidationStatus.Rejected,
                700,
                701));

        var service = CreateService(context, routing.Object, eligibility.Object);
        var response = await service.GetAsync(new TransactionThirdPartyAccountSelectorRequest(
            SourceAccount,
            seed.AchInstitution.Id,
            TransactionTypeEnum.Debit,
            CompanyEntryDescriptionId));

        var item = Assert.Single(response.Items);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.Rejected, item.EligibilityDecision);
        Assert.Equal("VALIDATION_REJECTED", item.EligibilityCode);
        Assert.Equal("La última validación fue rechazada.", item.EligibilityMessage);
        Assert.False(item.IsSelectable);
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, legacy.Status);
    }


    [Fact]
    public async Task Selector_EvaluatesEligibilityExactlyOncePerProjectedCandidate()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        var secondAccount = new ThirdPartyAccount
        {
            Id = 403,
            ThirdPartyId = seed.AchThirdParty.Id,
            ThirdParty = seed.AchThirdParty,
            FinancialInstitutionId = seed.AchInstitution.Id,
            FinancialInstitution = seed.AchInstitution,
            AccountType = AccountTypeEnum.Checking,
            AccountNumberNormalized = "987654321099",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        context.ThirdPartyAccounts.Add(secondAccount);
        await context.SaveChangesAsync();

        var routing = CreateRouting(seed.AchInstitution.Id, "ACH-CYCLE");
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(
                It.IsAny<ThirdPartyAccountEligibilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThirdPartyAccountEligibilityRequest request, CancellationToken _) =>
                new ThirdPartyAccountEligibilityResult(
                    ThirdPartyAccountEligibilityDecision.Allowed,
                    "ELIGIBLE",
                    "Elegible",
                    null,
                    ThirdPartyAccountValidationStatus.Accepted,
                    request.ThirdPartyAccountId,
                    null));

        var service = CreateService(context, routing.Object, eligibility.Object);
        var response = await service.GetAsync(new TransactionThirdPartyAccountSelectorRequest(
            SourceAccount,
            seed.AchInstitution.Id,
            TransactionTypeEnum.Debit,
            CompanyEntryDescriptionId));

        Assert.Equal(2, response.Items.Count);
        eligibility.Verify(x => x.EvaluateAsync(
            It.IsAny<ThirdPartyAccountEligibilityRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(2, eligibility.Invocations.Count);
    }

    [Fact]
    public async Task Selector_ProjectsCandidatesWithoutMaterializingCanonicalEntityGraphs()
    {
        var materialization = new EntityMaterializationCounter();
        await using var context = CreateContext(materialization);
        var seed = await SeedAsync(context);
        context.ChangeTracker.Clear();

        var routing = CreateRouting(seed.AchInstitution.Id, "ACH-CYCLE");
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(
                It.IsAny<ThirdPartyAccountEligibilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.Allowed,
                "ELIGIBLE",
                "Elegible",
                null,
                ThirdPartyAccountValidationStatus.Accepted,
                null,
                null));

        var service = CreateService(context, routing.Object, eligibility.Object);
        var response = await service.GetAsync(new TransactionThirdPartyAccountSelectorRequest(
            SourceAccount,
            seed.AchInstitution.Id,
            TransactionTypeEnum.Debit,
            CompanyEntryDescriptionId));

        Assert.Single(response.Items);
        Assert.Equal(0, materialization.Count<ThirdPartyAccount>());
        Assert.Equal(0, materialization.Count<ThirdParty>());
        Assert.Equal(0, materialization.Count<FinancialInstitution>());
        eligibility.Verify(x => x.EvaluateAsync(
            It.Is<ThirdPartyAccountEligibilityRequest>(request => request.ThirdPartyAccountId == seed.AchAccount.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static TransactionThirdPartyAccountSelectorService CreateService(
        AchDbContext context,
        IRoutingStrategyService routing,
        IThirdPartyAccountEligibilityService eligibility)
    {
        IAchCustomerRepository customers = new AchCustomerRepository(context);
        return new TransactionThirdPartyAccountSelectorService(
            context,
            customers,
            routing,
            eligibility);
    }

    private static Mock<IRoutingStrategyService> CreateRouting(int destinationInstitutionId, string cycleId)
    {
        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(
                destinationInstitutionId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycleId);
        return routing;
    }

    private static AchDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (interceptors.Length > 0)
        {
            options.AddInterceptors(interceptors);
        }

        return new AchDbContext(options.Options);
    }

    private static async Task<SelectorSeed> SeedAsync(AchDbContext context)
    {
        var customer = new Customer
        {
            Id = 100,
            FirstName = "Cliente",
            LastName = "Selector",
            CompanyName = "Cliente Selector",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "800555111"
        };
        var source = new CustomerAccount
        {
            Id = 101,
            CustomerId = customer.Id,
            Customer = customer,
            AccountNumber = SourceAccount
        };
        customer.Accounts.Add(source);

        var achInstitution = new FinancialInstitution
        {
            Id = 201,
            Name = "Banco ACH",
            RoutingNumber = "12345678",
            TransitCode = "001"
        };
        var cenitInstitution = new FinancialInstitution
        {
            Id = 202,
            Name = "Banco CENIT",
            RoutingNumber = "87654321",
            TransitCode = "002"
        };
        var ach = new ClearingHouse
        {
            Id = 1,
            ClearingHouseId = 1,
            Code = "ACHCOL",
            Name = "ACH Colombia",
            OriginCode = "00000001"
        };
        var cenit = new ClearingHouse
        {
            Id = 2,
            ClearingHouseId = 2,
            Code = "CENIT",
            Name = "CENIT",
            OriginCode = "00000002"
        };
        var achCycle = new AchCycle
        {
            Id = "ACH-CYCLE",
            CycleName = "ACH Cycle",
            ProcessingDate = new DateTime(2026, 10, 12),
            StartTime = new TimeSpan(8, 0, 0),
            CutoffTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            ClearingHouseId = ach.Id,
            ClearingHouse = ach
        };
        var cenitCycle = new AchCycle
        {
            Id = "CENIT-CYCLE",
            CycleName = "CENIT Cycle",
            ProcessingDate = new DateTime(2026, 10, 13),
            StartTime = new TimeSpan(11, 0, 0),
            CutoffTime = new TimeSpan(13, 0, 0),
            EndTime = new TimeSpan(13, 0, 0),
            ClearingHouseId = cenit.Id,
            ClearingHouse = cenit
        };
        var achThirdParty = new ThirdParty
        {
            Id = 301,
            CustomerId = customer.Id,
            Customer = customer,
            PersonType = "PN",
            DocumentType = "CC",
            DocumentNumberNormalized = "10101010",
            DisplayName = "Proveedor ACH",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var cenitThirdParty = new ThirdParty
        {
            Id = 302,
            CustomerId = customer.Id,
            Customer = customer,
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumberNormalized = "900222333",
            DisplayName = "Proveedor CENIT",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var achAccount = new ThirdPartyAccount
        {
            Id = 401,
            ThirdPartyId = achThirdParty.Id,
            ThirdParty = achThirdParty,
            FinancialInstitutionId = achInstitution.Id,
            FinancialInstitution = achInstitution,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = "987654321001",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var cenitAccount = new ThirdPartyAccount
        {
            Id = 402,
            ThirdPartyId = cenitThirdParty.Id,
            ThirdParty = cenitThirdParty,
            FinancialInstitutionId = cenitInstitution.Id,
            FinancialInstitution = cenitInstitution,
            AccountType = AccountTypeEnum.Checking,
            AccountNumberNormalized = "987654321002",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        achThirdParty.Accounts.Add(achAccount);
        cenitThirdParty.Accounts.Add(cenitAccount);
        var description = new CompanyEntryDescriptionCatalog
        {
            Id = CompanyEntryDescriptionId,
            Term = "PAGOS",
            Description = "Pagos PPD",
            StandardEntryClassCode = "PPD",
            IsActive = true
        };

        context.Customers.Add(customer);
        context.CustomerAccounts.Add(source);
        context.FinancialInstitutions.AddRange(achInstitution, cenitInstitution);
        context.ClearingHouses.AddRange(ach, cenit);
        context.AchCycles.AddRange(achCycle, cenitCycle);
        context.ThirdParties.AddRange(achThirdParty, cenitThirdParty);
        context.ThirdPartyAccounts.AddRange(achAccount, cenitAccount);
        context.CompanyEntryDescriptionCatalogs.Add(description);
        await context.SaveChangesAsync();

        return new SelectorSeed(customer, achInstitution, cenitInstitution, achThirdParty, achAccount);
    }

    private sealed class EntityMaterializationCounter : IMaterializationInterceptor
    {
        private readonly Dictionary<Type, int> _counts = [];

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            var type = entity.GetType();
            _counts[type] = _counts.TryGetValue(type, out var count) ? count + 1 : 1;
            return entity;
        }

        public int Count<T>() => _counts.TryGetValue(typeof(T), out var count) ? count : 0;
    }

    private sealed record SelectorSeed(
        Customer Customer,
        FinancialInstitution AchInstitution,
        FinancialInstitution CenitInstitution,
        ThirdParty AchThirdParty,
        ThirdPartyAccount AchAccount);
}
