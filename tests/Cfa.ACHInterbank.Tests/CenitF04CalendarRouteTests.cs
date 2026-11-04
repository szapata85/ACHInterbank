using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class CenitF04CalendarRouteTests
{
    private static readonly DateTime Monday10 = new(2026, 8, 10);
    private static readonly DateTime Friday14 = new(2026, 8, 14);
    private static readonly DateTime Tuesday18 = new(2026, 8, 18);

    [Theory]
    [InlineData(18, true)]
    [InlineData(17, false)]
    public async Task OutboundR23_UsesBankCalendarRatherThanSuppliedDeadline(int computedDay, bool permits)
    {
        var calendar = new Mock<IOperationalCalendarService>(MockBehavior.Strict);
        calendar.Setup(x => x.GetNextBusinessDayAsync(new DateOnly(2026, 8, 15), 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateOnly(2026, 8, computedDay));
        var policy = new AchReturnEligibilityPolicyComponent(
            null!, Mock.Of<IAchReturnEligibilityService>(), new CenitIncomingReturnPolicy(), calendar.Object);
        var original = new AchCycle { Id = "origin", ProcessingDate = Monday10, ClearingHouseId = 9 };
        var target = new AchCycle { Id = "target", ProcessingDate = Tuesday18, ClearingHouseId = 9 };
        var tx = new AchTransaction
        {
            Id = 71, Amount = 100m, Type = TransactionTypeEnum.Credit,
            EffectiveEntryDate = Monday10, AchCycle = original
        };
        var scope = new AchReturnGenerationScope(original, target, null, false, true);
        var correlation = new AchReturnCorrelationResult(scope, new[] { 71 },
            new Dictionary<int, AchTransaction> { [71] = tx },
            new Dictionary<int, AchReturnCenitOriginalSource>(), new AchReturnCenitCycleEvidence(1, 3, 5));
        var item = new ReturnSelectionItemDto(71, "R23", new CenitIncomingReturnOperationalEvidence(
            ReturnRequestDate: Friday14,
            ImmediateReturnCycleConfirmed: true,
            ReceiverRejectionDeadlineDate: Tuesday18.AddDays(10), // Must be ignored.
            ReceiverNotificationReceivedDate: Friday14));

        if (permits)
        {
            await policy.ValidateCenitOperationalPolicyAsync(tx, scope, correlation, item, "R23", 100m, CancellationToken.None);
        }
        else
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                policy.ValidateCenitOperationalPolicyAsync(tx, scope, correlation, item, "R23", 100m, CancellationToken.None));
            Assert.StartsWith("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED:", failure.Message);
        }

        calendar.VerifyAll();
    }
}
