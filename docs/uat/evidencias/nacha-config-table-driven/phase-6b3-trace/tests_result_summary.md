# Tests Result Summary — Phase 6B.3A

## Ejecución
```powershell
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "OfficialNachaGenerationTableDrivenTests"
```

**Resultado: 33 passed, 0 failed**

## Tests incluidos

### Core generation (6)
| Test | Status |
|------|--------|
| OfficialNachaGeneration_ShouldUsePublishedAchProfile | ✅ Passed |
| OfficialNachaGeneration_ShouldUsePublishedCenitProfile | ✅ Passed |
| OfficialNachaGeneration_ShouldNotFallbackToLegacy_WhenProfileMissing | ✅ Passed |
| OfficialGeneration_ShouldGenerateNonEmptyFile_ForAchColombia | ✅ Passed |
| OfficialGeneration_ShouldGenerateNonEmptyFile_ForCenit | ✅ Passed |
| MissingRecord_ShouldReturn_NACHA_REQUIRED_RECORD_MISSING | ✅ Passed |

### Error handling (4)
| Test | Status |
|------|--------|
| MissingRequiredField_ShouldReturn_NACHA_REQUIRED_FIELD_MISSING | ✅ Passed |
| FieldSourceNotFound_ShouldReturn_NACHA_FIELD_SOURCE_NOT_FOUND | ✅ Passed |
| FieldExceedsLength_ShouldReturn_NACHA_FIELD_LENGTH_INVALID | ✅ Passed |
| CalculationFailure_ShouldReturn_NACHA_CALCULATION_FAILED | ✅ Passed |

### Profile isolation (2)
| Test | Status |
|------|--------|
| ChangingAchColombiaField_ShouldAffectOnlyAchColombiaFile | ✅ Passed |
| ChangingCenitField_ShouldAffectOnlyCenitFile | ✅ Passed |

### Trace — Profile & ClearingHouse (2)
| Test | Status |
|------|--------|
| Trace_ShouldIncludeProfileInformation | ✅ Passed |
| Trace_ShouldIncludeClearingHouseInformation | ✅ Passed |

### Trace — Records & Fields (5)
| Test | Status |
|------|--------|
| Trace_ShouldIncludeRecords_1_5_6_7_8_9 | ✅ Passed |
| Trace_ShouldIncludeEveryRenderedField | ✅ Passed |
| Trace_ShouldLinkCfgLayoutField_ToRenderedValue | ✅ Passed |
| Trace_ShouldIncludePositionAndLength | ✅ Passed |
| Trace_ShouldIncludeSourceTypeAndSourceFieldPath | ✅ Passed |

### Trace — Calculations (4)
| Test | Status |
|------|--------|
| OfficialGeneration_ShouldEmitTrace_ForAchColombia | ✅ Passed |
| OfficialGeneration_ShouldEmitTrace_ForCenit | ✅ Passed |
| Trace_ShouldIncludeCalculatedFields | ✅ Passed |
| Trace_ShouldIncludeEntryHashCalculation | ✅ Passed |
| Trace_ShouldIncludeBlockCountCalculation | ✅ Passed |
| Trace_ShouldIncludeFileIdModifierCalculation | ✅ Passed |

### Trace — Legacy & errors (4)
| Test | Status |
|------|--------|
| Trace_ShouldMarkLegacyFallbackUsedFalse | ✅ Passed |
| Trace_ShouldCaptureFieldLengthError | ✅ Passed |
| Trace_ShouldCaptureMissingRequiredFieldError | ✅ Passed |
| Trace_ShouldNotContainSecrets | ✅ Passed |

### Trace — Reconstruction (2)
| Test | Status |
|------|--------|
| Trace_ShouldAllowReconstructingLineFromEntries | ✅ Passed |
| Trace_ShouldReferenceDifferentProfiles | ✅ Passed |

### Trace — Profile isolation (2)
| Test | Status |
|------|--------|
| AchFieldChange_ShouldAppearOnlyInAchTrace | ✅ Passed |
| CenitFieldChange_ShouldAppearOnlyInCenitTrace | ✅ Passed |

## Observaciones
- Todos los tests de generación exitosa emiten trace correctamente.
- Los tests de error (`FieldLengthError`, `MissingRequiredFieldError`) capturan trace `Status=Failed`.
- `LegacyFallbackUsed=false` en todos los casos table-driven.
- No se emiten secrets en el trace.
- El trace permite reconstruir la línea original a partir de los entries.
- ACH Colombia y CENIT referencian profiles distintos.
