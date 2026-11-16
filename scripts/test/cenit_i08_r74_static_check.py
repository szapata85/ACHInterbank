#!/usr/bin/env python3
"""I08 focused source checks; static only, NOT xUnit/SQL/real 106-byte output."""
from pathlib import Path
import sys
p = Path(__file__).resolve().parents[2]
src = p/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
checks = {}
svc = (src/'CenitReturnOfReturnService.cs').read_text(encoding='utf-8')
gen = (src/'AchReturnOfReturnFileGenerationService.cs').read_text(encoding='utf-8')
ev = (src/'CenitR74OriginalCreditEvidence.cs').read_text(encoding='utf-8')
tests = (p/'tests/Cfa.ACHInterbank.Tests/CenitReturnOfReturn2026Tests.cs').read_text(encoding='utf-8')
manual = (p/'scripts/test/cenit_f05_static_check.py').read_text(encoding='utf-8')
checks['ROR outgoing registration calls shared R74 rule'] = 'CenitR74OriginalCreditEvidence.ExistsAsync(context, original, ct)' in svc
checks['ROR incoming registration calls shared R74 rule'] = 'CenitR74OriginalCreditEvidence.ExistsAsync(context, parent.OriginalTransaction, ct)' in svc
checks['NACHA ROR generator uses identical R74 rule'] = 'CenitR74OriginalCreditEvidence.ExistsAsync(context, original, ct)' in gen
checks['No duplicated previous R74 validation remains'] = 'HasR74OriginalCreditEvidenceAsync' not in svc and 'static bool HasInfo' not in gen
checks['Parent R32 restriction preserved'] = 'CENIT_R74_R32_PARENT_REQUIRED' in svc and 'CENIT_R74_R32_PARENT_REQUIRED' in gen
checks['ROR physical addenda type 99 separate'] = 'CenitReturnOfReturn2026Layout' in gen and 'CodeTypeAddendumRecord == "05"' in ev
checks['Only credit transactions valid'] = 'original.Type != TransactionTypeEnum.Credit' in ev
checks['Original type6 code also monetary credit'] = '!MonetaryCreditCodes.Contains(original.TransactionCode?.Trim() ?? string.Empty)' in ev
checks['Debit code disguised as credit rejected'] = 'I08_R74_RejectsDebitCodeDisguisedAsCreditDespiteType05Addenda' in tests
checks['Prenotification not valid'] = 'original.IsPrenotification' in ev
checks['Amount must be positive'] = 'original.Amount <= 0' in ev
checks['Original-specific ID required'] = 'x.AchTransactionId == original.Id' in ev
checks['Raw entry is original trace'] = 'entry.SequenceNumber?.Trim(), original.TraceNumber?.Trim()' in ev
checks['Raw entry matches original amount'] = 'entry.Amount == original.Amount' in ev
checks['Raw entry has CENIT monetary credit code'] = 'MonetaryCreditCodes.Contains(code)' in ev and all(f'"{c}"' in ev for c in ('22','32','42','52'))
checks['Raw entry code matches original transaction code'] = 'original.TransactionCode?.Trim()' in ev
checks['Raw evidence link is final'] = 'return link.IsFinal' in ev
checks['Raw type05 matches its entry'] = 'entry.EntryDetailID == addenda.EntryDetailId' in ev
checks['Raw addenda and detail from same file'] = 'entry.NachaID, addenda.NachaID' in ev
checks['Wrong raw return row rejected in test'] = 'I08_R74_RejectsPaymentAddendaLinkedToReturnInsteadOfOriginalEntry' in tests
checks['Wrong raw original trace rejected in test'] = 'I08_R74_RejectsType05WithIncorrectOriginalTraceEvenWhenField3Valid' in tests
checks['Service full R74 path test with replay'] = 'I08_R74_OutgoingRegistrationUsesAuthoritativeRawField3' in tests
checks['No R32 return addenda as payment evidence'] = 'x.AddendaRecord.CodeTypeAddendumRecord == "05"' in ev
checks['No return reason on original type05 evidence'] = 'x.AddendaRecord.ReturnReasonCode == null' in ev
checks['Original raw field 3 primary'] = 'record.PaymentRelatedInformation' in ev and 'if (raw.Count > 0)' in ev
checks['Inconsistent local metadata cannot override raw'] = 'return raw.Any(x => HasCorrelatedOriginalEntry(x, original) && HasValidPhysicalField3(x.AddendaRecord!));' in ev
checks['Physical field limited to 80 characters'] = 'field.Length > 80' in ev
checks['Physical ID in positions 1-15'] = 'padded[..15]' in ev
checks['Physical PPD reserved positions 16-17'] = 'padded.Substring(15, 2) == "  "' in ev
checks['Physical PPD reserved positions 52-53'] = 'padded.Substring(51, 2) == "  "' in ev
checks['Physical PPD reserved positions 78-80'] = 'padded.Substring(77, 3) == "   "' in ev
checks['Zero-only originator is disallowed'] = "trimmed.Any(c => c != '0')" in ev
checks['Non-digit originator is disallowed'] = 'trimmed.All(char.IsAsciiDigit)' in ev
checks['Outgoing semantic fallback type05 only'] = 'x.AddendaType == "05"' in ev
checks['Outgoing semantic fallback checks company ID'] = 'HasNumericOriginator(original.CompanyIdentification)' in ev
checks['Outgoing semantic fallback checks actual payment'] = 'HasInformation(x.Information) || HasInformation(x.Reference)' in ev
checks['Existing positive and idempotency tests retained'] = 'F05_R74_RequiresMonetaryCreditAndOriginalPaymentInformation' in tests and 'replay.WasDuplicate.Should().BeTrue()' in tests
checks['Physical R74 106-byte builder test exists'] = 'I08_R74_GeneratedNachaPhysicalAddenda99PreservesR32SourceTrace' in tests and 'Assert.Equal("32", records[3].Substring(58, 2))' in tests
checks['Physical 106 record fixture test exists'] = 'I08_PhysicalField3_RequiresNumericNonZeroOriginator' in tests and 'Assert.Equal(106, physicalAddenda.Length)' in tests
checks['Physical evidence precedence negative test exists'] = 'I08_R74_RefusesLocalReferenceWhenExistingPhysicalField3IsEmpty' in tests
checks['Physical valid test exists'] = 'I08_R74_AcceptsOriginalType05WithValidPhysicalField3' in tests
checks['Monetary credit negative test exists'] = 'I08_R74_RequiresMonetaryCreditEvenWithPaymentReference' in tests
checks['Return99 false positive test exists'] = 'I08_R74_RejectsReturn99AsEvidenceOfOriginalPayment' in tests
checks['F05 guard updated for shared source'] = "h=(p/'CenitR74OriginalCreditEvidence.cs').read_text()" in manual
for i,(label,passed) in enumerate(checks.items(),1):
    print(f'{i:02d} {"PASS" if passed else "FAIL"}: {label}')
print(f'I08 STATIC: {sum(checks.values())}/{len(checks)}. xUnit, build, relational DB and bank operator NOT EXECUTED.')
sys.exit(0 if all(checks.values()) else 1)
