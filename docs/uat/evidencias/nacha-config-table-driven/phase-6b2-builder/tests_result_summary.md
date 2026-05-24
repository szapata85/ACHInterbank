# Fase 6B.2 - Resumen de pruebas

Comandos ejecutados:

```powershell
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Resultado:

- Build backend: OK.
- Tests backend: OK.
- Total: 1199.
- Passed: 1198.
- Failed: 0.
- Skipped: 1.

Pruebas nuevas principales:

- `OfficialNachaGeneration_ShouldUsePublishedAchProfile`.
- `OfficialNachaGeneration_ShouldUsePublishedCenitProfile`.
- `OfficialNachaGeneration_ShouldNotFallbackToLegacy_WhenProfileMissing`.
- `MissingRecord_ShouldReturn_NACHA_REQUIRED_RECORD_MISSING`.
- `MissingRequiredField_ShouldReturn_NACHA_REQUIRED_FIELD_MISSING`.
- `FieldSourceNotFound_ShouldReturn_NACHA_FIELD_SOURCE_NOT_FOUND`.
- `FieldExceedsLength_ShouldReturn_NACHA_FIELD_LENGTH_INVALID`.
- `CalculationFailure_ShouldReturn_NACHA_CALCULATION_FAILED`.
- `ChangingAchColombiaField_ShouldAffectOnlyAchColombiaFile`.
- `ChangingCenitField_ShouldAffectOnlyCenitFile`.

Productivo: **NO-GO**.
