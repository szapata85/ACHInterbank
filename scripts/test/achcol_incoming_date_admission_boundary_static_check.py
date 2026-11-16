#!/usr/bin/env python3
"""Static source invariants for ACHCOL admission -> parser date semantics.
NOT a .NET build, xUnit, physical NACHA execution or operator homologation.
"""
from pathlib import Path

r = Path(__file__).resolve().parents[2]
src = r / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
admission = (src / 'IncomingNachaAdmissionValidator.cs').read_text()
parser = (src / 'NachaParserService.cs').read_text()
validation = (src / 'NachaInboundValidation.cs').read_text()
physical = (src / 'NachaPhysicalParser.cs').read_text()
unit = r / 'tests/Cfa.ACHInterbank.Tests'
t_admission = (unit / 'IncomingNachaAdmissionAchColFileBasedTests.cs').read_text()
t_cenit = (unit / 'IncomingNachaTraceabilityCoreTests.cs').read_text()
t_parser = (unit / 'AchTransactionNachaTests.cs').read_text()

checks = {
    'Actual CH identity loaded by id before ACHCOL deferral': 'var resolvedHouse = request.Resolution.ClearingHouseId' in admission and 'FirstOrDefaultAsync(x => x.Id == resolvedHouseId' in admission,
    'Identity requires ACTIVE ACHCOL and resolved unambiguous': 'var isVerifiedAchColombia = request.Resolution.IsResolved' in admission and '!request.Resolution.IsAmbiguous' in admission and 'resolvedHouse?.IsActive == true' in admission and '"ACHCOL"' in admission,
    'CENIT/other retains file-level mismatch gate': 'if (!isVerifiedAchColombia && effectiveDate.HasValue && effectiveDate.Value != headerDate)' in admission,
    'No early ACHCOL bad effective date rejection': '"ACHCOL_EFFECTIVE_DATE_INVALID"' not in admission,
    'Preview uses ACHCOL eight-digit date format': 'ParseEffectiveDate(request.Records, isVerifiedAchColombia)' in admission,
    'Header vs filename fatal gate retained': '"HEADER_DATE_MISMATCH"' in admission,
    'CH authorization retained for file-based route': '"ACHCOL_CLEARING_HOUSE_NOT_AUTHORIZED"' in admission,
    'Header correlation retained for ACHCOL': 'AchColInboundHeaderCorrelation.TryValidate(' in admission,
    'Header creation time fatal retained': '"ACHCOL_HEADER_TIME_INVALID"' in admission,
    'Bank-calendar gate retained': '"NON_BUSINESS_DAY"' in admission and 'BankHolidays.AsNoTracking()' in admission,
    'Cycle unresolved/closed/win gates retained': all(x in admission for x in ['"CYCLE_NOT_RESOLVED"', '"CYCLE_ALREADY_CLOSED"', '"CYCLE_RECEPTION_WINDOW_EXPIRED"']),
    'Parser file-based scoped to verified CH code and resolved date': 'var achColFileBasedDate = request is not null' in parser and 'string.Equals(resolvedHouseCode, "ACHCOL"' in parser and 'request.OperationalDate.HasValue' in parser,
    'Parser no cycle inference for file-based ACHCOL': 'if (achCycleId is null && !achColFileBasedDate)' in parser,
    'Parser checks resolved date against physical header': 'actualHeaderDate.Value.Date != request!.OperationalDate!.Value.Date' in parser,
    'Parser checks resolved house against physical header': 'currentHeader.ClearingHouseId != request.ResolvedClearingHouseId' in parser,
    'Parser assigns authoritative operational date': 'currentOperationalProcessingDate = request.OperationalDate.Value.Date;' in parser,
    'Parser fails closed on mismatch': 'ACHCOL_OPERATIONAL_DATE_UNRESOLVED: la fecha/cámara resuelta' in parser,
    'Per-batch D01/D02 calculation retained': 'GetAchColombiaEffectiveDateFailure(' in parser and 'currentBatch.EffectiveEntryDate, currentOperationalProcessingDate' in parser,
    'Per-T6 rejection retained': 'failures.Add(new NachaValidationFailure("6"' in parser and 'currentBatchDateFailure' in parser,
    'V35 past date maps D01': 'effective.Value.Date < operationalDate.Value.Date' in validation and 'GetRegulatoryError("D01"' in validation,
    'V35 absent/invalid/future maps D02': 'if (!effective.HasValue)' in validation and 'effective.Value.Date > operationalDate.Value.Date' in validation and validation.count('GetRegulatoryError("D02"') >= 2,
    'Physical byte/frame guards untouched': all(x in physical for x in ['ACHCOL-PHYSICAL-RECORD-LENGTH','ACHCOL-PHYSICAL-NO-BOM','ACHCOL-PHYSICAL-NO-LINE-ENDINGS','ACHCOL-PHYSICAL-ASCII-REPERTOIRE']),
    'Positive ACHCOL admission fixture': 'ValidAchColFile_IsAdmittedWithoutAnyAchCycle' in t_admission,
    'Negative/blank ACHCOL admission fixtures': 'BatchEffectiveDate_IsDeferredToPerTransactionOperatorReturns' in t_admission and '[InlineData("        ", false)]' in t_admission,
    'Multi-batch admission fixture': 'MultipleBatches_DoNotRejectWholeAchColFileFromFirstOrLaterEffectiveDate' in t_admission,
    'CENIT vs ACHCOL control test': 'Admission_EffectiveDateMismatch_UsesVerifiedChamberSemantics' in t_cenit,
    'Physical file-based valid parser regression': 'ParseAndSaveDetailedAsync_AchColFileBasedValidDate_AcceptsPhysicalTransactionsWithoutCycle' in t_parser,
    'Physical file-based D01/D02 parser regression': 'ParseAndSaveDetailedAsync_AchColFileBasedPhysicalFile_UsesResolvedDateWithoutCycle' in t_parser,
    'Physical parser header mismatch negative': 'ParseAndSaveDetailedAsync_AchColFileBasedDateDisagreesWithHeader_FailsClosed' in t_parser,
    'Historic CENIT parser remains separate': 'NachaInboundRejectionCatalogSelector.EnsureNachaProfileMatchesChamber' in parser,
}
for i,(name,passed) in enumerate(checks.items(),1):
    print(f'{i:02} {"PASS_STATIC" if passed else "FAIL_STATIC"} {name}')
print(f'TOTAL {sum(checks.values())}/{len(checks)} STATIC, xUnit NOT EXECUTED')
raise SystemExit(0 if all(checks.values()) else 1)
