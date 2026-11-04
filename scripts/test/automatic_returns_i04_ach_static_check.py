#!/usr/bin/env python3
"""Revisión estática específica I04-ACH. No sustituye .NET/Angular ni homologación."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
files = {
    "interface": "src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchColombiaReturnReviewService.cs",
    "models": "src/Cfa.ACHInterbank.Application/ACH/Models/AchColombiaReturnReviewModels.cs",
    "service": "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs",
    "controller": "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaReturnReviewController.cs",
    "entity": "src/Cfa.ACHInterbank.Persistence/Configuration/AchColombiaReturnPreparationConfiguration.cs",
    "snapshot": "src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs",
    "di": "src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs",
    "spa": "web/ach-interbank-ui/src/app/features/transactions/components/ach-colombia-return-review/ach-colombia-return-review.component.ts",
    "spahtml": "web/ach-interbank-ui/src/app/features/transactions/components/ach-colombia-return-review/ach-colombia-return-review.component.html",
    "spaapi": "web/ach-interbank-ui/src/app/features/transactions/services/ach-colombia-return-review-api.service.ts",
    "parent": "web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.html",
    "backtest": "tests/Cfa.ACHInterbank.Tests/AchColombiaReturnReviewI04Tests.cs",
    "fronttest": "web/ach-interbank-ui/src/app/features/transactions/components/ach-colombia-return-review/ach-colombia-return-review.component.spec.ts"
}
s = {key: (root / path).read_text(encoding="utf-8-sig") for key,path in files.items()}
checks = [
    ("interfaces propuestas disponibles", all((root / f).is_file() for f in files.values())),
    ("rutas solo ACH Colombia", '/api/ach-colombia/' not in s['controller'] and 'api/ach-colombia/devoluciones-automaticas/candidatas' in s['controller']),
    ("GET con permiso lectura", bool(re.search(r'HttpGet.*?Authorize\(Policy = P0Policies.ReturnsRead\)',s['controller'],re.S))),
    ("POST con permiso generación", bool(re.search(r'HttpPost.*?Authorize\(Policy = P0Policies.ReturnsGenerateFile\)',s['controller'],re.S))),
    ("decisiones restringidas", '"APROBAR" or "DESCARTAR"' in s['controller'] and '"APROBAR" or "DESCARTAR"' in s['service']),
    ("no exposición de cuentas en DTO", 'SourceAccount' not in s['models'] and 'DestinationAccount' not in s['models']),
    ("consulta limitada", 'Math.Clamp(limit, 1, 100)' in s['service']),
    ("consulta filtra cámara", 'RegulatoryCycleScheduleCatalog.AchColombiaCode' in s['service']),
    ("configuración activa para aprobar", 'AutomaticReturnMode' in s['service']),
    ("verifica generación manual", 'AchReturnsGenerated' in s['service']),
    ("verifica fuente I02", 'AchColombiaApplicationFailureDetector.DetectedEvent' in s['service']),
    ("verifica JSON de origen", 'CORE_FUNCIONAL' in s['service']),
    ("verifica causal catálogo ACH", 'AchReturnCode' in s['service'] and 'ClearingHouseId == houseId' in s['service']),
    ("verifica causal de devolución", 'ReturnReasons' in s['service'] and 'IsForReturn' in s['service']),
    ("revalida cuatro ciclos", 'AchColombiaReturnPreparationWindow.Evaluate' in s['service']),
    ("rechaza ciclo distinto", 'current.Value.CycleId != candidate.SuggestedReturnCycleId' in s['service']),
    ("crédito tardío no aprobable", 'LateCreditReview' in s['service']),
    ("R10 sin base adicional bloqueado", 'candidate.ReasonCode == "R10"' in s['service']),
    ("estado aprobada sin envío", 'APROBADA_SIN_ENVIO' in s['service']),
    ("estado descartada", 'DESCARTADA' in s['service']),
    ("sin generador NACHA", all(x not in s['service'] for x in ['GenerateReturnsFileAsync(', 'SendAsync(', 'TransmitAsync('])),
    ("sin transición financiera", 'AchTransferStateEnum.' not in s['service']),
    ("guarda auditada", 'context.SaveChangesAsync(ct)' in s['service']),
    ("concurrencia optimista EF", 'IsConcurrencyToken()' in s['entity'] and 'DbUpdateConcurrencyException' in s['service']),
    ("snapshot actualizado", 'IsConcurrencyToken()' in s['snapshot'][s['snapshot'].index('modelBuilder.Entity("Cfa.ACHInterbank.Domain.Models.ACH.AchColombiaReturnPreparation"'):][:2100]),
    ("servicio inyectado", 'AddScoped<IAchColombiaReturnReviewService, AchColombiaReturnReviewService>()' in s['di']),
    ("Angular Material", 'MatCardModule' in s['spa'] and 'MatDialogModule' in s['spa']),
    ("confirmación por operador", 'afterClosed()' in s['spa'] and 'accepted' in s['spa']),
    ("permiso en SPA", 'Returns.GenerateFile' in s['spa']),
    ("error de concurrencia usuario", 'err.status === 409' in s['spa']),
    ("SPA no invoca creación archivo", 'generateFile' not in s['spa']),
    ("integración pantalla existente", 'app-ach-colombia-return-review' in s['parent']),
    ("endpoints SPA cámara específica", 'api/ach-colombia/devoluciones-automaticas/candidatas' in s['spaapi']),
    ("casos xUnit incorporados", s['backtest'].count('[Fact]') >= 6),
    ("casos Angular incorporados", s['fronttest'].count("it('") >= 4),
]
for name, ok in checks: print(f"{'PASS_STATIC' if ok else 'FAIL'} | {name}")
print(f"RESULTADO {sum(x for _,x in checks)}/{len(checks)}")
raise SystemExit(0 if all(x for _,x in checks) else 1)
