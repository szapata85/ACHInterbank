#!/usr/bin/env python3
"""Source-only invariants for the three pre-deployment hotfixes; NOT runtime tests."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
implementation = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
validation = (implementation / 'NachaInboundValidation.cs').read_text(encoding='utf-8')
parser = (implementation / 'NachaParserService.cs').read_text(encoding='utf-8')
r10 = (implementation / 'AchReturnEligibilityPolicyComponent.cs').read_text(encoding='utf-8')
ror = (implementation / 'CenitReturnOfReturnService.cs').read_text(encoding='utf-8')
units1 = (root / 'tests/Cfa.ACHInterbank.Tests/NachaInboundValidationSeparationTests.cs').read_text(encoding='utf-8')
units2 = (root / 'tests/Cfa.ACHInterbank.Tests/AchReturnsFileByClearingHouseTests.cs').read_text(encoding='utf-8')
units3 = (root / 'tests/Cfa.ACHInterbank.Tests/CenitReturnOfReturn2026Tests.cs').read_text(encoding='utf-8')
ach = (root / 'docs/normativa/md/ACH-Colombia-V35.md').read_text(encoding='utf-8')
cenit = (root / 'docs/normativa/md/CENIT-DSP-152-Anexo-2.md').read_text(encoding='utf-8')
checks = {
    'V35 D01 fecha efectiva anterior': 'D01 La fecha efectiva es menor a la fecha de proceso' in ach,
    'V35 D02 fecha efectiva inválida': 'D02 La fecha efectiva no es válida' in ach,
    'V35 D04 prenotificación no cero': 'D04 El valor de la transacción de prenotificación es diferente de cero' in ach,
    'ACH date comparison method': 'ValidateAchColombiaEffectiveDate' in validation,
    'D01 mapped exclusively to compared effective date': 'effective.Value.Date < operationalDate.Value.Date' in validation and 'GetRegulatoryError("D01",' in validation,
    'D02 covers absent and invalid date': 'if (!effective.HasValue)' in validation and 'GetRegulatoryError("D02",' in validation,
    'D04 linked to prenotification amount': 'GetRegulatoryError("D04", "El valor de la transacción de prenotificación' in validation,
    'No misuse of D01 D02 D04 in parser': not re.search(r'ThrowRegulatory\("D0[124]"', parser),
    'No misuse of D01 D02 D04 in validator': not re.search(r'ThrowRegulatory\("D0[124]"', validation),
    'Technical duplicate trace': 'NachaInboundValidator.ThrowTechnical("Número de secuencia duplicado' in validation,
    'Technical duplicate NACHA file': 'ThrowTechnical("El Archivo NACHA ya existe!' in parser,
    'Technical invalid filler': 'ThrowTechnical("Los registros de relleno después del Registro Tipo 9' in parser,
    'No D04 for invalid counts': not re.search(r'ThrowRegulatory\("D04"', validation),
    'CENIT physical fatal separated': 'CENIT-NACHA-FATAL' in validation,
    'ACH legacy catalogue descriptions overridden by V35': 'var achColombiaDescription = dxxCode.ToUpperInvariant() switch' in validation,
    'ACH Dxx semantic tests': 'AchColombiaEffectiveDate_UsesOnlyNormativeDateCauses' in units1 and 'AchColombiaPrenotificationAmount_CorrectlyReportsD04OnlyOnAchRail' in units1,
    'ACH technical negative tests': 'AchColombiaBatchCountFailure_IsTechnicalAndNeverPretendsPrenoteD04' in units1,
    'R10 still scopes to processing date': 'cycle.ProcessingDate.Date == originalCycle.ProcessingDate.Date' in r10,
    'R10 effective policy unchanged': 'ResolveAchColombiaMaxReturnCyclesAsync' in r10 and 'returnOpportunityCount > maxReturnCycles' in r10,
    'R10 operational chronology instead of time of day': '.OrderBy(ResolveOperationalCycleStart)' in r10 and '.OrderBy(cycle => cycle.StartTime)' not in r10,
    'R10 previous calendar date': 'startDate = startDate.AddDays(-1)' in r10,
    'R10 seven cycles full matrix': 'R10_Final_CatalogExactlyMatchesAllPublishedPairs' in units2 and 'SevenCycleNormativeMatrix' in units2,
    'R10 midnight boundary unit': 'R10_Hotfix02_CycleOneStartsPreviousCalendarDateEvenAcrossMidnight' in units2,
    'No missing operational date can bypass D01': 'ACHCOL_OPERATIONAL_DATE_UNRESOLVED' in validation and 'ThrowTechnical("ACHCOL_OPERATIONAL_DATE_UNRESOLVED' in validation,
    'ACH V35 operator cause is attached per entry': 'currentBatchDateFailure' in parser and 'failures.Add(new NachaValidationFailure("6"' in parser,
    'R10 uses bank holidays for original and return': r10.count('operationalCalendarService.IsBusinessDayAsync') >= 2,
    'R10 uses previous BANK operational day': 'GetPreviousBusinessDayAsync(' in r10 and 'returnDate.AddDays(-1)' in r10,
    'R10 uses canonical published windows': 'RegulatoryCycleScheduleCatalog.AllowsAchColombiaSevenCycleReturn(' in r10,
    'No unconditional previous-day R10 rejection': 'ACHCOL_RETURN_PREVIOUS_OPERATIONAL_DAY' not in r10,
    'No remaining strict same-date R10 condition': 'if (originalCycle.ProcessingDate.Date != returnCycle.ProcessingDate.Date)' not in r10,
    'Historical five-cycle R10 policy retained': 'ResolveAchColombiaMaxReturnCyclesAsync(' in r10,
    'R10 source/destination pair matrices unit data': 'R10_Final_PolicyEvaluatesActualOperationalDaysAndPairs' in units2,
    'R10 backend supports unresolved navigation': 'var allowsReturn = await context.AchCycles.AsNoTracking()' in r10,
    'ROR fixed C5 normative cycle preserved': 'private const int CenitLastRegularCycleNumber = 5' in ror and 'targetNumber.Value == CenitLastRegularCycleNumber' in ror,
    'ROR same-day bank calendar': 'if (target.ProcessingDate.Date == parent.ProcessingDate.Date)' in ror and 'IsBusinessDayAsync' in ror,
    'ROR target bank calendar for next-day path': ror.count('operationalCalendarService.IsBusinessDayAsync') >= 2,
    'ROR alternative next business day retained': 'GetNextBusinessDayAsync(' in ror and 'targetNumber.Value <= 2' in ror,
    'ROR both directions share the calendar gate': ror.count('var operationalFailure = await ValidateOperationalWindowAsync(') == 2,
    'ROR bank holiday, Sunday, Saturday and business day tests': all(x in units3 for x in ['InlineData(2026, 8, 22)', 'InlineData(2026, 8, 23)', 'InlineData(2026, 8, 17)', 'RorSameDay_ValidWednesdayPermitsC5AndRetainsHcCenit002']),
    'ROR non-operational target expected rejection': 'CENIT_ROR_NON_BUSINESS_DAY' in ror and 'RorNextBusinessDay_RejectsTargetOnWeekendDespiteValidCycleNumber' in units3,
    'CENIT DSP-152 banking day restriction': 'no estará disponible los' in cenit,
}
for index, (key, passed) in enumerate(checks.items(), 1):
    print(f'{index:02} {"PASS_STATIC" if passed else "FAIL_STATIC"} {key}')
print(f'TOTAL: {sum(checks.values())}/{len(checks)} source checks; runtime .NET/xUnit NOT executed')
raise SystemExit(0 if all(checks.values()) else 1)
