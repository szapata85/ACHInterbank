using System.Data.Common;
using System.Diagnostics.Metrics;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchBulkTransactionServiceTests
{
    private const int TestClearingHouseConfigId = 9001;
    private const int TestClearingHouseId = 9001;
    private const int TestSourceInstitutionId = 9101;
    private const int TestDestinationInstitutionId = 9102;
    private const string TestCycleId = "CYCLE-1";
    private const int TestBatchId = 9201;

    [Fact]
    public async Task RegisterBulkAsync_ReturnsTotalSuccess_WhenAllItemsAreValid()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var logger = new CapturingLogger<AchBulkTransactionService>();
        using var metrics = new MetricCapture("Cfa.ACHInterbank.ACH.Bulk");
        var service = CreateService(context, companyEntryDescriptionId, loggerOverride: logger);
        var request = BuildRequest("BULK-OK", 2, companyEntryDescriptionId);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(2, response.TotalReceived);
        Assert.Equal(2, response.TotalSucceeded);
        Assert.Equal(0, response.TotalFailed);
        Assert.All(response.ItemResults, item => Assert.True(item.Succeeded));
        Assert.True(metrics.Contains("ach_bulk_operations_total", "outcome", "accepted"));
        Assert.True(metrics.Count("ach_bulk_items_total", "outcome", "accepted") >= 2);
        Assert.Contains(logger.Messages, message => message.Contains("outcome accepted", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message =>
            request.Transactions.Any(item =>
                message.Contains(item.SourceAccountNumber, StringComparison.Ordinal)
                || message.Contains(item.DestinationAccountNumber.Trim(), StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(item.RecipientIdNumber)
                    && message.Contains(item.RecipientIdNumber, StringComparison.Ordinal))));
    }

    [Fact]
    public async Task RegisterBulkAsync_ReturnsPartialSuccess_WhenOneItemFailsValidation()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var validator = new Mock<ITransactionValidator>();
        validator
            .Setup(v => v.ValidateRequest(It.Is<AchTransactionRequestData>(x => x.Reference == "BULK-PARTIAL-0002"), It.IsAny<IReadOnlySet<int>?>()))
            .Throws(new ArgumentException("Referencia inválida."));

        var logger = new CapturingLogger<AchBulkTransactionService>();
        using var metrics = new MetricCapture("Cfa.ACHInterbank.ACH.Bulk");
        var service = CreateService(context, companyEntryDescriptionId, validator: validator, loggerOverride: logger);
        var request = BuildRequest("BULK-PARTIAL", 3, companyEntryDescriptionId);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(3, response.TotalProcessed);
        Assert.Equal(2, response.TotalSucceeded);
        Assert.Equal(1, response.TotalFailed);
        Assert.Contains(response.ItemResults, x => !x.Succeeded && x.ErrorCode == "ITEM_VALIDATION_FAILED");
        Assert.True(metrics.Contains("ach_bulk_operations_total", "outcome", "partial"));
        Assert.True(metrics.Contains("ach_bulk_items_total", "outcome", "rejected"));
        Assert.True(metrics.Contains("ach_bulk_items_total", "stage", "request_validation"));
        Assert.Contains(logger.Messages, message =>
            message.Contains("item 1", StringComparison.Ordinal)
            && message.Contains("outcome rejected", StringComparison.Ordinal)
            && message.Contains("request_validation", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RegisterBulkAsync_ReturnsTotalFailure_WhenAllItemsFailValidation()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var validator = new Mock<ITransactionValidator>();
        validator
            .Setup(v => v.ValidateRequest(It.IsAny<AchTransactionRequestData>(), It.IsAny<IReadOnlySet<int>?>()))
            .Throws(new ArgumentException("Lote inválido."));

        var service = CreateService(context, companyEntryDescriptionId, validator: validator);
        var request = BuildRequest("BULK-FAIL", 2, companyEntryDescriptionId);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(2, response.TotalFailed);
    }

    [Fact]
    public async Task RegisterBulkAsync_SilentlyCreatesCanonicalThirdPartyAndAccount_ThroughSharedOnboardingService()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var customerRepository = new AchCustomerRepository(context);
        var onboarding = new ThirdPartyOnboardingService(
            context,
            Mock.Of<ILogger<ThirdPartyOnboardingService>>());
        var service = CreateService(
            context,
            companyEntryDescriptionId,
            thirdPartyOnboardingServiceOverride: onboarding,
            customerRepositoryInstance: customerRepository);
        var request = BuildRequest("TPA2-BULK", 1, companyEntryDescriptionId);
        request.Transactions[0].AccountType = AccountTypeEnum.ElectronicDeposits;
        request.Transactions[0].DestinationAccountNumber = " 8800012345 ";

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(1, response.TotalSucceeded);
        var thirdParty = await context.ThirdParties.SingleAsync();
        var account = await context.ThirdPartyAccounts.SingleAsync();
        var sourceCustomerId = await context.Customers
            .Where(x => x.DocumentNumber == "900123456")
            .Select(x => x.Id)
            .SingleAsync();
        Assert.Equal(sourceCustomerId, thirdParty.CustomerId);
        Assert.Equal(thirdParty.Id, account.ThirdPartyId);
        Assert.Equal(TestDestinationInstitutionId, account.FinancialInstitutionId);
        Assert.Equal(AccountTypeEnum.ElectronicDeposits, account.AccountType);
        Assert.Equal("8800012345", account.AccountNumberNormalized);
        Assert.Equal(ThirdPartyAccountOriginSource.BulkTransaction, account.OriginSource);
    }

    [Fact]
    public async Task RegisterBulkAsync_FailsItems_WhenDuplicateReferencesExistInRequestOrPersistence()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        context.AchTransactions.Add(new AchTransaction
        {
            Amount = 100,
            Reference = "BULK-DUP-0002",
            Type = TransactionTypeEnum.Credit,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            TransactionCode = "22",
            OriginatingDFI = "000010070",
            ReceivingDFI = "000010010",
            TraceNumber = "000010070000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = DateTime.Today,
            SourceInstitutionId = TestSourceInstitutionId,
            DestinationInstitutionId = TestDestinationInstitutionId,
            AchCycleId = TestCycleId,
            AchBatchId = TestBatchId
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, companyEntryDescriptionId);
        var request = BuildRequest("BULK-DUP", 3, companyEntryDescriptionId);
        request.Transactions[2].Reference = request.Transactions[0].Reference;

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(3, response.TotalFailed);
        Assert.Contains(response.ItemResults, x => x.ErrorMessage!.Contains("duplicad", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.ItemResults, x => x.ErrorMessage!.Contains("ya exis", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterBulkAsync_FailsItem_WhenIntegratedTransactionPolicyRejectsCfaOperationalRestriction()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var operationalDate = new DateTime(2026, 8, 3);
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == TestCycleId);
        cycle.ProcessingDate = operationalDate;
        var cycleConfig = await context.ClearingHouseCycleConfigs.SingleAsync(x => x.Id == cycle.ClearingHouseCycleConfigId);
        cycleConfig.EffectiveFrom = operationalDate;
        cycleConfig.EffectiveTo = operationalDate;
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseId = TestClearingHouseId,
            TransactionType = TransactionTypeEnum.Debit,
            AllowedAccountTypes = [AccountTypeEnum.Savings],
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(
                TestDestinationInstitutionId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestCycleId);
        var clock = new FixedTimeProvider(
            new DateTimeOffset(2026, 8, 3, 17, 0, 0, TimeSpan.Zero),
            TimeZoneInfo.Utc);
        var policy = new TransactionPolicyService(
            context,
            routing.Object,
            new CfaOperationalTransactionRestrictionResolver(context),
            clock);

        var service = CreateService(context, companyEntryDescriptionId, policyService: policy);
        var request = BuildRequest("BULK-CFA-DENY", 1, companyEntryDescriptionId);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(1, response.TotalFailed);
        Assert.Contains("restricción operacional CFA", response.ItemResults.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterBulkAsync_Prenotification_OnboardsMissingCustomersAccountsAndThirdParty()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var customerRepository = CreateCustomerRepositoryMock();
        var addedCustomers = new List<Customer>();
        customerRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((customer, _) => addedCustomers.Add(customer))
            .Returns(Task.CompletedTask);
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string accountNumber, CancellationToken _) =>
                addedCustomers.FirstOrDefault(customer => customer.Accounts.Any(account => account.AccountNumber == accountNumber)));

        CustomerThirdParty? addedThirdParty = null;
        var thirdPartyRepository = new Mock<ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerThirdParty?)null);
        thirdPartyRepository
            .Setup(repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()))
            .Callback<CustomerThirdParty, CancellationToken>((thirdParty, _) => addedThirdParty = thirdParty)
            .Returns(Task.CompletedTask);

        var prenotificationHandler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object);
        var service = CreateService(
            context,
            companyEntryDescriptionId,
            customerRepositoryOverride: customerRepository,
            prenotificationHandlerOverride: prenotificationHandler);
        var request = BuildRequest("BULK-TPA0-NEW", 1, companyEntryDescriptionId);
        request.Transactions[0].IsPrenotification = true;
        request.Transactions[0].Amount = 0m;

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(1, response.TotalSucceeded);
        Assert.Equal(2, addedCustomers.Count);
        var sourceCustomer = Assert.Single(addedCustomers.Where(customer => customer.DocumentNumber == "900123456"));
        Assert.Contains(sourceCustomer.Accounts, account => account.AccountNumber == "12345000001");
        var recipientCustomer = Assert.Single(addedCustomers.Where(customer => customer.DocumentNumber == "1000000001"));
        Assert.Contains(recipientCustomer.Accounts, account => account.AccountNumber == "98765000001");
        Assert.NotNull(addedThirdParty);
        Assert.Same(sourceCustomer, addedThirdParty!.Customer);
        Assert.Equal(TestDestinationInstitutionId, addedThirdParty.DestinationInstitutionId);
        Assert.Equal("98765000001", addedThirdParty.DestinationAccountNumber);
        Assert.Equal("1000000001", addedThirdParty.RecipientIdNumber);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, addedThirdParty.Status);
    }

    [Fact]
    public async Task RegisterBulkAsync_Prenotification_ReusesExistingCustomersAccountsAndPendingThirdParty()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var sourceCustomer = new Customer
        {
            FirstName = "EMPRESA",
            LastName = "N/A",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "900123456",
            CompanyName = "EMPRESA"
        };
        sourceCustomer.Accounts.Add(new CustomerAccount { AccountNumber = "12345000001" });
        var recipientCustomer = new Customer
        {
            FirstName = "CLIENTE 1",
            LastName = "NO IDENTIFICADO",
            PersonType = "PN",
            DocumentType = "CC",
            DocumentNumber = "1000000001"
        };
        recipientCustomer.Accounts.Add(new CustomerAccount { AccountNumber = "98765000001" });
        context.Customers.AddRange(sourceCustomer, recipientCustomer);
        await context.SaveChangesAsync();

        var customerRepository = CreateCustomerRepositoryMock();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("12345000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceCustomer);

        var existingThirdParty = new CustomerThirdParty
        {
            Id = 77,
            CustomerId = sourceCustomer.Id,
            Customer = sourceCustomer,
            DestinationInstitutionId = TestDestinationInstitutionId,
            DestinationAccountNumber = "98765000001",
            RecipientIdNumber = "1000000001"
        };
        var thirdPartyRepository = new Mock<ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .Setup(repository => repository.FindAsync(
                sourceCustomer.Id,
                TestDestinationInstitutionId,
                "98765000001",
                "1000000001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingThirdParty);

        var prenotificationHandler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object);
        var service = CreateService(
            context,
            companyEntryDescriptionId,
            customerRepositoryOverride: customerRepository,
            prenotificationHandlerOverride: prenotificationHandler);
        var request = BuildRequest("BULK-TPA0-EXISTING", 1, companyEntryDescriptionId);
        request.Transactions[0].IsPrenotification = true;
        request.Transactions[0].Amount = 0m;

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(1, response.TotalSucceeded);
        customerRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
        thirdPartyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, existingThirdParty.Status);
        Assert.Equal(101, existingThirdParty.PrenotificationTransactionId);
        Assert.Single(sourceCustomer.Accounts);
        Assert.Single(recipientCustomer.Accounts);
    }

    [Fact]
    public async Task RegisterBulkAsync_MandatoryDebitEligibilityFailure_FailsItemBeforePersistence()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var rule = new ClearingHouseTransactionRule
        {
            Id = 7201,
            ClearingHouseId = TestClearingHouseId,
            TransactionNature = TransactionNature.Debit,
            TransactionType = TransactionTypeEnum.Debit,
            PrenotificationMode = PrenotificationRequirementMode.Mandatory,
            AppliesToMonetaryTransactions = true,
            AppliesToNachaExport = true,
            EffectiveFrom = DateTime.Today,
            IsActive = true,
            NormativeSource = "TEST",
            NormativeReference = "TEST"
        };
        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(TestClearingHouseId, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int clearingHouseId, TransactionNature ignoredNature, DateTime effectiveDate, CancellationToken ignoredCt) =>
                new ClearingHouseValidationPolicyResolution(rule, effectiveDate.Date, TransactionNature.Debit, clearingHouseId));
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(It.IsAny<ThirdPartyAccountEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.RequiresPrenotification,
                "PRENOTIFICATION_REQUIRED",
                "La política efectiva exige una prenotificación válida.",
                rule,
                null,
                null,
                null));
        var prerequisite = new TransactionPrerequisitePolicyService(context, Mock.Of<IBankHoliday>());
        var logger = new CapturingLogger<AchBulkTransactionService>();
        using var metrics = new MetricCapture("Cfa.ACHInterbank.ACH.Bulk");

        var service = CreateService(
            context,
            companyEntryDescriptionId,
            clearingHouseValidationPolicyServiceOverride: policy.Object,
            thirdPartyAccountEligibilityServiceOverride: eligibility.Object,
            transactionPrerequisitePolicyServiceOverride: prerequisite,
            loggerOverride: logger);

        var response = await service.RegisterBulkAsync(BuildRequest("TPA7A-BULK-BLOCK", 1, companyEntryDescriptionId));

        Assert.Equal(1, response.TotalFailed);
        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal("ITEM_VALIDATION_FAILED", response.ItemResults.Single().ErrorCode);
        Assert.Contains("TRANSACTION_PRENOTIFICATION_REQUIRED", response.ItemResults.Single().ErrorMessage, StringComparison.Ordinal);
        Assert.True(metrics.Contains("ach_bulk_items_total", "outcome", "rejected"));
        Assert.True(metrics.Contains("ach_bulk_items_total", "stage", "canonical_prerequisite"));
        Assert.True(metrics.Contains("ach_bulk_operations_total", "outcome", "rejected"));
        Assert.Contains(logger.Messages, message =>
            message.Contains("outcome rejected", StringComparison.Ordinal)
            && message.Contains("canonical_prerequisite", StringComparison.Ordinal));
        eligibility.Verify(x => x.EvaluateAsync(
            It.Is<ThirdPartyAccountEligibilityRequest>(request =>
                request.ClearingHouseId == TestClearingHouseId
                && request.TransactionNature == TransactionNature.Debit
                && request.EffectiveDate.Date == DateTime.Today
                && request.ServiceType == "PPD"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterBulkAsync_OptionalCreditWithoutPrenote_RemainsAllowedServerSide()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var rule = new ClearingHouseTransactionRule
        {
            Id = 7202,
            ClearingHouseId = TestClearingHouseId,
            TransactionNature = TransactionNature.Credit,
            TransactionType = TransactionTypeEnum.Credit,
            PrenotificationMode = PrenotificationRequirementMode.Optional,
            AppliesToMonetaryTransactions = true,
            AppliesToNachaExport = true,
            EffectiveFrom = DateTime.Today,
            IsActive = true,
            NormativeSource = "TEST",
            NormativeReference = "TEST"
        };
        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(TestClearingHouseId, TransactionNature.Credit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int clearingHouseId, TransactionNature ignoredNature, DateTime effectiveDate, CancellationToken ignoredCt) =>
                new ClearingHouseValidationPolicyResolution(rule, effectiveDate.Date, TransactionNature.Credit, clearingHouseId));
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(It.IsAny<ThirdPartyAccountEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification,
                "PRENOTIFICATION_OPTIONAL",
                "La política efectiva permite crédito sin prenotificación.",
                rule,
                null,
                null,
                null));
        var prerequisite = new TransactionPrerequisitePolicyService(context, Mock.Of<IBankHoliday>());
        var legacyCatalog = new Mock<IAchRegulatoryCatalogService>();
        legacyCatalog.Setup(x => x.IsPrenotificationRequiredAsync(TransactionTypeEnum.Credit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = BuildRequest("TPA7A-BULK-CREDIT", 1, companyEntryDescriptionId);
        request.Transactions[0].Type = TransactionTypeEnum.Credit;

        var service = CreateService(
            context,
            companyEntryDescriptionId,
            clearingHouseValidationPolicyServiceOverride: policy.Object,
            thirdPartyAccountEligibilityServiceOverride: eligibility.Object,
            transactionPrerequisitePolicyServiceOverride: prerequisite,
            catalogServiceOverride: legacyCatalog.Object);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(1, response.TotalSucceeded);
        Assert.Equal(0, response.TotalFailed);
        Assert.True(response.ItemResults.Single().Succeeded);
        legacyCatalog.Verify(
            x => x.IsPrenotificationRequiredAsync(TransactionTypeEnum.Credit, It.IsAny<CancellationToken>()),
            Times.Once);
        eligibility.Verify(x => x.EvaluateAsync(
            It.Is<ThirdPartyAccountEligibilityRequest>(request =>
                request.ClearingHouseId == TestClearingHouseId
                && request.TransactionNature == TransactionNature.Credit
                && request.EffectiveDate.Date == DateTime.Today
                && request.ServiceType == "PPD"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterBulkAsync_AllowedValidation_RecordsMonetaryUse()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var rule = new ClearingHouseTransactionRule
        {
            Id = 7203,
            ClearingHouseId = TestClearingHouseId,
            TransactionNature = TransactionNature.Debit,
            TransactionType = TransactionTypeEnum.Debit,
            PrenotificationMode = PrenotificationRequirementMode.Mandatory,
            AppliesToMonetaryTransactions = true,
            AppliesToNachaExport = true,
            EffectiveFrom = DateTime.Today,
            IsActive = true,
            NormativeSource = "TEST",
            NormativeReference = "TEST"
        };
        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(TestClearingHouseId, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int clearingHouseId, TransactionNature ignoredNature, DateTime effectiveDate, CancellationToken ignoredCt) =>
                new ClearingHouseValidationPolicyResolution(rule, effectiveDate.Date, TransactionNature.Debit, clearingHouseId));
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(It.IsAny<ThirdPartyAccountEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.Allowed,
                "ELIGIBLE",
                "La validación contextual autoriza la transacción.",
                rule,
                ThirdPartyAccountValidationStatus.Accepted,
                ValidationId: 91,
                AttemptId: 92));
        var validation = new Mock<IThirdPartyAccountValidationService>();
        validation.Setup(x => x.RecordMonetaryUseAsync(It.IsAny<ThirdPartyAccountMonetaryUseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var prerequisite = new TransactionPrerequisitePolicyService(context, Mock.Of<IBankHoliday>());

        var service = CreateService(
            context,
            companyEntryDescriptionId,
            clearingHouseValidationPolicyServiceOverride: policy.Object,
            thirdPartyAccountEligibilityServiceOverride: eligibility.Object,
            transactionPrerequisitePolicyServiceOverride: prerequisite,
            thirdPartyAccountValidationServiceOverride: validation.Object);

        var response = await service.RegisterBulkAsync(BuildRequest("TPA7A-BULK-USE", 1, companyEntryDescriptionId));

        Assert.Equal(1, response.TotalSucceeded);
        validation.Verify(x => x.RecordMonetaryUseAsync(
            It.Is<ThirdPartyAccountMonetaryUseRequest>(r => r.ThirdPartyAccountId == 1 && r.ValidationId == 91),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterBulkAsync_WhenItemFailsAfterCanonicalOnboarding_RollsBackOnlyThatItemAndDoesNotContaminateNextItem()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var customerRepository = new AchCustomerRepository(context);
        var onboarding = new ThirdPartyOnboardingService(
            context,
            Mock.Of<ILogger<ThirdPartyOnboardingService>>());
        var prerequisite = new Mock<ITransactionPrerequisitePolicyService>();
        prerequisite
            .SetupSequence(x => x.ValidateForRegistration(It.IsAny<TransactionRegistrationPrerequisiteRequest>()))
            .Returns(new TransactionPrerequisiteValidationResult(false, "TPA9A2_ITEM_REJECTED", "Fallo controlado después del onboarding canónico.", null))
            .Returns(new TransactionPrerequisiteValidationResult(true, "OK", "Validación permitida.", null));

        var service = CreateService(
            context,
            companyEntryDescriptionId,
            thirdPartyOnboardingServiceOverride: onboarding,
            transactionPrerequisitePolicyServiceOverride: prerequisite.Object,
            customerRepositoryInstance: customerRepository);
        var request = BuildRequest("TPA9A2-ITEM-ROLLBACK", 2, companyEntryDescriptionId);
        var failedRecipientDocument = request.Transactions[0].RecipientIdNumber!;
        var successfulRecipientDocument = request.Transactions[1].RecipientIdNumber!;
        var failedSourceAccount = request.Transactions[0].SourceAccountNumber;
        var successfulSourceAccount = request.Transactions[1].SourceAccountNumber;
        var failedDestinationAccount = request.Transactions[0].DestinationAccountNumber.Trim();
        var successfulDestinationAccount = request.Transactions[1].DestinationAccountNumber.Trim();

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(1, response.TotalSucceeded);
        Assert.Equal(1, response.TotalFailed);
        Assert.Contains(response.ItemResults, x => !x.Succeeded && x.ErrorMessage!.Contains("TPA9A2_ITEM_REJECTED", StringComparison.Ordinal));
        Assert.Contains(response.ItemResults, x => x.Succeeded && x.Index == 1);

        Assert.False(await context.Customers.AsNoTracking().AnyAsync(x => x.DocumentNumber == failedRecipientDocument));
        Assert.True(await context.Customers.AsNoTracking().AnyAsync(x => x.DocumentNumber == successfulRecipientDocument));
        Assert.False(await context.ThirdPartyAccounts.AsNoTracking().AnyAsync(x => x.AccountNumberNormalized == failedDestinationAccount));
        Assert.True(await context.ThirdPartyAccounts.AsNoTracking().AnyAsync(x => x.AccountNumberNormalized == successfulDestinationAccount));

        var source = await context.Customers
            .AsNoTracking()
            .Include(x => x.Accounts)
            .SingleAsync(x => x.DocumentNumber == "900123456");
        Assert.DoesNotContain(source.Accounts, x => x.AccountNumber == failedSourceAccount);
        Assert.Contains(source.Accounts, x => x.AccountNumber == successfulSourceAccount);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task RegisterBulkAsync_WhenCancellationOccursInLaterChunk_PreservesCommittedPriorChunkAndRollsBackCurrentChunk()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var customerRepository = new AchCustomerRepository(context);
        using var cts = new CancellationTokenSource();
        var onboardingCalls = 0;
        var onboarding = new Mock<IThirdPartyOnboardingService>();
        onboarding
            .Setup(x => x.EnsureAccountAsync(It.IsAny<ThirdPartyOnboardingRequest>(), It.IsAny<CancellationToken>()))
            .Returns<ThirdPartyOnboardingRequest, CancellationToken>((_, token) =>
            {
                onboardingCalls++;
                if (onboardingCalls == 51)
                {
                    cts.Cancel();
                    return Task.FromCanceled<ThirdPartyAccountRef>(token);
                }

                return Task.FromResult(new ThirdPartyAccountRef(1, 1, false, false, false));
            });

        var logger = new CapturingLogger<AchBulkTransactionService>();
        using var metrics = new MetricCapture("Cfa.ACHInterbank.ACH.Bulk");
        var service = CreateService(
            context,
            companyEntryDescriptionId,
            thirdPartyOnboardingServiceOverride: onboarding.Object,
            customerRepositoryInstance: customerRepository,
            loggerOverride: logger);
        var request = BuildRequest("TPA9A2-CHUNK-CANCEL", 51, companyEntryDescriptionId);
        var rolledBackRecipientDocument = request.Transactions[50].RecipientIdNumber!;
        var rolledBackSourceAccount = request.Transactions[50].SourceAccountNumber;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RegisterBulkAsync(request, cts.Token));

        Assert.Equal(51, await context.Customers.AsNoTracking().CountAsync());
        Assert.False(await context.Customers.AsNoTracking().AnyAsync(x => x.DocumentNumber == rolledBackRecipientDocument));
        var source = await context.Customers
            .AsNoTracking()
            .Include(x => x.Accounts)
            .SingleAsync(x => x.DocumentNumber == "900123456");
        Assert.Equal(50, source.Accounts.Count);
        Assert.DoesNotContain(source.Accounts, x => x.AccountNumber == rolledBackSourceAccount);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.True(metrics.Contains("ach_bulk_operations_total", "outcome", "canceled"));
        Assert.True(metrics.Contains("ach_bulk_rollbacks_total", "reason", "canceled"));
        Assert.Contains(logger.Messages, message => message.Contains("outcome canceled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RegisterBulkAsync_WhenUnitOfWorkCommitFails_RollsBackChunkAndClearsTracker()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var customerRepository = new AchCustomerRepository(context);
        var onboarding = new ThirdPartyOnboardingService(
            context,
            Mock.Of<ILogger<ThirdPartyOnboardingService>>());
        const string sensitiveFailureMarker = "CTA-12345000001-DOC-900123456";
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Fallo controlado de unidad de trabajo. {sensitiveFailureMarker}"));
        var logger = new CapturingLogger<AchBulkTransactionService>();
        using var metrics = new MetricCapture("Cfa.ACHInterbank.ACH.Bulk");

        var service = CreateService(
            context,
            companyEntryDescriptionId,
            thirdPartyOnboardingServiceOverride: onboarding,
            customerRepositoryInstance: customerRepository,
            unitOfWorkOverride: unitOfWork.Object,
            loggerOverride: logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterBulkAsync(BuildRequest("TPA9A2-UOW-ROLLBACK", 1, companyEntryDescriptionId)));

        Assert.Empty(await context.Customers.AsNoTracking().ToListAsync());
        Assert.Empty(await context.ThirdParties.AsNoTracking().ToListAsync());
        Assert.Empty(await context.ThirdPartyAccounts.AsNoTracking().ToListAsync());
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.True(metrics.Contains("ach_bulk_operations_total", "outcome", "failed"));
        Assert.True(metrics.Contains("ach_bulk_rollbacks_total", "reason", "failed"));
        Assert.Contains(logger.Messages, message => message.Contains("InvalidOperationException", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains(sensitiveFailureMarker, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RegisterBulkAsync_RecoversConcurrentLegacyCustomerUniqueConflict_ByRereadingWinner()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ach-tpa9a2-customer-{Guid.NewGuid():N}.db");
        try
        {
            var baseOptions = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            int companyEntryDescriptionId;
            await using (var seedContext = new AchDbContext(baseOptions))
            {
                await seedContext.Database.EnsureCreatedAsync();
                companyEntryDescriptionId = SeedCatalog(seedContext);
            }

            var request = BuildRequest("TPA9A2-CUSTOMER-RACE", 1, companyEntryDescriptionId);
            var interceptor = new InjectConcurrentLegacyCustomerInterceptor(databasePath, request.Transactions[0]);
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .AddInterceptors(interceptor)
                .Options;

            await using var context = new AchDbContext(options);
            var customerRepository = new AchCustomerRepository(context);
            var onboarding = new ThirdPartyOnboardingService(
                context,
                Mock.Of<ILogger<ThirdPartyOnboardingService>>());
            var logger = new CapturingLogger<AchBulkTransactionService>();
            using var metrics = new MetricCapture("Cfa.ACHInterbank.ACH.Bulk");
            var service = CreateService(
                context,
                companyEntryDescriptionId,
                thirdPartyOnboardingServiceOverride: onboarding,
                customerRepositoryInstance: customerRepository,
                loggerOverride: logger);

            var response = await service.RegisterBulkAsync(request);

            Assert.Equal(1, response.TotalSucceeded);
            Assert.True(interceptor.Triggered);
            Assert.True(metrics.Contains("ach_bulk_retries_total", "reason", "unique_conflict"));
            Assert.True(metrics.Contains("ach_bulk_rollbacks_total", "reason", "unique_conflict"));
            Assert.Contains(logger.Messages, message => message.Contains("unique_conflict", StringComparison.Ordinal));
            var recipientDocument = request.Transactions[0].RecipientIdNumber!.Trim();
            var destinationAccount = request.Transactions[0].DestinationAccountNumber.Trim();
            var recipient = await context.Customers
                .AsNoTracking()
                .Include(x => x.Accounts)
                .SingleAsync(x => x.DocumentNumber == recipientDocument);
            Assert.Single(recipient.Accounts.Where(x => x.AccountNumber == destinationAccount));
            Assert.Single(await context.ThirdParties.AsNoTracking().ToListAsync());
            Assert.Single(await context.ThirdPartyAccounts.AsNoTracking().ToListAsync());
            Assert.Empty(context.ChangeTracker.Entries());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task RegisterBulkAsync_RecoversConcurrentLegacyCustomerAccountUniqueConflict_WithoutDuplicate()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ach-tpa9a2-account-{Guid.NewGuid():N}.db");
        try
        {
            var baseOptions = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            int companyEntryDescriptionId;
            int recipientCustomerId;
            BulkAchTransactionRequest request;
            await using (var seedContext = new AchDbContext(baseOptions))
            {
                await seedContext.Database.EnsureCreatedAsync();
                companyEntryDescriptionId = SeedCatalog(seedContext);
                request = BuildRequest("TPA9A2-ACCOUNT-RACE", 1, companyEntryDescriptionId);
                var item = request.Transactions[0];
                var seededRecipient = new Customer
                {
                    PersonType = "PN",
                    DocumentType = "CC",
                    DocumentNumber = item.RecipientIdNumber!.Trim(),
                    FirstName = item.RecipientName ?? "CLIENTE",
                    LastName = "NO IDENTIFICADO"
                };
                seedContext.Customers.Add(seededRecipient);
                await seedContext.SaveChangesAsync();
                recipientCustomerId = seededRecipient.Id;
            }

            var interceptor = new InjectConcurrentLegacyCustomerAccountInterceptor(
                databasePath,
                recipientCustomerId,
                request.Transactions[0].DestinationAccountNumber.Trim());
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .AddInterceptors(interceptor)
                .Options;

            await using var context = new AchDbContext(options);
            var customerRepository = new AchCustomerRepository(context);
            var onboarding = new ThirdPartyOnboardingService(
                context,
                Mock.Of<ILogger<ThirdPartyOnboardingService>>());
            var service = CreateService(
                context,
                companyEntryDescriptionId,
                thirdPartyOnboardingServiceOverride: onboarding,
                customerRepositoryInstance: customerRepository);

            var response = await service.RegisterBulkAsync(request);

            Assert.Equal(1, response.TotalSucceeded);
            Assert.True(interceptor.Triggered);
            var destinationAccount = request.Transactions[0].DestinationAccountNumber.Trim();
            var recipient = await context.Customers
                .AsNoTracking()
                .Include(x => x.Accounts)
                .SingleAsync(x => x.Id == recipientCustomerId);
            Assert.Single(recipient.Accounts.Where(x => x.AccountNumber == destinationAccount));
            Assert.Single(await context.ThirdParties.AsNoTracking().ToListAsync());
            Assert.Single(await context.ThirdPartyAccounts.AsNoTracking().ToListAsync());
            Assert.Empty(context.ChangeTracker.Entries());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task RegisterBulkAsync_Throws_WhenBatchExceedsMaxItems()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Transactions:Bulk:MaxItems"] = "2",
            ["Transactions:Bulk:ChunkSize"] = "50"
        }).Build();

        var service = CreateService(context, companyEntryDescriptionId, configuration: configuration);
        var request = BuildRequest("BULK-LIMIT", 3, companyEntryDescriptionId);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterBulkAsync(request));
        Assert.Contains("máximo permitido", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static AchBulkTransactionService CreateService(
        AchDbContext context,
        int companyEntryDescriptionId,
        IConfiguration? configuration = null,
        Mock<ITransactionValidator>? validator = null,
        ITransactionPolicyService? policyService = null,
        Mock<IAchCustomerRepository>? customerRepositoryOverride = null,
        IPrenotificationHandler? prenotificationHandlerOverride = null,
        IThirdPartyOnboardingService? thirdPartyOnboardingServiceOverride = null,
        IClearingHouseValidationPolicyService? clearingHouseValidationPolicyServiceOverride = null,
        IThirdPartyAccountEligibilityService? thirdPartyAccountEligibilityServiceOverride = null,
        ITransactionPrerequisitePolicyService? transactionPrerequisitePolicyServiceOverride = null,
        IThirdPartyAccountValidationService? thirdPartyAccountValidationServiceOverride = null,
        IAchRegulatoryCatalogService? catalogServiceOverride = null,
        IAchCustomerRepository? customerRepositoryInstance = null,
        IUnitOfWork? unitOfWorkOverride = null,
        ILogger<AchBulkTransactionService>? loggerOverride = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var effectiveUnitOfWork = unitOfWorkOverride ?? unitOfWork.Object;

        var customerRepository = customerRepositoryInstance
            ?? customerRepositoryOverride?.Object
            ?? CreateCustomerRepositoryMock().Object;

        var txValidator = validator ?? new Mock<ITransactionValidator>();

        var batchResolver = new Mock<IBatchResolver>();
        batchResolver.Setup(r => r.ResolveAsync(It.IsAny<AchTransactionRequestData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TransactionBatchContext
            {
                Batch = new AchBatch { Id = TestBatchId, AchCycleId = TestCycleId, CompanyEntryDescription = "NOMINAS", EffectiveEntryDate = DateTime.Today },
                AchCycleId = TestCycleId,
                EffectiveEntryDate = DateTime.Today,
                OriginatingDfi = "000010070",
                ReceivingDfi = "000010010",
                CompanyName = "EMPRESA",
                CompanyIdentification = "900123456",
                CompanyEntryDescription = "NOMINAS",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                ServiceClassCode = "200",
                SourceServiceClassCode = "PPD",
                ClearingHouseId = TestClearingHouseId,
                SourceInstitutionId = TestSourceInstitutionId,
                SourceInstitutionIsDefault = true,
                DestinationInstitutionId = TestDestinationInstitutionId
            });

        var nextId = 100;
        var persister = new Mock<ITransactionPersister>();
        persister.Setup(p => p.PersistAsync(It.IsAny<AchTransactionRequestData>(), It.IsAny<TransactionBatchContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                nextId++;
                var tx = new AchTransaction { Id = nextId, StateChangedAtUtc = DateTime.UtcNow };
                return new TransactionPersistResult { Transaction = tx, Batch = new AchBatch { Id = TestBatchId } };
            });
        persister.Setup(p => p.UpdateBatchTotalsAsync(It.IsAny<AchBatch>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        persister.Setup(p => p.UpdateBatchServiceClassCodeAsync(It.IsAny<AchBatch>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var prenotification = new Mock<IPrenotificationHandler>();
        prenotification.Setup(x => x.HandleAsync(It.IsAny<AchTransactionRequestData>(), It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var effectivePrenotificationHandler = prenotificationHandlerOverride ?? prenotification.Object;
        var onboarding = new Mock<IThirdPartyOnboardingService>();
        onboarding
            .Setup(x => x.EnsureAccountAsync(It.IsAny<ThirdPartyOnboardingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountRef(1, 1, false, false, false));
        var effectiveOnboarding = thirdPartyOnboardingServiceOverride ?? onboarding.Object;

        var validationPolicy = new Mock<IClearingHouseValidationPolicyService>();
        validationPolicy
            .Setup(x => x.ResolveAsync(It.IsAny<int>(), It.IsAny<TransactionNature>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int clearingHouseId, TransactionNature nature, DateTime effectiveDate, CancellationToken _) =>
            {
                var rule = new ClearingHouseTransactionRule
                {
                    Id = 9001,
                    ClearingHouseId = clearingHouseId,
                    TransactionNature = nature,
                    TransactionType = nature == TransactionNature.Debit ? TransactionTypeEnum.Debit : TransactionTypeEnum.Credit,
                    PrenotificationMode = PrenotificationRequirementMode.Optional,
                    AppliesToMonetaryTransactions = true,
                    AppliesToNachaExport = true,
                    EffectiveFrom = effectiveDate.Date,
                    IsActive = true,
                    NormativeSource = "TEST",
                    NormativeReference = "TEST"
                };
                return new ClearingHouseValidationPolicyResolution(rule, effectiveDate.Date, nature, clearingHouseId);
            });

        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility
            .Setup(x => x.EvaluateAsync(It.IsAny<ThirdPartyAccountEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThirdPartyAccountEligibilityRequest request, CancellationToken _) =>
            {
                var rule = new ClearingHouseTransactionRule
                {
                    Id = 9001,
                    ClearingHouseId = request.ClearingHouseId,
                    TransactionNature = request.TransactionNature,
                    TransactionType = request.TransactionNature == TransactionNature.Debit ? TransactionTypeEnum.Debit : TransactionTypeEnum.Credit,
                    PrenotificationMode = PrenotificationRequirementMode.Optional,
                    AppliesToMonetaryTransactions = true,
                    AppliesToNachaExport = true,
                    EffectiveFrom = request.EffectiveDate.Date,
                    IsActive = true,
                    NormativeSource = "TEST",
                    NormativeReference = "TEST"
                };
                return new ThirdPartyAccountEligibilityResult(
                    ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification,
                    "PRENOTIFICATION_OPTIONAL",
                    "Test permissive eligibility.",
                    rule,
                    null,
                    null,
                    null);
            });

        var registrationPrerequisite = new Mock<ITransactionPrerequisitePolicyService>();
        registrationPrerequisite
            .Setup(x => x.ValidateForRegistration(It.IsAny<TransactionRegistrationPrerequisiteRequest>()))
            .Returns((TransactionRegistrationPrerequisiteRequest request) =>
                new TransactionPrerequisiteValidationResult(true, "OK", "Test prerequisite satisfied.", request.PolicyRule));

        var contrapartida = new Mock<IContrapartidaDispatchPersistenceService>();
        contrapartida.Setup(x => x.EnsurePendingDispatchAsync(It.IsAny<AchTransaction>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ContrapartidaDispatchItem());

        return new AchBulkTransactionService(
            context,
            effectiveUnitOfWork,
            customerRepository,
            txValidator.Object,
            batchResolver.Object,
            persister.Object,
            effectivePrenotificationHandler,
            effectiveOnboarding,
            clearingHouseValidationPolicyServiceOverride ?? validationPolicy.Object,
            thirdPartyAccountEligibilityServiceOverride ?? eligibility.Object,
            transactionPrerequisitePolicyServiceOverride ?? registrationPrerequisite.Object,
            configuration ?? new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Transactions:Bulk:MaxItems"] = "2000",
                ["Transactions:Bulk:ChunkSize"] = "50"
            }).Build(),
            contrapartida.Object,
            catalogService: catalogServiceOverride,
            transactionPolicyService: policyService,
            thirdPartyAccountValidationService: thirdPartyAccountValidationServiceOverride,
            logger: loggerOverride);
    }

    private static Mock<IAchCustomerRepository> CreateCustomerRepositoryMock()
    {
        var repository = new Mock<IAchCustomerRepository>();
        repository.Setup(r => r.ResolveDocumentTypeCodeAsync("NIT", It.IsAny<CancellationToken>())).ReturnsAsync("NIT");
        repository.Setup(r => r.ResolveDocumentTypeCodeAsync("CC", It.IsAny<CancellationToken>())).ReturnsAsync("CC");
        repository.Setup(r => r.ResolvePersonTypeCodeAsync("PJ", It.IsAny<CancellationToken>())).ReturnsAsync("PJ");
        repository.Setup(r => r.ResolvePersonTypeCodeAsync("PN", It.IsAny<CancellationToken>())).ReturnsAsync("PN");
        repository.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return repository;
    }

    private static BulkAchTransactionRequest BuildRequest(string prefix, int count, int companyEntryDescriptionId)
    {
        var items = Enumerable.Range(1, count).Select(i => new BulkAchTransactionItemRequest
        {
            Amount = 1000 + i,
            Reference = $"{prefix}-{i:0000}",
            Type = TransactionTypeEnum.Debit,
            AccountType = AccountTypeEnum.Checking,
            IsPrenotification = false,
            DestinationInstitutionId = TestDestinationInstitutionId,
            SourceAccountNumber = $"123450{i:00000}",
            DestinationAccountNumber = $"987650{i:00000}",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourcePersonType = "PJ",
            RecipientPersonType = "PN",
            RecipientIdNumber = $"10{i:00000000}",
            RecipientName = $"CLIENTE {i}",
            Addendas = [new AddendaDto { AddendaType = "05", Information = "PAGO" }]
        }).ToList();

        return new BulkAchTransactionRequest
        {
            BatchReference = $"{prefix}-BATCH",
            ChunkSize = 50,
            Transactions = items
        };
    }

    private static int SeedCatalog(AchDbContext context)
    {
        var companyEntryDescriptionId = context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "NOMINAS" && x.IsActive)
            .Select(x => x.Id)
            .First();

        if (!context.ClearingHouseConfigs.Any(x => x.Id == TestClearingHouseConfigId))
        {
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = TestClearingHouseConfigId,
                HolidayStrategy = "Colombian",
                TimeZoneId = "America/Bogota"
            });
        }

        if (!context.ClearingHouses.Any(x => x.Id == TestClearingHouseId))
        {
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = TestClearingHouseId,
                Name = "ACH Colombia",
                Code = "ACHCOL",
                OriginCode = "12345678",
                ClearingHouseId = TestClearingHouseConfigId
            });
        }

        if (!context.AchCycles.Any(x => x.Id == TestCycleId))
        {
            var cycleConfig = new ClearingHouseCycleConfig
            {
                ClearingHouseId = TestClearingHouseId,
                PolicyVersion = "TEST-V1",
                CycleName = "Ciclo 1",
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(23, 59, 0),
                OutputReleaseTime = new TimeSpan(23, 59, 0),
                EffectiveFrom = DateTime.Today,
                EffectiveTo = DateTime.Today,
                IsActive = true
            };
            context.ClearingHouseCycleConfigs.Add(cycleConfig);
            context.AchCycles.Add(new AchCycle
            {
                Id = TestCycleId,
                CycleName = "Ciclo 1",
                ProcessingDate = DateTime.Today,
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(23, 59, 0),
                OutputReleaseTime = new TimeSpan(23, 59, 0),
                ClearingHouseId = TestClearingHouseId,
                ClearingHouseCycleConfig = cycleConfig
            });
        }

        if (!context.AchBatches.Any(x => x.Id == TestBatchId))
        {
            context.AchBatches.Add(new AchBatch
            {
                Id = TestBatchId,
                AchCycleId = TestCycleId,
                CompanyName = "EMPRESA",
                CompanyIdentification = "900123456",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                CompanyEntryDescription = "NOMINAS",
                OriginOrOdfi = "00001007",
                EffectiveEntryDate = DateTime.Today,
                BatchSequenceNumber = 1
            });
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == TestSourceInstitutionId))
        {
            context.FinancialInstitutions.Add(new FinancialInstitution { Id = TestSourceInstitutionId, Name = "Origen", RoutingNumber = "00001", TransitCode = "007", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active });
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == TestDestinationInstitutionId))
        {
            context.FinancialInstitutions.Add(new FinancialInstitution { Id = TestDestinationInstitutionId, Name = "Destino", RoutingNumber = "00001", TransitCode = "001", Status = FinancialInstitutionStatus.Active });
        }

        foreach (var institution in context.FinancialInstitutions.Local)
        {
            institution.CalculateCheckDigit();
        }

        context.SaveChanges();
        return companyEntryDescriptionId;
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class MetricCapture : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly string _meterName;

        public MetricCapture(string meterName)
        {
            _meterName = meterName;
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == _meterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                var capturedTags = new List<KeyValuePair<string, object?>>(tags.Length);
                foreach (var tag in tags)
                {
                    capturedTags.Add(tag);
                }

                Measurements.Add(new MetricMeasurement(instrument.Name, measurement, capturedTags));
            });
            _listener.Start();
        }

        private List<MetricMeasurement> Measurements { get; } = [];

        public bool Contains(string instrumentName, string tagName, string tagValue)
            => Measurements.Any(measurement =>
                measurement.Name == instrumentName
                && measurement.Tags.Any(tag =>
                    tag.Key == tagName
                    && string.Equals(tag.Value?.ToString(), tagValue, StringComparison.Ordinal)));

        public int Count(string instrumentName, string tagName, string tagValue)
            => Measurements.Count(measurement =>
                measurement.Name == instrumentName
                && measurement.Tags.Any(tag =>
                    tag.Key == tagName
                    && string.Equals(tag.Value?.ToString(), tagValue, StringComparison.Ordinal)));

        public void Dispose() => _listener.Dispose();

        private sealed record MetricMeasurement(
            string Name,
            long Value,
            IReadOnlyList<KeyValuePair<string, object?>> Tags);
    }

    private sealed class InjectConcurrentLegacyCustomerInterceptor : DbTransactionInterceptor
    {
        private readonly string _databasePath;
        private readonly BulkAchTransactionItemRequest _item;

        public InjectConcurrentLegacyCustomerInterceptor(string databasePath, BulkAchTransactionItemRequest item)
        {
            _databasePath = databasePath;
            _item = item;
        }

        public bool Triggered { get; private set; }

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            if (Triggered || eventData.Context is not AchDbContext)
            {
                return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
            }

            // BuildCustomerCacheAsync has already completed when the chunk transaction starts.
            // Persist the concurrent winner immediately before the transaction begins so the
            // caller's cache is intentionally stale and its first INSERT loses the unique race.
            Triggered = true;
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;
            await using var competitor = new AchDbContext(options);
            var winner = new Customer
            {
                PersonType = "PN",
                DocumentType = "CC",
                DocumentNumber = _item.RecipientIdNumber!.Trim(),
                FirstName = _item.RecipientName ?? "CLIENTE",
                LastName = "NO IDENTIFICADO"
            };
            winner.Accounts.Add(new CustomerAccount { AccountNumber = _item.DestinationAccountNumber.Trim() });
            competitor.Customers.Add(winner);
            await competitor.SaveChangesAsync(cancellationToken);

            return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }
    }

    private sealed class InjectConcurrentLegacyCustomerAccountInterceptor : DbTransactionInterceptor
    {
        private readonly string _databasePath;
        private readonly int _customerId;
        private readonly string _accountNumber;

        public InjectConcurrentLegacyCustomerAccountInterceptor(string databasePath, int customerId, string accountNumber)
        {
            _databasePath = databasePath;
            _customerId = customerId;
            _accountNumber = accountNumber;
        }

        public bool Triggered { get; private set; }

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            if (Triggered || eventData.Context is not AchDbContext)
            {
                return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
            }

            Triggered = true;
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;
            await using var competitor = new AchDbContext(options);
            var winner = await competitor.Customers
                .Include(x => x.Accounts)
                .SingleAsync(x => x.Id == _customerId, cancellationToken);
            winner.Accounts.Add(new CustomerAccount { AccountNumber = _accountNumber });
            await competitor.SaveChangesAsync(cancellationToken);

            return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }
    }
}
