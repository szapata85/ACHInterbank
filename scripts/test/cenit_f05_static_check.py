#!/usr/bin/env python3
"""F05 static guards: source inspections, NOT C# compilation or integration proof."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
s=(p/'CenitReturnOfReturnService.cs').read_text()
g=(p/'AchReturnOfReturnFileGenerationService.cs').read_text()
c=(p/'Seeders/RegulatoryCatalogSeeder.cs').read_text()
t=(root/'tests/Cfa.ACHInterbank.Tests/CenitReturnOfReturn2026Tests.cs').read_text()
u=(root/'tests/Cfa.ACHInterbank.Tests/AchReturnOfReturnFileGenerationServiceTests.cs').read_text()
v=(root/'tests/Cfa.ACHInterbank.Tests/RegulatoryCatalogSeederReturnOfReturnPoliciesByClearingHouseTests.cs').read_text()
checks={
 'C1 source Return id recorded on flow':'SourceReturnTransactionId = sourceReturnId' in s,
 'C1 source Return resolved by raw trace':'x.TraceNumber == parentReturnTrace' in s,
 'C1 source Return amount checked':'x.Amount == sourceArtifact.EntryDetail.Amount' in s,
 'C1 source Return cycle checked':'x.AchCycleId == parentCycleId' in s,
 'C1 no ambiguous duplicates':'CENIT_ROR_SOURCE_RETURN_AMBIGUOUS' in s,
 'C1 registration preserves raw-only correlation':'CENIT_ROR_SOURCE_RETURN_RAW_INVALID' in s and 'SourceReturnTransactionId = sourceReturnId' in s,
 'C1 no Original fallback':'flow.SourceReturnTransaction ?? flow.OriginalTransaction' not in g,
 'C1 generator validates original raw without synthetic Return':'CENIT_ROR_SOURCE_RETURN_RAW_NOT_FOUND' in g and 'flow.SourceReturnTransaction is not null' in g,
 'C1 generator verifies parent link':'parentEvent.AchTransactionId != flow.OriginalTransactionId' in g,
 'C1 generator validates raw trace':'flow.SourceReturnTransaction.TraceNumber, sourceArtifact.EntryDetail.SequenceNumber?.Trim()' in g,
 'C1 generator validates raw amount':'flow.SourceReturnTransaction.Amount != sourceArtifact.EntryDetail.Amount' in g,
 'C1 calendar rule maintained':'GetPreviousBusinessDayAsync' in g,
 'R74 catalog only on R32':'originalCause == "R32"' in c and 'Where(code => code != "R74")' in c,
 'R74 parent validation outgoing/incoming':'CENIT_R74_R32_PARENT_REQUIRED' in s,
 'R74 monetary credit validation':'original.Type != TransactionTypeEnum.Credit || original.IsPrenotification || original.Amount <= 0' in s,
 'R74 adenda return code 99 never counted':'x.AddendaRecord.CodeTypeAddendumRecord == "05"' in s and 'x.AddendaRecord.CodeTypeAddendumRecord == "05"' in g,
 'R74 requires field3 information':'HasMeaningfulPaymentInformation(x.AddendaRecord!.IdUserOrig)' in s,
 'R74 zero-only fields do not qualify':"value.Any(c => c != '0' && c != ' ')" in s,
 'R74 rerun on productive file generator':'CENIT_R74_ORIGINAL_ADDENDA_REQUIRED' in g,
 'R74 parent and addenda tests':'F05_R74_MustHaveR32Parent' in t and 'F05_R74_RequiresMonetaryCreditAndOriginalPaymentInformation' in t,
 'C1 persisted source test':'F05_Cycle1_CorrelatesPersistedSourceReturnBeforeCreatingFlow' in t,
 'C1 generator raw fallback/negative tests':'GenerateNachaAsync_CenitCycle1_UsesVerifiedRawWhenNoSeparateReturnRowExists' in u and 'GenerateNachaAsync_CenitCycle1_RejectsMismatchedReturnTrace' in u and 'GenerateNachaAsync_CenitCycle1_RejectsMissingParentRawLink' in u,
 'C1 seeded test fixture has original/source distinct':'SourceReturnTransactionId = sourceReturn.Id' in u and 'OriginalTransactionId = original.Id' in u,
 'Catalog tests revised':'policy.OriginalReturnCode == "R32" ? expected' in v,
 'ACH Colombia policy unchanged':'foreach (var rule in baseRules)' in c and 'ClearingHouseId = clearingHouseIds.AchColombiaId' in c,
 'CUD remains out of scope':all(token not in (s+g+c).lower() for token in ['cudbalance','cudapi','cudtransfer']),
}
for label,pass_ in checks.items():print(('PASS' if pass_ else 'FAIL')+' '+label)
print(f'TOTAL {sum(checks.values())}/{len(checks)}')
raise SystemExit(0 if all(checks.values()) else 1)
