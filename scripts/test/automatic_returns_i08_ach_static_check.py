#!/usr/bin/env python3
"""I08-ACH: controles de fuente; NO equivalen a compilación ni pruebas xUnit."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[2]
service = (root / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs").read_text()
tests = (root / "tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI08Tests.cs").read_text()
tests06 = (root / "tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI06Tests.cs").read_text()
settings = (root / "src/Cfa.ACHInterbank.Api/appsettings.json").read_text()
runner = (root / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaAutomaticReturnFileRunner.cs").read_text()
checks = [
    ("comparación de contenido real", 'CryptographicOperations.FixedTimeEquals(rebuilt.Content, original.Content)' in service),
    ("comprobación previa a cambio de estado", service.index('await VerifyAutomaticArtifactAsync(candidate, generated, response, ct);') < service.index('candidate.Status = FileGenerated;')),
    ("solo rama automática", 'if (automatic)\n                await VerifyAutomaticArtifactAsync' in service),
    ("transacción envolvente", 'context.Database.BeginTransactionAsync(ct)' in service),
    ("reversión ante errores", 'await unitOfWork.RollbackAsync(CancellationToken.None)' in service),
    ("validación de disponibilidad del reconstructor", 'if (automatic && artifactService is null)' in service),
    ("cámara verificada", 'rebuilt.ClearingHouseId != candidate.OriginalTransaction.AchCycle.ClearingHouseId' in service),
    ("ciclo verificado", 'rebuilt.CycleId != candidate.SuggestedReturnCycleId' in service),
    ("archivo verificado", 'rebuilt.FileName != generated.FileName' in service),
    ("transacción verificada", 'rebuilt.TransactionIds[0] != candidate.OriginalTransactionId' in service),
    ("registros verificados", 'rebuilt.RecordCount != original.TotalRecords' in service),
    ("archivo vacío impedido", 'rebuilt.Content.Length == 0' in service),
    ("error controlado", 'Se detuvo la generación automática; requiere revisión operativa.' in service),
    ("xUnit discrepancia bytes", '[InlineData("bytes")]' in tests),
    ("xUnit cámara diferente", '[InlineData("camara")]' in tests),
    ("xUnit ciclo diferente", '[InlineData("ciclo")]' in tests),
    ("xUnit identificador diferente", '[InlineData("transaccion")]' in tests),
    ("xUnit registro diferente", '[InlineData("registros")]' in tests),
    ("xUnit servicio ausente", 'I08_AutomaticGenerationRequiresIntegrityService' in tests),
    ("xUnit fallo reconstrucción", 'I08_AutomaticGenerationStopsIfRebuildingFails' in tests),
    ("xUnit aislamiento CENIT", 'I08_CenitNeverCallsAutomaticReturnGeneratorOrRebuilder' in tests),
    ("fixtures I06 tienen bytes coherentes", 'CompatibleArtifact().Object' in tests06),
    ("habilitación permanece apagada", re.search(r'"EnableAutomaticReturnFileGeneration"\s*:\s*false', settings) is not None),
    ("ejecutor no transmite", 'DispatchAsync(' not in runner and 'transportAdapter' not in runner),
    ("sin nuevas rutas de API en incremento", 'Controller' not in tests),
]
for description, valid in checks:
    print(("PASS_STATIC" if valid else "FAIL_STATIC") + " | " + description)
print(f"RESULTADO: {sum(ok for _, ok in checks)}/{len(checks)}")
sys.exit(0 if all(ok for _, ok in checks) else 1)
