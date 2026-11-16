"""I10 source-structure assertions. NOT a substitute for compiling or running C# tests."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[2]
impl = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
parser = (impl/'NachaPhysicalParser.cs').read_text()
writer = (impl/'NachaFileGenerationCoordinator.cs').read_text()
tests = (root/'tests/Cfa.ACHInterbank.Tests/CenitI10PhysicalAlphabetTests.cs').read_text()
legacy = (root/'tests/Cfa.ACHInterbank.Tests/NachaPhysicalParserTests.cs').read_text()
record = (root/'tests/Cfa.ACHInterbank.Tests/TestData/Nacha/NormativeSynthetic/Cenit/Ror/CENIT_ROR_IN_001.ach').read_bytes()
repertoire = b' 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,;:-*/&#$%='
checks = [
 ('CENIT profile select keeps explicit and known code routes', 'isCenitNachaProfile |= NachaInboundValidator.IsCenitNachaProfileCode(profileReader?.ProfileCode)' in parser),
 ('CENIT input rejects unapproved bytes before decode', parser.index('!IsAcceptedByte(value, isCenitNachaProfile)') < parser.index('Encoding.Latin1.GetString(bytes)')),
 ('CENIT input rejects CR/LF', "(byte)'\\r' or (byte)'\\n'" in parser),
 ('CENIT input rejects BOM', 'Encoding.UTF8.Preamble' in parser),
 ('CENIT input keeps own fatal reason', 'CENIT-NACHA-FATAL ERR_INV_PATTERN' in parser),
 ('ACH input preserves legacy Ñ/ñ', '(!isCenitNachaProfile && (value is 0xD1 or 0xF1))' in parser),
 ('source has uppercase A-Z range', "value is >= (byte)'A' and <= (byte)'Z'" in parser),
 ('source has lowercase a-z range', "value is >= (byte)'a' and <= (byte)'z'" in parser),
 ('source has numeric 0-9 range', "value is >= (byte)'0' and <= (byte)'9'" in parser),
 ('source has ASCII-only outbound gate', 'value <= 0x7F && IsAcceptedByte((byte)value, isCenitNachaProfile: true)' in parser),
 ('source punctuation exact profile text', all("(byte)'" + chr(x) + "'" in parser for x in b'.,;:-*/&#$%=')),
 ('CENIT outbound guard isolated by chamber', 'string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)' in writer),
 ('CENIT outbound guard has safe error', 'CENIT_NACHA_CHARACTER_REPERTOIRE_INVALID' in writer),
 ('return-out validated before audit/hash', writer.index('ValidateCenitOutputAlphabet(clearingHouseCode, content)') < writer.index('audit.FileHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(content)))')),
 ('legacy export validated before audit', writer.index('ValidateCenitOutputAlphabet(audit.ClearingHouseCode, fileContent)') < writer.index('await PersistGenerationAuditAsync(audit, resolution.Profile?.Id, ct);')),
 ('official export validated before hash', writer.index('ValidateCenitOutputAlphabet(clearingHouseCode, fileContent)') < writer.index('audit.FileHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(fileContent)))')),
 ('CENIT generated output validates original Unicode codepoints', 'content.Any(character => !NachaPhysicalParser.IsCenitAcceptedCharacter(character))' in writer),
 ('same input byte matrix tested by xUnit source', 'for (var value = 0; value <= byte.MaxValue; value++)' in tests),
 ('ACH differential tests written', 'AchColombia_SamePhysicalFixture_RetainsLatin1Enye' in tests),
 ('CENIT generation guard negative cases written', 'CenitGeneratedContent_MustFailBeforeAsciiReplacementAndHash' in tests),
 ('CENIT valid alphabet generator cases written', 'GeneratedCenit_FullPublishedPunctuationAndCase_MustRemainUnchanged' in tests),
 ('CENIT golden case written', 'ExistingCenitRorGoldenFixture_MustPassPhysicalAlphabetAndLayout' in tests),
 ('ACH generation unchanged test written', 'AchColombiaGeneration_IsUnchangedByCenitSpecificGate' in tests),
 ('pre-existing ACH Ñ/ñ cases preserved', 'ParseAsync_NormativeLatin1Enye_ShouldRemainAccepted' in legacy),
 ('golden CENIT physical fixture 10x106 (line endings excluded)', len(b''.join(record.splitlines())) == 1060),
 ('golden CENIT repertoire matches published §4.1', set(record.strip(b'\r\n')).issubset(set(repertoire + b'\r\n'))),
 ('explicit binary alphabet size matches spec', len(set(repertoire)) == 26 + 26 + 10 + 12 + 1),
]
for idx, (name, passed) in enumerate(checks, 1):
 print(f'{idx:02} {"PASS" if passed else "FAIL"} {name}')
print(f'RESULT: {sum(ok for _, ok in checks)}/{len(checks)} PASS')
sys.exit(0 if all(ok for _, ok in checks) else 1)
