using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Cfa.ACHInterbank.Tests.TestSupport;

namespace Cfa.ACHInterbank.Tests;

public class AchTransactionNachaTests
{
    [Fact]
    public async Task RegisterTransactionAsync_CreatesTransactionAndBatch()
    {
        using var connection = CreateOpenConnection();

        AchTransaction tx = null!;
        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);

            string cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
            var service = BuildTransactionService(arrangeContext, cycleId);

            tx = await service.RegisterTransactionAsync(
                amount: 1500m,
                reference: "PAGO-REF-001",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                recipientIdNumber: "900123456",
                recipientName: "Cliente sintético",
                requiresIdentityValidation: false,
                addendas:
                [
                    new() { AddendaType = "05", BusinessType = AchAddendaBusinessType.Debit, CollectorId = "9001234567", ReceiverCustomerCode = "CLI0000000001", ServiceDescription = "FACTURA" },
                    new() { AddendaType = "99", BusinessType = AchAddendaBusinessType.Return, ReturnReasonCode = "R01", OriginalTraceNumber = "123456780000001", NewTraceNumber = "765432100000001" }
                ],
                ct: CancellationToken.None);

            Assert.NotEqual(0, tx.Id);
            Assert.Equal("PAGO-REF-001", tx.Reference);
            Assert.StartsWith("12345678", tx.TraceNumber);
            Assert.Equal("123456780", tx.CompanyIdentification);
            Assert.Equal("12345678", tx.OriginatingDFI);
            Assert.Equal("76543210", tx.ReceivingDFI);
        }

        using var verification = CreateContext(connection);

        var savedTransaction = await verification.AchTransactions
            .Include(t => t.AchBatch)
            .Include(t => t.Addendas)
            .SingleAsync();

        Assert.Equal(tx.Id, savedTransaction.Id);
        Assert.Equal("225", savedTransaction.AchBatch.ServiceClassCode);
        Assert.True(savedTransaction.AddendaRecordIndicator);
        Assert.Equal(2, savedTransaction.Addendas.Count);
        Assert.Collection(
            savedTransaction.Addendas.OrderBy(a => a.SequenceNumber),
            first =>
            {
                Assert.Equal("05", first.AddendaType);
                Assert.Equal(AchAddendaBusinessType.Debit, first.BusinessType);
                Assert.Equal("9001234567", first.CollectorId);
                Assert.Equal("CLI0000000001", first.ReceiverCustomerCode);
                Assert.Equal("FACTURA", first.ServiceDescription);
                Assert.Equal(1, first.SequenceNumber);
            },
            second =>
            {
                Assert.Equal("99", second.AddendaType);
                Assert.Equal(AchAddendaBusinessType.Return, second.BusinessType);
                Assert.Equal("R01", second.ReturnReasonCode);
                Assert.Equal("123456780000001", second.OriginalTraceNumber);
                Assert.Equal("765432100000001", second.NewTraceNumber);
                Assert.Equal(2, second.SequenceNumber);
            });
        Assert.Equal(1, savedTransaction.SourceInstitutionId);

        var batch = await verification.AchBatches.Include(b => b.Transactions).SingleAsync();
        Assert.Single(batch.Transactions);
        Assert.Equal(tx.Id, batch.Transactions.Single().Id);
    }

    [Fact]
    public async Task RegisterTransactionAsync_CreatesInitialStateEventAndTraceabilityIncludesIt()
    {
        using var connection = CreateOpenConnection();

        AchTransaction tx;
        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);

            string cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
            var service = BuildTransactionService(arrangeContext, cycleId);

            tx = await service.RegisterTransactionAsync(
                amount: 1500m,
                reference: "PAGO-REF-EVENT-001",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                transactionExternalId: "TX-EVENT-001",
                recipientIdNumber: "900123456",
                recipientName: "Cliente sintético",
                requiresIdentityValidation: false,
                addendas: null,
                ct: CancellationToken.None);
        }

        using var verification = CreateContext(connection);

        var stateEvent = await verification.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == tx.Id);
        Assert.Equal(AchTransferStateEnum.Pending, stateEvent.FromState);
        Assert.Equal(AchTransferStateEnum.Pending, stateEvent.ToState);
        Assert.Equal(AchStateEventSourceEnum.System, stateEvent.Source);
        Assert.Equal("CREATED", stateEvent.ReasonCode);
        Assert.Contains("TransactionCreated", stateEvent.PayloadJson);
        Assert.Contains("TX-EVENT-001", stateEvent.PayloadJson);

        var traceability = new AchTraceabilityService(verification, new AchStateTransitionService(verification));
        var detail = await traceability.GetTransactionTraceabilityAsync(tx.Id);

        Assert.NotNull(detail);
        var traceEvent = Assert.Single(detail.Events);
        Assert.Equal(stateEvent.Id, traceEvent.Id);
        Assert.Equal(AchTransferStateEnum.Pending, traceEvent.FromState);
        Assert.Equal(AchTransferStateEnum.Pending, traceEvent.ToState);
        Assert.Equal(AchStateEventSourceEnum.System, traceEvent.Source);
    }

    [Fact]
    public async Task RegisterTransactionAsync_WhenPolicyRejectsDuplicate_DoesNotCreateSecondInitialStateEvent()
    {
        using var connection = CreateOpenConnection();

        using var context = CreateContext(connection);
        SeedCoreEntities(context);

        string cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var policyService = new Mock<ITransactionPolicyService>();
        policyService
            .SetupSequence(x => x.PreviewAsync(It.IsAny<TransactionPolicyPreviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPolicyPreview(true, null, cycleId, "CICLO-TEST", TestClock.OperationalDate, "ACH Colombia", 1, "", true, null, null, null, $"{cycleId}:Debit:111122223333:999988887777:1500:TX-DUP-001", false))
            .ReturnsAsync(new TransactionPolicyPreview(false, "Ya existe una transacción equivalente para el mismo ciclo.", cycleId, "CICLO-TEST", TestClock.OperationalDate, "ACH Colombia", 1, "", true, null, null, null, $"{cycleId}:Debit:111122223333:999988887777:1500:TX-DUP-001", true));

        var service = BuildTransactionService(context, cycleId, policyService.Object);

        await service.RegisterTransactionAsync(
            amount: 1500m,
            reference: "PAGO-REF-DUP-001",
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.Checking,
            isPrenotification: false,
            destinationInstitutionId: 2,
            sourceAccountNumber: "111122223333",
            destinationAccountNumber: "999988887777",
            companyName: "Empresa Demo",
            companyIdentification: "123456780",
            companyEntryDescriptionId: GetCompanyEntryDescriptionId(context, "PAGOS PSE"),
            transactionExternalId: "TX-DUP-001",
            recipientIdNumber: "900123456",
            recipientName: "Cliente sintético",
            requiresIdentityValidation: false,
            addendas: null,
            ct: CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterTransactionAsync(
            amount: 1500m,
            reference: "PAGO-REF-DUP-001",
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.Checking,
            isPrenotification: false,
            destinationInstitutionId: 2,
            sourceAccountNumber: "111122223333",
            destinationAccountNumber: "999988887777",
            companyName: "Empresa Demo",
            companyIdentification: "123456780",
            companyEntryDescriptionId: GetCompanyEntryDescriptionId(context, "PAGOS PSE"),
            transactionExternalId: "TX-DUP-001",
            recipientIdNumber: "900123456",
            recipientName: "Cliente sintético",
            requiresIdentityValidation: false,
            addendas: null,
            ct: CancellationToken.None));

        Assert.Contains("equivalente", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await context.AchTransactions.CountAsync());
        Assert.Equal(1, await context.AchTransactionStateEvents.CountAsync());
        policyService.Verify(x => x.PreviewAsync(It.IsAny<TransactionPolicyPreviewRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RegisterTransactionAsync_WithoutDefaultSource_Throws()
    {
        using var connection = CreateOpenConnection();

        using var arrangeContext = CreateContext(connection);
        SeedCoreEntities(arrangeContext);

        var defaultSource = await arrangeContext.FinancialInstitutions
            .SingleAsync(fi => fi.IsDefaultSource);
        defaultSource.IsDefaultSource = false;
        arrangeContext.FinancialInstitutions.Update(defaultSource);
        await arrangeContext.SaveChangesAsync();

        string cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var service = BuildTransactionService(arrangeContext, cycleId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterTransactionAsync(
            amount: 1500m,
            reference: "PAGO-REF-003",
            type: TransactionTypeEnum.Credit,
            accountType: AccountTypeEnum.Checking,
            isPrenotification: false,
            destinationInstitutionId: 2,
            sourceAccountNumber: "111122223333",
            destinationAccountNumber: "999988887777",
            companyName: "Empresa Demo",
            companyIdentification: "123456780",
            companyEntryDescriptionId: 1,
            recipientIdNumber: null,
            requiresIdentityValidation: false,
            addendas: null,
            ct: CancellationToken.None));
    }

    [Fact]
    public async Task BuildNachaFileByCycleAsync_GeneratesSequentialRecords()
    {
        using var connection = CreateOpenConnection();

        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var transactionService = BuildTransactionService(arrangeContext, cycleId);

            await transactionService.RegisterTransactionAsync(
                amount: 0m,
                reference: "PAGOPRE-002",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: true,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: "900123456",
                recipientName: "Cliente sintético",
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            await transactionService.RegisterTransactionAsync(
                amount: 1500m,
                reference: "PAGO-REF-002",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: "900123456",
                recipientName: "Cliente sintético",
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            var returned = await transactionService.RegisterTransactionAsync(
                amount: 999m,
                reference: "PAGO-RETURNED-OMIT",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: "900123456",
                recipientName: "Cliente sintético",
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            returned.State = AchTransferStateEnum.ReturnedByOperator;
            arrangeContext.AchTransactions.Update(returned);

            var prenote = await arrangeContext.AchTransactions
                .SingleAsync(t => t.Reference == "PAGOPRE-002");
            prenote.EffectiveEntryDate = TestClock.OperationalDate.AddDays(-5);
            arrangeContext.AchTransactions.Update(prenote);
            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = CreateContext(connection);
        var holidayService = new Mock<IBankHoliday>();
        holidayService
            .Setup(h => h.GetHolidays(It.IsAny<int>()))
            .Returns([]);
        var loader = new NachaDataLoader(executionContext);
        var validation = new NachaTransactionValidationService(executionContext, holidayService.Object, CreatePermissivePrerequisitePolicy());
        var renderer = new NachaFixedWidthRecordRenderer();
        var recordDataProvider = new NachaRecordDataProvider(executionContext);
        var semanticValidator = new NachaSemanticValidator();
        var builder = new NachaFileBuilder(executionContext, holidayService.Object, loader, validation, renderer, recordDataProvider, semanticValidator,
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "LEGACY", ExecutionScope = "DEVELOPMENT" }));
        var nachaContent = await builder.BuildNachaFileByCycleAsync(cycleId, CancellationToken.None);

        var records = ChunkRecords(nachaContent);

        Assert.Equal(10, records.Count);
        Assert.All(records, record => Assert.Equal(106, record.Length));
        Assert.Equal("1", records[0][..1]);
        Assert.Equal("5", records[1][..1]);
        Assert.Equal("6", records[2][..1]);
        Assert.Equal("6", records[3][..1]);
        Assert.Equal("7", records[4][..1]);
        Assert.Equal("7", records[5][..1]);
        Assert.Equal("8", records[6][..1]);
        Assert.Equal("9", records[7][..1]);
        Assert.Equal("9", records[8][..1]);
        Assert.Equal("9", records[9][..1]);

        Assert.Equal("05", records[4].Substring(1, 2));
        Assert.Equal("0009001234567", records[4].Substring(3, 13));
        Assert.Equal("0000001", records[4].Substring(87, 7));
    }

    [Fact]
    public async Task RegisterTransactionAsync_Prenotification_DoesNotEnqueueContrapartidaDispatch()
    {
        using var connection = CreateOpenConnection();

        using var arrangeContext = CreateContext(connection);
        SeedCoreEntities(arrangeContext);
        SeedNachaLayouts(arrangeContext);

        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var (service, contrapartida) = BuildTransactionServiceWithDispatchMock(arrangeContext, cycleId);

        var transaction = await service.RegisterTransactionAsync(
            amount: 0m,
            reference: "PAGOPRE-UAT-001",
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.Checking,
            isPrenotification: true,
            destinationInstitutionId: 2,
            sourceAccountNumber: "111122223333",
            destinationAccountNumber: "999988887777",
            companyName: "Empresa Demo",
            companyIdentification: "123456780",
            companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "RECAUDOS"),
            recipientIdNumber: "900123456",
            recipientName: "Cliente Recaudo",
            requiresIdentityValidation: false,
            addendas:
            [
                new()
                {
                    AddendaType = "05",
                    BusinessType = AchAddendaBusinessType.Debit,
                    CollectorId = "9001234567",
                    ReceiverCustomerCode = "CLI0000000001",
                    ServiceDescription = "FACTURA"
                }
            ],
            ct: CancellationToken.None);

        contrapartida.Verify(
            x => x.EnsurePendingDispatchAsync(It.IsAny<AchTransaction>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        Assert.True(transaction.IsPrenotification);
        Assert.Equal(TransactionTypeEnum.Prenotification, transaction.Type);
    }

    [Fact]
    public async Task RegisterTransactionAsync_Prenotification_OnboardsParticipantsIdempotently()
    {
        using var connection = CreateOpenConnection();

        using (var context = CreateContext(connection))
        {
            SeedCoreEntities(context);
            var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
            var (service, contrapartida) = BuildTransactionServiceWithDispatchMock(context, cycleId);
            var descriptionId = GetCompanyEntryDescriptionId(context, "RECAUDOS");

            foreach (var sequence in new[] { "001", "002" })
            {
                await service.RegisterTransactionAsync(
                    amount: 0m,
                    reference: $"PRE-ONBOARD-{sequence}",
                    type: TransactionTypeEnum.Debit,
                    accountType: AccountTypeEnum.Checking,
                    isPrenotification: true,
                    destinationInstitutionId: 2,
                    sourceAccountNumber: "700000000001",
                    destinationAccountNumber: "800000000001",
                    companyName: "ORIGEN SINTETICO",
                    companyIdentification: "900777001",
                    companyEntryDescriptionId: descriptionId,
                    sourcePersonType: "PJ",
                    recipientPersonType: "PN",
                    recipientIdNumber: "1030555001",
                    recipientName: "RECEPTOR SINTETICO",
                    transactionExternalId: $"TX-PRE-ONBOARD-{sequence}",
                    requiresIdentityValidation: false,
                    addendas:
                    [
                        new()
                        {
                            AddendaType = "05",
                            BusinessType = AchAddendaBusinessType.Debit,
                            CollectorId = "900777001",
                            ReceiverCustomerCode = "CLIENTE-SINTETICO",
                            ServiceDescription = "PRENOTIFICACION"
                        }
                    ],
                    ct: CancellationToken.None);
            }

            contrapartida.Verify(
                dispatch => dispatch.EnsurePendingDispatchAsync(
                    It.IsAny<AchTransaction>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        using var verification = CreateContext(connection);
        var originator = await verification.Customers
            .Include(customer => customer.Accounts)
            .SingleAsync(customer => customer.DocumentType == "NIT" && customer.DocumentNumber == "900777001");
        var recipient = await verification.Customers
            .Include(customer => customer.Accounts)
            .SingleAsync(customer => customer.DocumentType == "CC" && customer.DocumentNumber == "1030555001");
        var thirdParty = await verification.CustomerThirdParties.SingleAsync(thirdParty =>
            thirdParty.CustomerId == originator.Id
            && thirdParty.DestinationInstitutionId == 2
            && thirdParty.DestinationAccountNumber == "800000000001"
            && thirdParty.RecipientIdNumber == "1030555001");

        Assert.Single(originator.Accounts, account => account.AccountNumber == "700000000001");
        Assert.Single(recipient.Accounts, account => account.AccountNumber == "800000000001");
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, thirdParty.Status);
        Assert.Equal(2, await verification.AchTransactions.CountAsync(transaction =>
            transaction.TransactionExternalId.StartsWith("TX-PRE-ONBOARD-")));
        Assert.Equal(1, await verification.CustomerThirdParties.CountAsync(candidate =>
            candidate.CustomerId == originator.Id
            && candidate.DestinationInstitutionId == 2
            && candidate.DestinationAccountNumber == "800000000001"
            && candidate.RecipientIdNumber == "1030555001"));
    }

    [Fact]
    public async Task RegisterTransactionAsync_WhenDispatchPersistenceFails_RollsBackSilentOnboarding()
    {
        using var connection = CreateOpenConnection();

        using (var context = CreateContext(connection))
        {
            SeedCoreEntities(context);
            var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
            var (service, contrapartida) = BuildTransactionServiceWithDispatchMock(context, cycleId);
            contrapartida
                .Setup(dispatch => dispatch.EnsurePendingDispatchAsync(
                    It.IsAny<AchTransaction>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Fallo controlado de persistencia del dispatch."));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterTransactionAsync(
                amount: 1m,
                reference: "FAIL-ONBOARD-001",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "700000000002",
                destinationAccountNumber: "800000000002",
                companyName: "ORIGEN FALLIDO",
                companyIdentification: "900777002",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(context, "RECAUDOS"),
                sourcePersonType: "PJ",
                recipientPersonType: "PN",
                recipientIdNumber: "1030555002",
                recipientName: "RECEPTOR FALLIDO",
                transactionExternalId: "TX-FAIL-ONBOARD-001",
                requiresIdentityValidation: false,
                addendas:
                [
                    new()
                    {
                        AddendaType = "05",
                        BusinessType = AchAddendaBusinessType.Debit,
                        CollectorId = "900777002",
                        ReceiverCustomerCode = "CLIENTE-FALLIDO",
                        ServiceDescription = "FALLO CONTROL"
                    }
                ],
                ct: CancellationToken.None));
        }

        using var verification = CreateContext(connection);
        Assert.Equal(0, await verification.Customers.CountAsync(customer =>
            customer.DocumentNumber == "900777002" || customer.DocumentNumber == "1030555002"));
        Assert.Equal(0, await verification.CustomerAccounts.CountAsync(account =>
            account.AccountNumber == "700000000002" || account.AccountNumber == "800000000002"));
        Assert.Equal(0, await verification.CustomerThirdParties.CountAsync(thirdParty =>
            thirdParty.DestinationAccountNumber == "800000000002"
            || thirdParty.RecipientIdNumber == "1030555002"));
        Assert.Equal(0, await verification.AchTransactions.CountAsync(transaction =>
            transaction.TransactionExternalId == "TX-FAIL-ONBOARD-001"));
        Assert.Equal(0, await verification.ContrapartidaDispatchItems.CountAsync(item =>
            item.AchTransaction.TransactionExternalId == "TX-FAIL-ONBOARD-001"));
    }

    [Fact]
    public async Task BuildNachaFileByCycleAsync_Throws_WhenAddendaBusinessTypeIsIncompatibleWithTransactionType()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var cycle = await arrangeContext.AchCycles.AsNoTracking().SingleAsync(c => c.Id == cycleId);
            var batch = new AchBatch
            {
                AchCycleId = cycleId,
                EffectiveEntryDate = cycle.ProcessingDate,
                BatchSequenceNumber = 1,
                ServiceClassCode = "220",
                CompanyName = "EMPRESA DEMO",
                CompanyIdentification = "123456780",
                CompanyEntryDescription = "PAGOS PSE",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                OriginOrOdfi = "12345678"
            };

            var tx = new AchTransaction
            {
                Amount = 0m,
                TransactionExternalId = "TX-OP-001",
                Reference = "LEG-REF-001",
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "22",
                ServiceClassCode = "220",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                CompanyName = "EMPRESA DEMO",
                CompanyIdentification = "123456780",
                OriginatingDFI = "12345678",
                ReceivingDFI = "76543210",
                TraceNumber = "123456780000001",
                TraceSequenceNumber = 1,
                EffectiveEntryDate = cycle.ProcessingDate,
                AddendaRecordIndicator = true,
                SourceAccountNumber = "111122223333",
                DestinationAccountNumber = "999988887777",
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2,
                AchCycleId = cycleId,
                AchBatch = batch,
                IsPrenotification = true,
                Addendas =
                [
                    new AchTransactionAddenda
                    {
                        AddendaType = "05",
                        BusinessType = AchAddendaBusinessType.Return,
                        ReturnReasonCode = "R01",
                        OriginalTraceNumber = "123456780000001",
                        NewTraceNumber = "765432100000001",
                        SequenceNumber = 1
                    }
                ]
            };

            arrangeContext.AchBatches.Add(batch);
            arrangeContext.AchTransactions.Add(tx);
            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = CreateContext(connection);
        var holidayService = new Mock<IBankHoliday>();
        holidayService.Setup(h => h.GetHolidays(It.IsAny<int>())).Returns([]);
        var loader = new NachaDataLoader(executionContext);
        var validation = new NachaTransactionValidationService(executionContext, holidayService.Object, CreatePermissivePrerequisitePolicy());
        var renderer = new NachaFixedWidthRecordRenderer();
        var recordDataProvider = new NachaRecordDataProvider(executionContext);
        var semanticValidator = new NachaSemanticValidator();
        var builder = new NachaFileBuilder(executionContext, holidayService.Object, loader, validation, renderer, recordDataProvider, semanticValidator,
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "LEGACY", ExecutionScope = "DEVELOPMENT" }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildNachaFileByCycleAsync(cycleId, CancellationToken.None));
        Assert.Contains("devolución", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterTransactions_WithSavingsCheckingAndPrenote_BuildsNachaFile()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var transactionService = BuildTransactionService(arrangeContext, cycleId);

            await transactionService.RegisterTransactionAsync(
                amount: 0m,
                reference: "PAGOPRE-SAV-001",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Savings,
                isPrenotification: true,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "222233334444",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: null,
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            await transactionService.RegisterTransactionAsync(
                amount: 125m,
                reference: "PAGO-SAV-001",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Savings,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "222233334444",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: "900123456",
                recipientName: "Cliente sintético",
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            await transactionService.RegisterTransactionAsync(
                amount: 0m,
                reference: "PAGOPRE-CHK-001",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: true,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "555566667777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: null,
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            await transactionService.RegisterTransactionAsync(
                amount: 350m,
                reference: "PAGO-CHK-001",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "555566667777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: "900123456",
                recipientName: "Cliente sintético",
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            await transactionService.RegisterTransactionAsync(
                amount: 0m,
                reference: "PAGOPRE-003",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: true,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "888899990000",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "MULTICREDIT"),
                recipientIdNumber: null,
                requiresIdentityValidation: false,
                addendas: BuildDebitAddendas(),
                ct: CancellationToken.None);

            var prenotes = await arrangeContext.AchTransactions
                .Where(t => t.IsPrenotification)
                .ToListAsync();
            foreach (var prenote in prenotes)
            {
                prenote.EffectiveEntryDate = TestClock.OperationalDate.AddDays(-5);
            }

            arrangeContext.AchTransactions.UpdateRange(prenotes);
            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = CreateContext(connection);
        var holidayService = new Mock<IBankHoliday>();
        holidayService
            .Setup(h => h.GetHolidays(It.IsAny<int>()))
            .Returns([]);
        var loader = new NachaDataLoader(executionContext);
        var validation = new NachaTransactionValidationService(executionContext, holidayService.Object, CreatePermissivePrerequisitePolicy());
        var renderer = new NachaFixedWidthRecordRenderer();
        var recordDataProvider = new NachaRecordDataProvider(executionContext);
        var semanticValidator = new NachaSemanticValidator();
        var builder = new NachaFileBuilder(executionContext, holidayService.Object, loader, validation, renderer, recordDataProvider, semanticValidator,
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "LEGACY", ExecutionScope = "DEVELOPMENT" }));
        var nachaContent = await builder.BuildNachaFileByCycleAsync(cycleId, CancellationToken.None);

        Assert.NotEmpty(nachaContent);
        var records = ChunkRecords(nachaContent);
        Assert.Equal(5, records.Count(record => record.StartsWith("6", StringComparison.Ordinal)));
        Assert.Equal(5, records.Count(record => record.StartsWith("7", StringComparison.Ordinal)));
        Assert.Contains(records, record => record.StartsWith("5", StringComparison.Ordinal) && record.Contains("MULTICREDI", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuildNachaFileByCycleAsync_DebitAddenda_UsesGoldenPositions()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var transactionService = BuildTransactionService(arrangeContext, cycleId);
            await transactionService.RegisterTransactionAsync(
                amount: 0m,
                reference: "PRE-RECAUDO-SERVICIO",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: true,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: GetCompanyEntryDescriptionId(arrangeContext, "RECAUDOS"),
                recipientIdNumber: "900123456",
                recipientName: "Cliente Recaudo",
                requiresIdentityValidation: false,
                addendas:
                [
                    new()
                    {
                        AddendaType = "05",
                        BusinessType = AchAddendaBusinessType.Debit,
                        CollectorId = "9001234567",
                        ReceiverCustomerCode = "CLI0000000001",
                        ServiceDescription = "FACTURA"
                    }
                ],
                ct: CancellationToken.None);

            await transactionService.RegisterTransactionAsync(
                amount: 2500m,
                reference: "RECAUDO-SERVICIO",
                type: TransactionTypeEnum.Debit,
                accountType: AccountTypeEnum.Checking,
                isPrenotification: false,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                companyName: "Empresa Demo",
                companyIdentification: "123456780",
                companyEntryDescriptionId: 2,
                recipientIdNumber: "900123456",
                recipientName: "Cliente Recaudo",
                requiresIdentityValidation: false,
                addendas:
                [
                    new()
                    {
                        AddendaType = "05",
                        BusinessType = AchAddendaBusinessType.Debit,
                        CollectorId = "9001234567",
                        ReceiverCustomerCode = "CLI0000000001",
                        ServiceDescription = "FACTURA"
                    }
                ],
                ct: CancellationToken.None);

            var prenote = await arrangeContext.AchTransactions
                .SingleAsync(t => t.Reference == "PRE-RECAUDO-SERVICIO");
            Assert.Equal("28", prenote.TransactionCode);
                prenote.EffectiveEntryDate = TestClock.OperationalDate.AddDays(-5);
            arrangeContext.AchTransactions.Update(prenote);
            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = CreateContext(connection);
        var holidayService = new Mock<IBankHoliday>();
        holidayService.Setup(h => h.GetHolidays(It.IsAny<int>())).Returns([]);
        var loader = new NachaDataLoader(executionContext);
        var validation = new NachaTransactionValidationService(executionContext, holidayService.Object, CreatePermissivePrerequisitePolicy());
        var renderer = new NachaFixedWidthRecordRenderer();
        var recordDataProvider = new NachaRecordDataProvider(executionContext);
        var semanticValidator = new Mock<INachaSemanticValidator>();
        var builder = new NachaFileBuilder(executionContext, holidayService.Object, loader, validation, renderer, recordDataProvider, semanticValidator.Object,
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "LEGACY", ExecutionScope = "DEVELOPMENT" }));
        var records = ChunkRecords(await builder.BuildNachaFileByCycleAsync(cycleId, CancellationToken.None));
        var addendaRecord = records.Last(record => record.StartsWith("7"));

        Assert.Equal("05", addendaRecord.Substring(1, 2));
        Assert.Equal("0009001234567", addendaRecord.Substring(3, 13));
        Assert.Equal("CLI0000000001                 ", addendaRecord.Substring(16, 30));
        Assert.Equal("FACTURA        ", addendaRecord.Substring(46, 15));
        Assert.Equal(7, addendaRecord.Substring(87, 7).Length);
        Assert.True(addendaRecord.Substring(87, 7).All(char.IsDigit));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ReturnAddenda_DelegatesV35SemanticsToOptionC()
    {
        using var connection = CreateOpenConnection();

        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var cycle = await arrangeContext.AchCycles.SingleAsync(c => c.Id == cycleId);
            var batch = new AchBatch
            {
                AchCycleId = cycleId,
                EffectiveEntryDate = cycle.ProcessingDate,
                BatchSequenceNumber = 1,
                ServiceClassCode = "220",
                CompanyName = "Empresa Demo",
                CompanyIdentification = "123456780",
                CompanyEntryDescription = "PAGOS PSE",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                OriginOrOdfi = "12345678"
            };

            var transaction = new AchTransaction
            {
                Amount = 1200m,
                Reference = "PAGO RET",
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "22",
                OriginatingDFI = "12345678",
                ReceivingDFI = "76543210",
                TraceNumber = "123456780000123",
                TraceSequenceNumber = 123,
                EffectiveEntryDate = cycle.ProcessingDate,
                AddendaRecordIndicator = true,
                IsPrenotification = false,
                CompanyName = "Empresa Demo",
                CompanyIdentification = "123456780",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                SourceAccountNumber = "111122223333",
                DestinationAccountNumber = "999988887777",
                AchCycleId = cycle.Id,
                AchBatch = batch,
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2
            };

            arrangeContext.AchTransactions.Add(transaction);
            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = CreateContext(connection);
        var persistedTransactionId = await executionContext.AchTransactions.Select(t => t.Id).SingleAsync();
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchReturnEligibilityRequest req, CancellationToken _) => new AchReturnEligibilityResult(true, req.ReturnReasonCode.Trim().ToUpperInvariant(), 1, "Credit", "Pending", []));
        var builder = ReturnOutNachaFileBuilderFactory.Create();
        var service = new AchReturnsService(executionContext, regulatoryCatalogService: new AchRegulatoryCatalogService(executionContext), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: builder);
        var response = await service.GenerateReturnsFileAsync(
            new GenerateReturnsFileRequest(
                cycleId,
                [new ReturnSelectionItemDto(persistedTransactionId, "R01")]),
            CancellationToken.None);

        var requests = Mock.Get(builder).Invocations
            .Where(x => x.Method.Name == nameof(INachaFileBuilder.BuildReturnOutAsync))
            .Select(x => (NachaReturnOutBuildRequest)x.Arguments[0])
            .ToArray();
        var entry = requests[^1].Batches.Single().Entries.Single();

        Assert.Equal(2, requests.Length);
        Assert.Equal("21", entry.TransactionCode);
        Assert.Equal("R01", entry.ReturnReasonCode);
        Assert.Equal("123456780000123", entry.OriginalTraceNumber);
        Assert.Equal("76543210", entry.OriginalReceivingDfi);
        Assert.StartsWith("76543210", entry.NewTraceNumber, StringComparison.Ordinal);
        Assert.Equal(1200m, entry.Amount);
        var records = ChunkRecords(System.Text.Encoding.UTF8.GetString(response.Content));
        Assert.Equal(10, records.Count);
        Assert.Equal(106 * records.Count, response.Content.Length);
        Assert.DoesNotContain('\n', System.Text.Encoding.UTF8.GetString(response.Content));
        Assert.DoesNotContain('\r', System.Text.Encoding.UTF8.GetString(response.Content));
        Assert.All(records.Skip(6), record => Assert.Equal(new string('9', 106), record));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_WithFiveCharacterReasonCode_FailsClosedAtOptionCBoundary()
    {
        using var connection = CreateOpenConnection();

        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var cycle = await arrangeContext.AchCycles.SingleAsync(c => c.Id == cycleId);
            var batch = new AchBatch
            {
                AchCycleId = cycleId,
                EffectiveEntryDate = cycle.ProcessingDate,
                BatchSequenceNumber = 1,
                ServiceClassCode = "225",
                CompanyName = "Empresa Demo",
                CompanyIdentification = "123456780",
                CompanyEntryDescription = "RECAUDOS",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "RECAUDOS"),
                OriginOrOdfi = "12345678"
            };

            arrangeContext.AchTransactions.Add(new AchTransaction
            {
                Amount = 3200m,
                Reference = "PAGO DEV14",
                Type = TransactionTypeEnum.Debit,
                TransactionCode = "27",
                OriginatingDFI = "12345678",
                ReceivingDFI = "76543210",
                TraceNumber = "123456780000456",
                TraceSequenceNumber = 456,
                EffectiveEntryDate = cycle.ProcessingDate,
                AddendaRecordIndicator = true,
                IsPrenotification = false,
                CompanyName = "Empresa Demo",
                CompanyIdentification = "123456780",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "RECAUDOS"),
                SourceAccountNumber = "111122223333",
                DestinationAccountNumber = "999988887777",
                RecipientIdNumber = "900123456",
                AchCycleId = cycle.Id,
                AchBatch = batch,
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2
            });

            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = CreateContext(connection);
        var persistedTransactionId = await executionContext.AchTransactions.Select(t => t.Id).SingleAsync();
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchReturnEligibilityRequest req, CancellationToken _) => new AchReturnEligibilityResult(true, req.ReturnReasonCode.Trim().ToUpperInvariant(), 1, "Credit", "Pending", []));
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        builder.Setup(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("NACHA_ALLOWED_VALUE_INVALID"));
        var service = new AchReturnsService(executionContext, regulatoryCatalogService: new AchRegulatoryCatalogService(executionContext), returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: builder.Object);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateReturnsFileAsync(
            new GenerateReturnsFileRequest(
                cycleId,
                [new ReturnSelectionItemDto(persistedTransactionId, "DEV14")]),
            CancellationToken.None));

        Assert.Contains("NACHA_ALLOWED_VALUE_INVALID", ex.Message, StringComparison.Ordinal);
        Assert.False(await executionContext.Set<AchReturnGenerated>().AnyAsync(x => x.OriginalTransactionId == persistedTransactionId));
        Assert.False(await executionContext.AchTransactionStateEvents.AnyAsync(x => x.AchTransactionId == persistedTransactionId));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_WhenCatalogPolicyRejectsReason_ThrowsRegulatoryMessage()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var cycle = await arrangeContext.AchCycles.SingleAsync(c => c.Id == cycleId);
            var batch = new AchBatch
            {
                AchCycleId = cycleId,
                EffectiveEntryDate = cycle.ProcessingDate,
                BatchSequenceNumber = 1,
                ServiceClassCode = "220",
                CompanyName = "Empresa Demo",
                CompanyIdentification = "123456780",
                CompanyEntryDescription = "PAGOS PSE",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                OriginOrOdfi = "12345678"
            };
            arrangeContext.AchTransactions.Add(new AchTransaction
            {
                Amount = 100m,
                Reference = "PAGO-REG-CAT",
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "22",
                OriginatingDFI = "12345678",
                ReceivingDFI = "76543210",
                TraceNumber = "123456780001111",
                TraceSequenceNumber = 1111,
                EffectiveEntryDate = cycle.ProcessingDate,
                AddendaRecordIndicator = true,
                CompanyName = "Empresa Demo",
                CompanyIdentification = "123456780",
                CompanyEntryDescriptionId = GetCompanyEntryDescriptionId(arrangeContext, "PAGOS PSE"),
                SourceAccountNumber = "111122223333",
                DestinationAccountNumber = "999988887777",
                AchCycleId = cycle.Id,
                AchBatch = batch,
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2
            });
            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = CreateContext(connection);
        var persistedTransactionId = await executionContext.AchTransactions.Select(t => t.Id).SingleAsync();
        var catalog = new Mock<IAchRegulatoryCatalogService>();
        catalog
            .Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), "R01", TransactionTypeEnum.Credit, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "La causal R01 no está permitida para Credit."));

        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(false, "R01", 1, "Credit", "Pending", [new AchReturnEligibilityFailure("RETURN_CODE_REJECTED", "La causal R01 no está permitida para Credit.")]));

        var service = new AchReturnsService(executionContext, regulatoryCatalogService: catalog.Object, returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create(), nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateReturnsFileAsync(
            new GenerateReturnsFileRequest(cycleId, [new ReturnSelectionItemDto(persistedTransactionId, "R01")]),
            CancellationToken.None));

        Assert.Contains("R01 no está permitida", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAndSaveAsync_WithValidBaseFile_ShouldParseSuccessfully()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var nachaContent = await BuildValidNachaFileAsync(connection, cycleId);

        using var parseContext = CreateContext(connection);
        var parser = BuildParser(parseContext);
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(nachaContent));

        var failures = await parser.ParseAndSaveAsync(stream, "valid-base.ach", CancellationToken.None);

        Assert.Empty(failures);
        Assert.NotNull(await parseContext.NachaHeaders.FirstOrDefaultAsync());
        Assert.NotNull(await parseContext.BatchHeaders.FirstOrDefaultAsync());
        Assert.NotNull(await parseContext.EntryDetails.FirstOrDefaultAsync());
        Assert.NotNull(await parseContext.BatchControls.FirstOrDefaultAsync());
        Assert.NotNull(await parseContext.FileControls.FirstOrDefaultAsync());
    }

    [Theory]
    [InlineData("22", "INVOICE-001", "FREE-INFO-001", null, null, null)]
    [InlineData("23", "PRENOTE-REFERENCE-001", null, null, null, null)]
    [InlineData("27", null, null, "9001234567890", "CUSTOMER-001", "SERVICE-001")]
    public void ParseAddendaLinq_WithoutProfileReader_ReadsTheSelectedType7Variant(
        string transactionCode,
        string? expectedInvoiceOrReference,
        string? expectedFreeInformation,
        string? expectedCollectorId,
        string? expectedCustomerCode,
        string? expectedServiceDescription)
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var parser = BuildParser(context);
        var record = BuildType7Variant(
            transactionCode,
            expectedInvoiceOrReference,
            expectedFreeInformation,
            expectedCollectorId,
            expectedCustomerCode,
            expectedServiceDescription);

        var addenda = Assert.Single(parser.ParseAddendaLinq(
            [record],
            new ParsedEntryDetail { TransactionCode = transactionCode },
            profileReader: null));

        Assert.Equal(expectedInvoiceOrReference, addenda.InvoiceOrAccountNumber);
        Assert.Equal(expectedFreeInformation, addenda.InfofromOriginator);
        Assert.Equal(expectedCollectorId, addenda.CollectorId);
        Assert.Equal(expectedCustomerCode, addenda.ReceiverCustomerCode);
        Assert.Equal(expectedServiceDescription, addenda.ServiceDescription);
    }

    [Fact]
    public async Task ParseAndSaveAsync_WhenBatchControlCountDoesNotMatch_ThrowsFatal51()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var nachaContent = await BuildValidNachaFileAsync(connection, cycleId);
        var records = ChunkRecords(nachaContent);
        var controlIndex = records.FindIndex(record => record.StartsWith("8"));
        records[controlIndex] = ReplaceSegment(records[controlIndex], 4, 6, "000003");

        using var parseContext = CreateContext(connection);
        var parser = BuildParser(parseContext);
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Concat(records)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAndSaveAsync(stream, "fatal51.ach", CancellationToken.None));

        Assert.Contains("D04", ex.Message);
    }

    [Fact]
    public async Task ParseAndSaveAsync_WhenBatchControlHashDoesNotMatch_ThrowsFatal52()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var nachaContent = await BuildValidNachaFileAsync(connection, cycleId);
        var records = ChunkRecords(nachaContent);
        var controlIndex = records.FindIndex(record => record.StartsWith("8"));
        records[controlIndex] = ReplaceSegment(records[controlIndex], 10, 10, "9999999999");

        using var parseContext = CreateContext(connection);
        var parser = BuildParser(parseContext);
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Concat(records)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAndSaveAsync(stream, "fatal52.ach", CancellationToken.None));

        Assert.Contains("D05", ex.Message);
    }

    [Fact]
    public async Task ParseAndSaveAsync_WhenBatchControlReservedFieldContainsData_ThrowsFatal87()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var nachaContent = await BuildValidNachaFileAsync(connection, cycleId);
        var records = ChunkRecords(nachaContent);
        var controlIndex = records.FindIndex(record => record.StartsWith("8"));
        records[controlIndex] = ReplaceSegment(records[controlIndex], 85, 6, "ABC123");

        using var parseContext = CreateContext(connection);
        var parser = BuildParser(parseContext);
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Concat(records)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAndSaveAsync(stream, "fatal87.ach", CancellationToken.None));

        Assert.Contains("Error Fatal 87", ex.Message);
    }

    [Fact]
    public async Task ParseAndSaveAsync_WhenFileControlCountDoesNotMatch_ThrowsFatal60()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var nachaContent = await BuildValidNachaFileAsync(connection, cycleId);
        var records = ChunkRecords(nachaContent);
        var fileControlIndex = records.FindIndex(record => record.StartsWith("9") && record != new string('9', 106));
        records[fileControlIndex] = ReplaceSegment(records[fileControlIndex], 13, 8, "00000099");

        using var parseContext = CreateContext(connection);
        var parser = BuildParser(parseContext);
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Concat(records)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAndSaveAsync(stream, "fatal60.ach", CancellationToken.None));

        Assert.Contains("D04", ex.Message);
    }

    [Fact]
    public async Task ParseAndSaveAsync_WhenPaddingContainsCharactersOtherThanNine_ThrowsFatal64()
    {
        using var connection = CreateOpenConnection();
        var cycleId = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate);
        var nachaContent = await BuildValidNachaFileAsync(connection, cycleId);
        var records = ChunkRecords(nachaContent);
        records[^1] = ReplaceSegment(records[^1], 50, 1, "0");

        using var parseContext = CreateContext(connection);
        var parser = BuildParser(parseContext);
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Concat(records)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAndSaveAsync(stream, "fatal64.ach", CancellationToken.None));

        Assert.Contains("D02", ex.Message);
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

    private static ITransactionPrerequisitePolicyService CreatePermissivePrerequisitePolicy()
    {
        var policy = new Mock<ITransactionPrerequisitePolicyService>();
        policy
            .Setup(x => x.ValidateForNachaExportAsync(
                It.IsAny<AchTransaction>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPrerequisiteValidationResult(true, "OK", "Política satisfecha.", null));
        return policy.Object;
    }

    private static void SeedCoreEntities(AchDbContext context)
    {
        var documentType = new DocumentTypeCatalog
        {
            Code = "DNI",
            Name = "Documento"
        };

        var personType = new PersonTypeCatalog
        {
            Code = "NAT",
            Name = "Natural"
        };

        var gender = new GenderCatalog
        {
            Code = "M",
            Name = "Masculino"
        };

        var config = new ClearingHouseConfig
        {
            Id = 1,
            ClearingHouseId = 1,
            HolidayStrategy = "Test",
            TimeZoneId = "America/Bogota"
        };

        var clearingHouse = new ClearingHouse
        {
            Id = 1,
            Name = "ACH Test",
            Code = "ACH",
            OriginCode = "ORG",
            ClearingHouseId = 1,
            ClearingHouseConfig = config
        };

        var cycleConfig = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 1,
            PolicyVersion = "TEST-V1",
            CycleName = "CICLO-TEST",
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = TimeSpan.FromHours(17),
            OutputReleaseTime = new TimeSpan(23, 59, 0),
            EffectiveFrom = TestClock.OperationalDate,
            EffectiveTo = TestClock.OperationalDate,
            IsActive = true
        };

        var cycle = new AchCycle
        {
            Id = AchCycleIdHelper.GenerateId(1, "CICLO-TEST", TestClock.OperationalDate),
            CycleName = "CICLO-TEST",
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = TimeSpan.FromHours(17),
            OutputReleaseTime = new TimeSpan(23, 59, 0),
            RescheduleOnHoliday = false,
            ClearingHouseId = 1,
            ClearingHouse = clearingHouse,
            ClearingHouseCycleConfig = cycleConfig
        };

        var sourceInstitution = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Origen",
            IsDefaultSource = true,
            RoutingNumber = "1234567",
            TransitCode = "8",
            Status = FinancialInstitutionStatus.Active
        };
        sourceInstitution.CalculateCheckDigit();

        var destinationInstitution = new FinancialInstitution
        {
            Id = 2,
            Name = "Banco Destino",
            IsDefaultSource = false,
            RoutingNumber = "7654321",
            TransitCode = "0",
            Status = FinancialInstitutionStatus.Active
        };
        destinationInstitution.CalculateCheckDigit();

        context.ClearingHouseConfigs.Add(config);
        EnsureCompanyEntryDescription(context, "PAGOS PSE");
        EnsureCompanyEntryDescription(context, "RECAUDOS");
        EnsureCompanyEntryDescription(context, "MULTICREDIT");
        context.DocumentTypes.Add(documentType);
        context.PersonTypes.Add(personType);
        context.GenderTypes.Add(gender);
        context.ClearingHouses.Add(clearingHouse);
        context.ClearingHouseCycleConfigs.Add(cycleConfig);
        context.AchCycles.Add(cycle);
        var alternativeSource = new FinancialInstitution
        {
            Id = 3,
            Name = "Banco Alterno",
            IsDefaultSource = false,
            RoutingNumber = "3333333",
            TransitCode = "1",
            Status = FinancialInstitutionStatus.Active
        };
        alternativeSource.CalculateCheckDigit();

        context.FinancialInstitutions.AddRange(sourceInstitution, destinationInstitution, alternativeSource);
        context.AchReturnCodes.AddRange(
            new AchReturnCode { ClearingHouseId = clearingHouse.Id, Code = "R01", Description = "Fondos insuficientes", AppliesToCredit = true, AppliesToDebit = true, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 15, IsActive = true },
            new AchReturnCode { ClearingHouseId = clearingHouse.Id, Code = "DEV14", Description = "No consentimiento", AppliesToCredit = false, AppliesToDebit = true, AppliesToReturn = true, RequiresAddenda = true, MaxDaysAllowed = 60, IsActive = true });
        context.AchReturnPolicies.AddRange(
            new AchReturnPolicy { ClearingHouseId = clearingHouse.Id, TransactionType = "Credit", AllowedReturnCodesCsv = "R01", MaxDays = 15, RequiredOriginalTransactionState = "Pending", RequiresAddenda = true, IsActive = true },
            new AchReturnPolicy { ClearingHouseId = clearingHouse.Id, TransactionType = "Debit", AllowedReturnCodesCsv = "R01,DEV14", MaxDays = 60, RequiredOriginalTransactionState = "Pending", RequiresAddenda = true, IsActive = true });
        context.Customers.Add(new Customer
        {
            FirstName = "Test",
            LastName = "Customer",
            Gender = gender.Code,
            PersonType = personType.Code,
            DocumentType = documentType.Code,
            DocumentNumber = "123456789",
        });
        context.CustomerAccounts.Add(new CustomerAccount
        {
            Customer = context.Customers.Local.Last(),
            AccountNumber = "111122223333"
        });
        context.SaveChanges();
    }

    private static int GetCompanyEntryDescriptionId(AchDbContext context, string term)
        => context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == term)
            .Select(x => x.Id)
            .First();

    private static IEnumerable<AddendaDto> BuildCreditAddendas(string purpose, string reference)
    {
        return
        [
            new AddendaDto
            {
                AddendaType = "05",
                BusinessType = AchAddendaBusinessType.Credit,
                Purpose = purpose,
                Reference = reference
            }
        ];
    }

    private static IEnumerable<AddendaDto> BuildDebitAddendas()
    {
        return
        [
            new AddendaDto
            {
                AddendaType = "05",
                BusinessType = AchAddendaBusinessType.Debit,
                CollectorId = "9001234567",
                ReceiverCustomerCode = "CLI0000000001",
                ServiceDescription = "FACTURA"
            }
        ];
    }

    private static void EnsureCompanyEntryDescription(AchDbContext context, string term)
    {
        if (context.CompanyEntryDescriptionCatalogs.Any(x => x.Term == term))
        {
            return;
        }

        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Term = term,
            Description = term,
            StandardEntryClassCode = "PPD",
            IsActive = true
        });
    }

    private static AchTransactionService BuildTransactionService(
        AchDbContext context,
        string cycleId,
        ITransactionPolicyService? policyServiceOverride = null)
    {
        var (service, _) = BuildTransactionServiceWithDispatchMock(context, cycleId, policyServiceOverride);
        return service;
    }

    private static (AchTransactionService Service, Mock<IContrapartidaDispatchPersistenceService> ContrapartidaDispatch) BuildTransactionServiceWithDispatchMock(
        AchDbContext context,
        string cycleId,
        ITransactionPolicyService? policyServiceOverride = null)
    {
        var routing = new Mock<IRoutingStrategyService>();
        routing
            .Setup(r => r.ResolveClearingHouseForTransactionAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycleId);

        var holiday = new Mock<IBankHoliday>();
        holiday
            .Setup(h => h.GetHolidays(It.IsAny<int>()))
            .Returns([]);

        var validator = new TransactionValidator(context);
        var achBatchRepository = new AchBatchRepository(context);
        var achTransactionRepository = new AchTransactionRepository(context);
        var batchResolver = new BatchResolver(context, achBatchRepository, routing.Object, timeProvider: TestClock.Create());
        var persister = new TransactionPersister(achTransactionRepository, achBatchRepository, validator);
        var customerRepo = new AchCustomerRepository(context);
        var thirdPartyRepo = new CustomerThirdPartyRepository(context);
        var prenotificationHandler = new PrenotificationHandler(customerRepo, thirdPartyRepo);

        var policyService = new Mock<ITransactionPolicyService>();
        policyService
            .Setup(x => x.PreviewAsync(It.IsAny<TransactionPolicyPreviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPolicyPreview(true, null, cycleId, "CICLO-TEST", TestClock.OperationalDate, "ACH Colombia", 1, "", true, null, null, null, null, false));

        var unitOfWork = new UnitOfWork(context);
        var contrapartida = new Mock<IContrapartidaDispatchPersistenceService>();
        contrapartida.Setup(x => x.EnsurePendingDispatchAsync(It.IsAny<AchTransaction>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ContrapartidaDispatchItem());

        var service = new AchTransactionService(context, unitOfWork, customerRepo, holiday.Object, validator, batchResolver, persister, prenotificationHandler, contrapartida.Object, null, null, policyServiceOverride ?? policyService.Object);
        return (service, contrapartida);
    }

    private static void SeedNachaLayouts(AchDbContext context)
    {
        var layout1 = new NachaRecordLayout
        {
            RecordType = "1",
            RecordCode = "1",
            TotalLength = 106,
            Description = "File Header"
        };
        layout1.Fields.Add(new NachaRecordField
        {
            FieldName = "CycleName",
            StartPosition = 2,
            Length = 10,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchCycle.CycleName),
            Layout = layout1
        });
        layout1.Fields.Add(new NachaRecordField
        {
            FieldName = "ProcessingDate",
            StartPosition = 12,
            Length = 8,
            PadChar = '0',
            Justification = 'R',
            DbColumn = nameof(AchCycle.ProcessingDate),
            Format = "yyyyMMdd",
            Layout = layout1
        });

        var layout5 = new NachaRecordLayout
        {
            RecordType = "5",
            RecordCode = "5",
            TotalLength = 106,
            Description = "Batch Header"
        };
        layout5.Fields.Add(new NachaRecordField
        {
            FieldName = "CompanyName",
            StartPosition = 2,
            Length = 20,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchBatch.CompanyName),
            Layout = layout5
        });
        layout5.Fields.Add(new NachaRecordField
        {
            FieldName = "EffectiveEntryDate",
            StartPosition = 22,
            Length = 8,
            PadChar = '0',
            Justification = 'R',
            DbColumn = nameof(AchBatch.EffectiveEntryDate),
            Format = "yyyyMMdd",
            Layout = layout5
        });
        layout5.Fields.Add(new NachaRecordField
        {
            FieldName = "CompanyEntryDescription",
            StartPosition = 54,
            Length = 10,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchBatch.CompanyEntryDescription),
            Layout = layout5
        });

        var layout6 = new NachaRecordLayout
        {
            RecordType = "6",
            RecordCode = "6",
            TotalLength = 106,
            Description = "Entry Detail"
        };
        layout6.Fields.Add(new NachaRecordField
        {
            FieldName = "Reference",
            StartPosition = 2,
            Length = 20,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchTransaction.Reference),
            Layout = layout6
        });
        layout6.Fields.Add(new NachaRecordField
        {
            FieldName = "Amount",
            StartPosition = 22,
            Length = 10,
            PadChar = '0',
            Justification = 'R',
            DbColumn = nameof(AchTransaction.Amount),
            Layout = layout6
        });
        layout6.Fields.Add(new NachaRecordField
        {
            FieldName = "TraceNumber",
            StartPosition = 32,
            Length = 9,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchTransaction.TraceNumber),
            Layout = layout6
        });

        var layout7 = new NachaRecordLayout
        {
            RecordType = "7",
            RecordCode = "7",
            TotalLength = 106,
            Description = "Addenda"
        };
        layout7.Fields.Add(new NachaRecordField
        {
            FieldName = "Information",
            StartPosition = 2,
            Length = 18,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchTransactionAddenda.Information),
            Layout = layout7
        });

        var layout8 = new NachaRecordLayout
        {
            RecordType = "8",
            RecordCode = "8",
            TotalLength = 106,
            Description = "Batch Control"
        };
        layout8.Fields.Add(new NachaRecordField
        {
            FieldName = "CompanyName",
            StartPosition = 2,
            Length = 18,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchBatch.CompanyName),
            Layout = layout8
        });

        var layout9 = new NachaRecordLayout
        {
            RecordType = "9",
            RecordCode = "9",
            TotalLength = 106,
            Description = "File Control"
        };
        layout9.Fields.Add(new NachaRecordField
        {
            FieldName = "CycleName",
            StartPosition = 2,
            Length = 18,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchCycle.CycleName),
            Layout = layout9
        });

        context.NachaRecordLayouts.AddRange(layout1, layout5, layout6, layout7, layout8, layout9);
        context.SaveChanges();
    }

    private static List<string> ChunkRecords(string content)
    {
        return Enumerable.Range(0, content.Length / 106)
            .Select(index => content.Substring(index * 106, 106))
            .ToList();
    }

    private static NachaParserService BuildParser(AchDbContext context)
    {
        var logger = new Mock<ILogger<NachaParserService>>();
        var stateTransitionService = new Mock<IAchStateTransitionService>();
        stateTransitionService
            .Setup(service => service.TransitionAsync(
                It.IsAny<int>(),
                It.IsAny<AchTransferStateEnum>(),
                It.IsAny<AchStateEventSourceEnum>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction());

        return new NachaParserService(context, logger.Object, stateTransitionService.Object);
    }

    private static string ReplaceSegment(string value, int startIndex, int length, string replacement)
    {
        Assert.Equal(length, replacement.Length);
        return string.Concat(value.AsSpan(0, startIndex), replacement, value.AsSpan(startIndex + length));
    }

    private static async Task<string> BuildValidNachaFileAsync(SqliteConnection connection, string cycleId)
    {
        using var arrangeContext = CreateContext(connection);
        SeedCoreEntities(arrangeContext);

        var processingDate = TestClock.OperationalDate;
        const string receivingDfi = "76543210";
        const string companyId = "1234567800";
        const string batchNumber = "0000001";
        const string traceNumber = "123456780000001";
        const string accountNumber = "999988887777";
        const string amount18 = "000000000000150000"; // 1500.00
        const string hash10 = "0076543210";
        const string entryAddendaCount6 = "000002";
        const string entryAddendaCount8 = "00000002";
        const string blockCount = "000001";
        const string batchCount = "000001";

        var records = new List<string>
        {
            BuildType1(processingDate),
            BuildType5(processingDate, companyId, batchNumber),
            BuildType6(receivingDfi, accountNumber, amount18, traceNumber),
            BuildType7(traceNumber),
            BuildType8(entryAddendaCount6, hash10, amount18, companyId, batchNumber),
            BuildType9(batchCount, blockCount, entryAddendaCount8, hash10, amount18),
            new string('9', 106),
            new string('9', 106),
            new string('9', 106),
            new string('9', 106)
        };

        await Task.CompletedTask;
        return string.Concat(records);
    }

    private static string BuildType1(DateTime processingDate)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '1';
        Copy("01", line, 1);
        Copy("0000000001", line, 3);
        Copy("ORG", line, 13);
        Copy(processingDate.ToString("yyyyMMdd"), line, 23);
        Copy("1200", line, 31);
        Copy("A", line, 35);
        Copy("106", line, 36);
        Copy("10", line, 39);
        Copy("1", line, 41);
        Copy("ACH COLOMBIA".PadRight(23), line, 42);
        Copy("COOP TEST".PadRight(23), line, 65);
        Copy("REF00001", line, 88);
        return new string(line);
    }

    private static string BuildType5(DateTime processingDate, string companyId, string batchNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '5';
        Copy("220", line, 1);
        Copy("EMPRESA DEMO".PadRight(16), line, 4);
        Copy(companyId.PadRight(10), line, 40);
        Copy("PPD", line, 50);
        Copy("MULTICREDIT", line, 53);
        Copy(processingDate.ToString("yyyyMMdd"), line, 63);
        Copy(processingDate.ToString("yyyyMMdd"), line, 71);
        Copy("1", line, 82);
        Copy("12345678", line, 83);
        Copy(batchNumber, line, 91);
        return new string(line);
    }

    private static string BuildType6(string receivingDfi, string accountNumber, string amount18, string traceNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        var checkDigit = DigitoChequeoHelper.CalcularDigitoChequeo(receivingDfi);
        line[0] = '6';
        Copy("22", line, 1);
        Copy(receivingDfi, line, 3);
        Copy(checkDigit, line, 11);
        Copy(accountNumber.PadRight(17), line, 12);
        Copy(amount18, line, 29);
        Copy("900000001".PadLeft(15), line, 47);
        Copy("CLIENTE CREDITO".PadRight(22), line, 62);
        Copy("  ", line, 84);
        Copy("1", line, 86);
        Copy(traceNumber, line, 87);
        return new string(line);
    }

    private static string BuildType7(string traceNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '7';
        Copy("05", line, 1);
        Copy("INFO-ADDENDA".PadRight(80), line, 3);
        Copy("0001", line, 83);
        Copy(traceNumber[^7..], line, 87);
        return new string(line);
    }

    private static string BuildType7Variant(
        string transactionCode,
        string? invoiceOrReference,
        string? freeInformation,
        string? collectorId,
        string? customerCode,
        string? serviceDescription)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '7';
        Copy("05", line, 1);
        if (transactionCode == "27")
        {
            Copy(collectorId!.PadRight(13), line, 3);
            Copy(customerCode!.PadRight(30), line, 16);
            Copy(serviceDescription!.PadRight(15), line, 46);
        }
        else
        {
            Copy("ORIGINATOR-001".PadRight(15), line, 3);
            Copy("PURPOSE-01".PadRight(10), line, 20);
            Copy(invoiceOrReference!.PadRight(transactionCode == "23" ? 53 : 24), line, 30);
            if (transactionCode == "22")
            {
                Copy(freeInformation!.PadRight(24), line, 56);
            }
        }

        Copy("0001", line, 83);
        Copy("0000001", line, 87);
        return new string(line);
    }

    private static string BuildType8(string entryAddendaCount6, string hash10, string amount18, string companyId, string batchNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '8';
        Copy("220", line, 1);
        Copy(entryAddendaCount6, line, 4);
        Copy(hash10, line, 10);
        Copy("000000000000000000", line, 20);
        Copy(amount18, line, 38);
        Copy(companyId.PadRight(10), line, 56);
        Copy(new string(' ', 19), line, 66);
        Copy(new string(' ', 6), line, 85);
        Copy("12345678", line, 91);
        Copy(batchNumber, line, 99);
        return new string(line);
    }

    private static string BuildType9(string batchCount, string blockCount, string entryAddendaCount8, string hash10, string amount18)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '9';
        Copy(batchCount, line, 1);
        Copy(blockCount, line, 7);
        Copy(entryAddendaCount8, line, 13);
        Copy(hash10, line, 21);
        Copy("000000000000000000", line, 31);
        Copy(amount18, line, 49);
        Copy(new string(' ', 39), line, 67);
        return new string(line);
    }

    private static void Copy(string value, char[] buffer, int index)
    {
        value.AsSpan().CopyTo(buffer.AsSpan(index, value.Length));
    }
}
