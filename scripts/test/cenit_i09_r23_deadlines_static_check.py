#!/usr/bin/env python3
"""I09 source guard only: not a .NET runtime, SQL, holiday DB or CENIT test."""
from pathlib import Path
import sys
p = Path(__file__).resolve().parents[2]
pol = (p / 'src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs').read_text()
out = (p / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs').read_text()
inb = (p / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs').read_text()
calendar = (p / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/OperationalCalendarService.cs').read_text()
banking = (p / 'src/Cfa.ACHInterbank.Application/ACH/Services/CenitR23BankingDate.cs').read_text()
pre = (p / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityService.cs').read_text()
catalog = (p / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchRegulatoryCatalogService.cs').read_text()
cat_tests = (p / 'tests/Cfa.ACHInterbank.Tests/AchRegulatoryCatalogClearingHouseTests.cs').read_text()
t = (p / 'tests/Cfa.ACHInterbank.Tests/CenitI09R23DeadlineTests.cs').read_text()
checks = {
    'Policy still enforces full original R23 amount': 'CENIT_R23_PARTIAL_OR_AMOUNT_MISMATCH' in pol,
    'Policy preserves intraday next-session rule': 'CENIT_R23_SAME_DAY_NEXT_CYCLE_ALLOWED' in pol,
    'Policy uses authoritative actual operational date': 'var actualReturnDay = request.ReturnOperationalDate?.Date ?? request.ReturnValueDate.Date;' in pol,
    'Policy distinguishes original receipt before return day': 'var receivedBeforeReturnDay = request.OriginalReceivedDate.HasValue' in pol,
    'Policy prevents earlier-day claim being classified only by value date': 'if (receivedToday || (sameValueDate && !receivedBeforeReturnDay))' in pol,
    'Policy validates intraday return value date where operational evidence exists': 'request.ReturnValueDate.Date != actualReturnDay' in pol,
    'Policy requires original receipt evidence': 'CENIT_R23_ORIGINAL_RECEIPT_REQUIRED' in pol,
    'Policy requires resolved return operational date': 'CENIT_R23_RETURN_OPERATIONAL_DATE_REQUIRED' in pol,
    'Policy derives actual submission date from resolved return cycle': 'var submission = request.ReturnOperationalDate.Value.Date;' in pol,
    'Policy prevents chronology reversal': 'receipt >= submission' in pol,
    'Policy requires value date of later claim to match submission date': 'CENIT_R23_LATER_VALUE_DATE_MISMATCH' in pol,
    'Policy measures 15 calendar days from actual submission and receipt': '(submission - receipt).Days > 15' in pol,
    'Policy does NOT measure limit with value date': '(request.ReturnValueDate.Date - receipt).Days > 15' not in pol,
    'Notification clock has Bogotá timezone identifier': 'RegulatoryCycleScheduleCatalog.BogotaTimeZoneId' in banking,
    'Notification clock converts UTC instants': 'TimeZoneInfo.ConvertTimeFromUtc(utc, bogota)' in banking,
    'Notification clock preserves unspecified local banking dates': 'timestamp.Kind == DateTimeKind.Unspecified' in banking,
    'Policy uses normalized notification day': 'CenitR23BankingDate.FromNotificationTimestamp(request.ReceiverNotificationReceivedDate.Value)' in pol,
    'Policy uses normalized claim day': 'CenitR23BankingDate.FromNotificationTimestamp(request.ReturnRequestDate.Value)' in pol,
    'Policy requires notification receipt evidence': 'CENIT_R23_NOTIFICATION_RECEIPT_REQUIRED' in pol,
    'Policy limits notification date against submission day': 'notificationDate > submission' in pol,
    'Policy requires bank-calculated deadline': 'CENIT_R23_CALENDAR_VALIDATION_REQUIRED' in pol,
    'Policy compares actual submission to calculated deadline': 'return submission <= request.CalculatedNextBusinessDayDeadline.Value.Date' in pol,
    'Policy does NOT trust external submitted deadline': 'request.ReceiverRejectionDeadlineDate.Value.Date' not in pol,
    'Outbound calendar trigger uses original operational receipt': 'scope.ReturnCycle.ProcessingDate.Date > transaction.AchCycle.ProcessingDate.Date' in out,
    'Outbound trigger does NOT use original value date': 'scope.ReturnCycle.ProcessingDate.Date > transaction.EffectiveEntryDate.Date' not in out,
    'Outbound calculates after normalized Bogotá notification day': 'CenitR23BankingDate.FromNotificationTimestamp(evidence.ReceiverNotificationReceivedDate.Value).AddDays(1)' in out,
    'Outbound uses clearing house-specific calendar': 'scope.ReturnCycle.ClearingHouseId, ct' in out,
    'Inbound trigger uses resolved operational cycle dates': 'resolvedReturnCycle.ProcessingDate.Date > originalCycle.ProcessingDate.Date' in inb,
    'Inbound trigger does NOT use original value date': 'returnValueDate.Value.Date > transaction.EffectiveEntryDate.Date' not in inb,
    'Inbound calculates after normalized Bogotá notification day': 'CenitR23BankingDate.FromNotificationTimestamp(evidence.ReceiverNotificationReceivedDate.Value).AddDays(1)' in inb,
    'Inbound uses resolved clearing house calendar': 'ingestion.ResolvedClearingHouseId.Value, ct' in inb,
    'Generic eligibility scopes exemption to credit R23': 'tx.Type == Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit' in pre and 'normalizedReasonCode == "R23"' in pre,
    'Generic eligibility verifies CENIT identity from database': 'x.Id == clearingHouseId.Value && x.Code == "CENIT"' in pre,
    'Generic eligibility uses actual original cycle receipt for R23': 'tx.AchCycle!.ProcessingDate.Date' in pre and 'originalWindowStart' in pre,
    'Catalog code validation receives normalized receipt start': 'tx.Type,\n            originalWindowStart,\n            request.ReturnDate.Date' in pre,
    'Catalog policy validation receives normalized receipt start': 'normalizedReasonCode,\n            originalWindowStart,\n            request.ReturnDate.Date' in pre,
    'Generic CENIT R23 credit does not inherit one-day credit cutoff': 'var maxDays = isCenitCreditR23 ? 15 : policy.MaxDays;' in catalog,
    'Catalog exception requires CREDIT transaction': 'transactionType == TransactionTypeEnum.Credit' in catalog,
    'Catalog exception requires R23 code': 'string.Equals(returnCode.Trim(), "R23", StringComparison.OrdinalIgnoreCase)' in catalog,
    'Catalog exception is CENIT only': 'x.Id == clearingHouseId && x.Code == "CENIT"' in catalog,
    'Other codes and chambers preserve configured max': 'isCenitCreditR23 ? 15 : policy.MaxDays' in catalog,
    'SQLite tests enforce R23 day15/day16 and ACH isolation': 'I09_GenericCreditPolicy_OnlyCenitR23UsesFifteenDays' in cat_tests and '[InlineData("CENIT", "R23", 15, true)]' in cat_tests and '[InlineData("CENIT", "R23", 16, false)]' in cat_tests and '[InlineData("ACHCOL", "R23", 2, false)]' in cat_tests,
    'Existing calendar rejects weekends': 'date.DayOfWeek == DayOfWeek.Saturday' in calendar and 'date.DayOfWeek == DayOfWeek.Sunday' in calendar,
    'Existing calendar checks national holidays': 'GetNationalHolidaysAsync(date.Year, ct)' in calendar,
    'Existing calendar checks active CENIT special closures': 'x.ClearingHouseId == clearingHouseId && x.Date == date && x.IsActive' in calendar,
    'Tests include day0 check': 'DayZero_OnlyNextSessionIsAllowed' in t,
    'Tests include inclusive day15 and day16': 'FifteenCalendarDays_InclusiveFromActualReceipt' in t and '[InlineData(16,' in t,
    'Tests include value date manipulation': 'BackdatedReturnValueDate_CannotConcealSubmissionOnDaySixteen' in t,
    'Tests include operational date missing': 'MissingResolvedOperationalDay_RequiresManualReview' in t,
    'Tests include original receipt missing': 'MissingOriginalReceipt_RequiresManualReview' in t,
    'Tests include notification missing': 'MissingNotification_RequiresManualReviewEvenWithSuppliedExternalDeadline' in t,
    'Tests include external deadline not trusted': 'MissingCalendarDeadline_CannotBeReplacedByExternalDeadline' in t,
    'Tests include first next business day after Friday holiday': '[InlineData(2026, 8, 14, 2026, 8, 18)]' in t,
    'Tests include UTC boundary in Bogotá': 'UtcAfterMidnight_UsesPreviousBogotaBankingDate' in t,
    'Tests cover calendar lookup on local banking date': 'OutboundDeadline_ConvertsUtcNotificationToBogotaBeforeCalendarLookup' in t,
    'Tests include New Year transition': '[InlineData(2026, 12, 31, 2027, 1, 4)]' in t,
    'Tests verify production calendar invocation': 'calendar.VerifyAll();' in t,
    'Tests do not claim executed runtime': 'require dotnet test' in t,
}
for i,(description,ok) in enumerate(checks.items(),1):
    print(f'{i:02d} {"PASS" if ok else "FAIL"}: {description}')
print(f'I09 STATIC: {sum(checks.values())}/{len(checks)}; no .NET, DB, bank operator or UAT executed.')
sys.exit(0 if all(checks.values()) else 1)
