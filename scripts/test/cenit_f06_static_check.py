from pathlib import Path
import sys
base=Path(__file__).resolve().parents[2]
p=base/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
physical=(p/'NachaPhysicalParser.cs').read_text()
validation=(p/'NachaInboundValidation.cs').read_text()
parser=(p/'NachaParserService.cs').read_text()
physical_test=(base/'tests/Cfa.ACHInterbank.Tests/NachaPhysicalParserTests.cs').read_text()
validation_test=(base/'tests/Cfa.ACHInterbank.Tests/NachaInboundValidationSeparationTests.cs').read_text()
sta=(p/'Seeders/RegulatoryCatalogSeeder.cs').read_text()
checks=[
('CENIT physical parser profile-scoped', 'isCenitNachaProfile = false' in physical and 'IsCenitNachaProfileCode(profileReader?.ProfileCode)' in physical),
('CENIT enyes blocked without ACH impact','(!isCenitNachaProfile && (value is 0xD1 or 0xF1))' in physical),
('CENIT physical official ERR_INV_PATTERN','CENIT-NACHA-FATAL ERR_INV_PATTERN' in physical),
('ACH repertoire message remains','ACHCOL-PHYSICAL-ASCII-REPERTOIRE' in physical),
('CENIT physical framing classified','CENIT-NACHA-FATAL-LINE-ENDINGS' in physical and 'CENIT-NACHA-FATAL-T1' in physical),
('CENIT returns selected','CenitReturnIn2026Layout.IsProfile(profileCode)' in validation),
('CENIT ROR selected','CenitReturnOfReturn2026Layout.IsProfile(profileCode)' in validation),
('CENIT ordinary selected','CenitOrdinaryInbound2026Layout.IsProfile(profileCode)' in validation),
('published CENIT authority','publishedAuthority?.IsCenit == true' in validation),
('validation context carries chamber authority','IsCenitNachaProfile);' in validation),
('validator switches error domain','SelectNachaErrorDomain(bool isCenitNachaProfile)' in validation),
('STA catalog not dereferenced in CENIT branch',validation.index('if (_isCenitNachaProfile)') < validation.index('if (_rejectionCatalog.TryGetValue(dxxCode')),
('CENIT emitted cause not STA','return $"CENIT-NACHA-FATAL: {fallbackMessage}";' in validation),
('parser configures before persistence and physical parse', parser.index('SelectNachaErrorDomain(validationContext.IsCenitNachaProfile)') < parser.index('new NachaInboundDatabaseValidator') < parser.index('NachaPhysicalParser.ParseAsync(nachaStream, ct, profileReader, validationContext.IsCenitNachaProfile)')),
('physical utility recognizes reader','NachaInboundValidator.IsCenitNachaProfileCode(profileReader?.ProfileCode)' in physical),
('CENIT invalid repertoire tested','ParseAsync_CenitPublishedProfile_ShouldRejectUnsupportedRepertoire' in physical_test and '[InlineData(0xD1)]' in physical_test and '[InlineData(0xF1)]' in physical_test),
('CENIT supported chars tested','ParseAsync_CenitPublishedProfile_ShouldAcceptDocumentedCharacters' in physical_test),
('ACH existing Ñ/ñ test preserved','ParseAsync_NormativeLatin1Enye_ShouldRemainAccepted' in physical_test),
('CENIT framing failure tested','ParseAsync_CenitPhysicalFatal_ShouldNeverUseAchColombiaErrorCode' in physical_test),
('catalog isolation test covers D01,D02,D04,D05,D06',all(f'[InlineData("{x}")]' in validation_test for x in ['D01','D02','D04','D05','D06'])),
('duplicate-entry path tested','CenitDuplicateEntryRule_ShouldNotReportStaDuplicateFileCode' in validation_test),
('formal entry failure remains scoped','CenitTransactionFormalFailure_ShouldRemainEntryScopedAndWithoutStaCodes' in validation_test),
('ACH catalog behavior tested','AchColombiaNacha_ShouldRetainLegacyCodeLookup' in validation_test),
('STA seeder original available', all(f'Code = "{x}"' in sta for x in ['D01','D02','D03','D04','D05','D06'])),
]
for n,(label,ok) in enumerate(checks,1):print(f'{n:02} {"PASS" if ok else "FAIL"} {label}')
print(f'RESULT: {sum(x[1] for x in checks)}/{len(checks)} PASS')
sys.exit(0 if all(x[1] for x in checks) else 1)
