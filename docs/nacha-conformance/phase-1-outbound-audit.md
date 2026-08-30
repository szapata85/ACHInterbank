# NACHA-CONFORMANCE-001 — Phase 1 Outbound

## 1. Audit Metadata

* Branch: `ACH-Interbank-Postgresql`
* HEAD: `cc9879c535de9f9c86db15aab889abad24a545b0`
* Date: 2026-08-29
* Scope: outbound NACHA-M generation only; ACH Colombia DDS-DIS-MAN-004 V35 (April 2026) and CENIT Manual de Especificaciones Formato NACHA-M (7 May 2026)
* Initial Git status: `?? docs/uat/certificados_pruebas/` (pre-existing, not touched)
* Product code changed: NO

## 2. Executive Verdict

`GAPS_FOUND`

ACH Colombia's V35 ordinary outbound path has direct profile, implementation, and focused-test evidence for the core 106-character physical contract, ordering, controls, credit/debit/prenotification families, and chamber isolation. Filename and sequence evidence remains incomplete where the retrieved V35 clauses did not establish the complete external contract.

CENIT ordinary outbound is not conformant to the 7 May 2026 manual. Its published ordinary profiles are explicitly placeholder (`NormativeVersion=NOT-DEMONSTRATED`), use materially different field positions/lengths, and are blocked in LIVE. CTX, CCD grouping limits, and the manual's filename/Type-1 identifier relationship are not represented correctly. Passing tests characterize placeholder behavior and do not prove CENIT conformance.

## 3. Architecture Located

`AchTransaction/AchBatch → AchCycle.ClearingHouse → INachaConfigResolver/CfgProfile → NachaFileBuilder → table-driven field renderer → NACHA-M bytes → ExternalFileNameBuilder`

* Entry path: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`, `BuildNachaFileAsync` → `BuildOfficialTableDrivenFileAsync`.
* Clearing-house/profile resolution: `BuildConfigResolutionRequest`, `ResolveOfficialRuntimeConfigAsync`; domain configuration in `CfgProfile`, `CfgProfileRecord`, `CfgLayoutVariant`, `CfgLayoutField`, and `CfgFieldRule`.
* Profiles: `NachaConfigOfficialProfilesSeeder`; ACH layouts in `AchColOfficialNachaLayout`; ordinary CENIT uses placeholder `BuildRecord1/5/6/7/8/9` layouts.
* Rendering: `NachaFileBuilder.RenderOfficialRecord`; fields are ordered by configured position and padded from configured justification/pad character.
* Type 1/5/6/8/9 source models: nested `FileHeaderRecord`, `BatchHeaderRecord`, `EntryDetailRecord`, `BatchControlRecord`, and `FileControlRecord`.
* Type 7: `NachaType7GenerationStrategy.BuildCandidates` and `BuildAddendasForTransaction`; layout selection occurs in `NachaFileBuilder`.
* Controls/filler: `NachaControlTotalsCalculator.Calculate`; padding is appended by `BuildOfficialTableDrivenFileAsync`.
* Naming/sequence: `ExternalFileNameBuilder.BuildAsync`, `ExternalFileNameSupport.BuildAchName/BuildCenitName`, `NachaFileNamingRuleSeeder`, and external filename sequence/reservation services.

## 4. Conformance Matrix

| Rule ID | Clearing House | Area | Normative Source | Implementation | Test Evidence | Table-Driven Status | Result |
|---|---|---|---|---|---|---|---|
| ACH35-OUT-001 | ACH Colombia | 106 chars/no line endings | V35 ch. 6, Type 1 size `106` | Profile total length; physical writer emits contiguous records | `AchColV35_ShouldRenderCriticalOffsetsAndPhysicalRules` | TABLE_DRIVEN | PASS |
| ACH35-OUT-002 | ACH Colombia | Type order | V35 ch. 6 sequence: 1, 5, 6, 7, 8, 9 | Generic writer interleaves 6/7 per batch | critical-offset and multi-batch tests | GENERIC_ENGINE | PASS |
| ACH35-OUT-003 | ACH Colombia | Type 1 | V35 ch. 6, credit/debit/prenote Type 1 tables | `FileHeaderRecord` + V35 configured fields/rules | exact offsets asserted | TABLE_DRIVEN | PASS |
| ACH35-OUT-004 | ACH Colombia | Type 5 | V35 ch. 6 Type 5 tables | `BatchHeaderRecord` + V35 profile | exact dates, DFI, ordinal asserted | TABLE_DRIVEN | PASS |
| ACH35-OUT-005 | ACH Colombia | Type 6 | V35 ch. 6 Type 6 tables | `EntryDetailRecord` + V35 profile | codes, amount, identity, name, trace asserted | TABLE_DRIVEN | PASS |
| ACH35-OUT-006 | ACH Colombia | Type 7/addenda | V35 ch. 6: addenda mandatory for originated monetary credit/debit, prenotes, returns | candidates + credit/debit/prenote variants | family and trace-suffix assertions | TABLE_DRIVEN | PASS |
| ACH35-OUT-007 | ACH Colombia | Type 8 | V35 ch. 6 batch counters/totals | calculator → `BatchControlRecord` → profile | rendered totals and exact offsets asserted | GENERIC_ENGINE | PASS |
| ACH35-OUT-008 | ACH Colombia | Type 9 | V35 ch. 6 file counters/totals | calculator → `FileControlRecord` → profile | rendered totals and exact offsets asserted | GENERIC_ENGINE | PASS |
| ACH35-OUT-009 | ACH Colombia | Padding/filler | V35 ch. 6: blocks of 10 and Type-9 filler | ceil(record count/10), append 106 `9`s | padding and block-count tests | GENERIC_ENGINE | PASS |
| ACH35-OUT-010 | ACH Colombia | Counts/hash/debit/credit | V35 Type 8/9 tables | source transactions/addendas feed calculator; hash source/length from layout | 17 control-total tests plus rendered-control tests | GENERIC_ENGINE | PASS |
| ACH35-OUT-011 | ACH Colombia | Credit/debit/prenote | V35 credit/debit technical sheets | separate original/prenote profiles and addenda variants | four supported ordinary families | TABLE_DRIVEN | PASS |
| ACH35-OUT-012 | ACH Colombia | Filename | V35 identifier clause retrieved; complete filename clause not reliably retrieved | `RRRRTTT.ZZZ.N`, seeded `.ach`; LIVE naming gate remains | filename-policy tests | TABLE_DRIVEN | DOCUMENTATION_EVIDENCE_INSUFFICIENT |
| ACH35-OUT-013 | ACH Colombia | File identifier/daily sequence | V35 Type 1 field 7 distinguishes same-date files | sequence-to-character map; filename reservation maps sequence to Type 1 | control-total and filename consistency tests | GENERIC_ENGINE | PARTIAL |
| ACH35-OUT-014 | ACH Colombia | Entry/addenda sequence | V35 ch. 6 and sequence reference; addenda suffix must match Type 6 | trace supplied from transaction; Type 7 association validated | suffix/ordinal/multi-batch tests | HARDCODED_NORMATIVE_RULE | PARTIAL |
| ACH35-OUT-015 | ACH Colombia | PPD/CCD/CTX applicability | V35 retrieval did not establish a CENIT-style PPD/CCD/CTX service contract | builder catalog resolves only PPD/CCD | no ACH CTX test | UNRESOLVED | DOCUMENTATION_EVIDENCE_INSUFFICIENT |
| ACH35-OUT-016 | ACH Colombia | Return/ROR construction | V35 ch. 6 return layouts; full semantics out of Phase 1 | separate ReturnOut V35 path; participant ACH ROR not supported by project decision | ReturnOut profile/physical tests | TABLE_DRIVEN | PARTIAL |
| CENIT-OUT-001 | CENIT | 106 chars/no line endings | Manual §3 and Annex 1: six types, 106 chars, no EOL | placeholder layouts total 106; generic writer | non-empty/modulo-106 test | GENERIC_ENGINE | PASS |
| CENIT-OUT-002 | CENIT | Type order | Manual §§3.1-3.2 | generic 1/5/(6/7)/8/9 writer | presence/order characterized | GENERIC_ENGINE | PASS |
| CENIT-OUT-003 | CENIT | Type 1 | Annex 1 §1.1: date at 24-31, identifier at 36, size at 37-39 | placeholder uses 6-digit date at 24-29 and shifts following fields | only non-empty/profile-use tests | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-004 | CENIT | Type 5 | Annex 1 §1.2: 8-digit dates, service-specific fields, DFI at 84-91 | placeholder uses NACHA 6-digit dates and fields ending at 94 plus filler | structural presence only | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-005 | CENIT | Type 6 | Annex 1 §§1.3-1.4: amount occupies 30-47; identity 48-62; trace 88-102 | placeholder amount is 10 chars at 30-39; identity/name/indicator are shifted | no May-2026 offset test | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-006 | CENIT | Type 7 PPD/CCD | Annex 1 §1.5: type 05, info 4-83, sequence 84-87, suffix 88-94 | placeholder positions appear compatible but rules/content are not May-2026 tagged | generic Type-7 tests only | GENERIC_ENGINE | PARTIAL |
| CENIT-OUT-007 | CENIT | Type 8 | Annex 1 §1.8: debit/credit totals are 18 chars at 21-38/39-56 | placeholder uses 12-char totals at 21-32/33-44 and shifted following fields | calculator tests use configured lengths | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-008 | CENIT | Type 9 | Annex 1 §1.9: 18-char totals at 32-49/50-67; reserved 68-106 | placeholder uses 12-char totals at 32-43/44-55; filler starts 56 | no May-2026 offset test | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-009 | CENIT | Padding/filler | Manual §3: complete 10-record blocks with filler | generic ceil/block/padding implementation | block/padding tests | GENERIC_ENGINE | PASS |
| CENIT-OUT-010 | CENIT | Counts/hash/debit/credit | Annex 1 §§1.8-1.9 | arithmetic sources are correct, but placeholder control field widths/positions are not | arithmetic tests; no official CENIT control golden | GENERIC_ENGINE | GAP |
| CENIT-OUT-011 | CENIT | PPD addenda | §§3.1 and Annex 1 §1.5; one addenda is mandatory for specified PPD cases | strategy creates or preserves one/more addendas; no CENIT-specific cardinality/content rules | generic candidate test | HARDCODED_NORMATIVE_RULE | PARTIAL |
| CENIT-OUT-012 | CENIT | CCD grouping/limits | §3.1: one detail per CCD batch; maximum 10,000 batches/file; specific credit grouping | writer permits multiple details per CCD batch and has no limit guard | no boundary/grouping test | UNRESOLVED | GAP |
| CENIT-OUT-013 | CENIT | CTX service | §§3.2 and Annex 1 §1.4 | `BatchHeaderRecord.From` rejects every SEC except PPD/CCD | no CTX generation test | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-014 | CENIT | CTX multi-addenda | §§3.2/5.2: 1-9,999 addendas for payment entries | storage/loop can enumerate many, but CTX cannot reach rendering and no 9,999/cardinality rules exist | none | UNRESOLVED | GAP |
| CENIT-OUT-015 | CENIT | CTX file separation | §3.2 and operational rules: CTX must be in an independent file from PPD/CCD | no service-isolation policy before generic file build | none | UNRESOLVED | GAP |
| CENIT-OUT-016 | CENIT | Filename | rejection guidance: `RRRRTTT.SSS.N` | generator emits `RRRRTTT.CCC.YYYYMMDD.S` | current-pattern test proves implemented behavior only | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-017 | CENIT | Filename ↔ Type 1 identifier | rejection guidance: next daily sequence must be in filename and Type 1 modifier | CENIT naming completes reservation with null modifier; builder derives modifier independently from header cycle/default | no linking test | UNRESOLVED | GAP |
| CENIT-OUT-018 | CENIT | Max files/day | §§3/Type 1: maximum 999/day | `BuildCenitName` accepts any positive suffix; seeded max is `int.MaxValue` | no 999/1000 boundary test | HARDCODED_NORMATIVE_RULE | GAP |
| CENIT-OUT-019 | CENIT | Effective profile/version | Manual date 7 May 2026 | ordinary profiles are published placeholders effective 1 Jan 2026, `NOT-DEMONSTRATED`, not homologated | seeder tests assert placeholder publication, not conformance | TABLE_DRIVEN | GAP |
| CENIT-OUT-020 | CENIT | Formal homologation | External operator certification | LIVE gate rejects `CENIT_NOT_HOMOLOGATED` | explicit LIVE fail-closed test | LEGITIMATE_ORCHESTRATION | EXTERNAL_VALIDATION_REQUIRED |
| ARCH-OUT-001 | Both | Profile isolation | Both manuals are separate authorities | separate profiles/layout rows; field mutation stays chamber-local | ACH/CENIT isolation tests | TABLE_DRIVEN | PASS |
| ARCH-OUT-002 | Both | Generic-writer boundary | Clearing-house differences must follow each manual | renderer/order/totals are generic, but SEC restriction, Type-7 association checks, settlement/date behavior, and CENIT placeholder snapshots remain in code | architecture/fail-closed tests | HARDCODED_NORMATIVE_RULE | PARTIAL |

## 5. P1 Physical Contract Results

ACH Colombia passes the representative physical contract: exact 106-byte records without CR/LF, sequence `1,5,6,7,8,9`, configured alignment and character validation, Type-9 filler to a multiple of ten, and Type-8/9 totals sourced from actual entries/addendas. The strongest automated evidence asserts exact offsets across all six record types and four ordinary credit/debit/prenotification families.

CENIT shares only the generic 106-byte envelope, order, and block-padding mechanics. The ordinary profile is a placeholder built from legacy 94-character field positions plus 12 filler characters. This directly conflicts with the May 2026 Annex 1 layouts for Types 1, 5, 6, 8, and 9. Type 7 PPD/CCD positions appear structurally aligned, but the required service-specific content and cardinality are not demonstrated.

## 6. PPD / CCD / CTX Results

* ACH Colombia V35 retrieved evidence defines credit, debit, prenotification, and return technical sheets, but did not safely establish the CENIT-style PPD/CCD/CTX taxonomy. Those named-service conclusions are therefore not inferred.
* CENIT PPD and CCD are explicit in §3.1. Generic addenda enumeration exists, but the ordinary profile is not tied to the 2026 rule set. CCD's one-detail-per-batch and 10,000-batch limit are unenforced.
* CENIT CTX is a confirmed GAP: the manual requires independent CTX files, a distinct Type-6 layout, and 1-9,999 addendas; the builder rejects SEC codes other than PPD/CCD before output.
* Return and return-of-return physical tests exist, but only representative construction evidence was used here; full semantics remain out of Phase 1.

## 7. File Identity and Sequence Results

ACH Colombia has durable per-scope filename reservation, daily reset, isolation, and a sequence-to-Type-1 identifier map. The exact V35 filename clause was not retrieved reliably enough to certify the seeded `.ach` convention, so filename compliance remains documentation-insufficient and the end-to-end relationship is partial.

CENIT is divergent against the specified manual. The manual states `RRRRTTT.SSS.N`, maximum 999 files/day, and requires the same next daily sequence in the filename and Type-1 identifier. Current code emits `RRRTTT.CCC.YYYYMMDD.S`, permits an unbounded positive suffix, and does not pass that suffix into Type 1.

Type-6 trace composition and Type-7 suffix association are represented and tested for ACH Colombia. CENIT Type-6 trace positions exist in the manual, but the ordinary placeholder Type-6 layout shifts the field and therefore cannot be certified.

## 8. Table-Driven Isolation Results

1. The outbound writer/renderer is generic for record ordering, configured field rendering, alignment, validation hooks, totals, and padding.
2. ACH Colombia differences are substantially supplied by V35 profiles, layout variants, field rules, direction/flow dimensions, and effective dates.
3. CENIT ordinary differences are not supplied by an authoritative May-2026 profile; they fall through to placeholder `BuildRecord1/5/6/7/8/9` snapshots.
4. Hard-coded code with compliance impact includes the PPD/CCD-only SEC check, ACH-specific Type-7 association branches, synthesized default addenda, and independently derived file identifier. The CENIT SEC restriction is a normative coupling because it prevents CTX.
5. ACH field-position changes can generally be made through profile data without changing the renderer. A conformant CENIT ordinary/CTX change cannot currently be completed without changing code because the generic build model rejects CTX and lacks separation/cardinality policy.
6. Effective dating exists, but the ordinary CENIT profiles are dated before the cited manual and explicitly tagged `NOT-DEMONSTRATED`.
7. Profile rows and mutation tests prevent direct cross-chamber field leakage. Shared hard-coded orchestration can still affect both; the CTX restriction demonstrates this risk.

## 9. Confirmed GAPs

### GAP candidate: NACHA-GAP-001

* Clearing house: CENIT
* Normative rule: Types 1, 5, 6, 8, and 9 must use the positions and lengths in Annex 1 of the 7 May 2026 manual.
* Source: Manual §§3, 4.1 and Annex 1 §§1.1-1.4, 1.8-1.9.
* Current behavior: ordinary CENIT profiles are published placeholders using legacy 94-character layouts plus 12 filler characters; examples include a 6-digit Type-1 date instead of 8, a 10-character Type-6 amount instead of 18, and 12-character Type-8/9 monetary totals instead of 18.
* Concrete evidence: `NachaConfigOfficialProfilesSeeder.BuildRecord1/5/6/8/9`; profile tags `IsPlaceholder=true`, `NormativeVersion=NOT-DEMONSTRATED`; no May-2026 exact-offset CENIT test.
* Impact: CENIT can reject or misinterpret ordinary outbound files; LIVE is deliberately blocked.
* Severity: CRITICAL
* Recommended next action: define and verify an effective-dated ordinary CENIT May-2026 profile family before considering gate removal.

### GAP candidate: NACHA-GAP-002

* Clearing house: CENIT
* Normative rule: CTX has a distinct detail layout, 1-9,999 addendas per payment entry, and must be sent separately from PPD/CCD.
* Source: Manual §§3.2, 5.2 and Annex 1 §1.4.
* Current behavior: `BatchHeaderRecord.From` accepts only `PPD` or `CCD`; no CTX file-isolation or 1-9,999 rule is enforced.
* Concrete evidence: `NachaFileBuilder.BatchHeaderRecord.From`, `ResolveStandardEntryClassCode`, and absence of CTX generation tests/profile.
* Impact: required CTX output cannot be produced, and mixed-service prevention is absent.
* Severity: HIGH
* Recommended next action: represent CTX service/layout/cardinality/separation in the profile/policy model and add focused conformance evidence.

### GAP candidate: NACHA-GAP-003

* Clearing house: CENIT
* Normative rule: CCD uses one detail per batch and no more than 10,000 batches per file, with specified grouping for applicable credit flows.
* Source: Manual §3.1 and Type-5 guidance.
* Current behavior: the generic builder accepts multiple transactions in a CCD batch and does not enforce the batch limit.
* Concrete evidence: `BuildOfficialTableDrivenFileAsync` groups all transactions by batch without SEC-specific cardinality checks; no boundary test exists.
* Impact: structurally valid-looking files may violate CENIT service grouping and limits.
* Severity: HIGH
* Recommended next action: add a CENIT effective policy for grouping/cardinality and validate it before rendering.

### GAP candidate: NACHA-GAP-004

* Clearing house: CENIT
* Normative rule: filename format is `RRRRTTT.SSS.N` in the cited manual.
* Source: Manual file-rejection guidance/table, 7 May 2026.
* Current behavior: `ExternalFileNameSupport.BuildCenitName` emits `RRRRTTT.CCC.YYYYMMDD.S`; the seeded rule cites a different document/pattern.
* Concrete evidence: `ExternalFileNameSupport.BuildCenitName`, `NachaFileNamingRuleSeeder.SeedAsync`, and `CenitBuilder_Uses_CurrentDocumentedPattern_WithoutAlternateNaming`.
* Impact: outbound filename can be rejected against the manual in audit scope.
* Severity: HIGH
* Recommended next action: reconcile the two cited CENIT authorities formally, then configure the governing effective-dated filename rule.

### GAP candidate: NACHA-GAP-005

* Clearing house: CENIT
* Normative rule: at most 999 files/day and the next daily sequence must appear in both filename and Type-1 file identifier.
* Source: Manual Type-1 identifier and rejection guidance.
* Current behavior: the CENIT suffix has no maximum (`int.MaxValue` policy); naming completes the reservation with a null file modifier, while the file builder independently uses `NachaHeader.CycleNumber` or `1`.
* Concrete evidence: `NachaFileNamingRuleSeeder`, `ExternalFileNameBuilder.BuildAsync`, `NachaControlTotalsCalculator.ResolveFileIdModifier`, and absence of a CENIT link/boundary test.
* Impact: duplicate/rejected files and broken filename/header identity coherence.
* Severity: HIGH
* Recommended next action: use one reserved daily sequence as the source for both artifacts and enforce the normative bound.

### GAP candidate: NACHA-GAP-006

* Clearing house: CENIT
* Normative rule: current output must be driven by the 7 May 2026 specification.
* Source: Manual title/date and Annex 1.
* Current behavior: ordinary profiles are published effective 1 January 2026 but explicitly `NOT-DEMONSTRATED`, placeholder, and non-homologated; LIVE generation fails closed.
* Concrete evidence: `NachaConfigOfficialProfilesSeeder.SeedAsync`, `NachaConfigSeeds_ShouldCreatePublishedCenitProfile`, and `CenitLive_ShouldRemainFailClosed_WhileAchColIsNotBlockedByGate`.
* Impact: there is no current authoritative ordinary CENIT outbound profile.
* Severity: CRITICAL
* Recommended next action: replace the placeholder status only after exact May-2026 profile evidence and focused tests exist; retain the LIVE gate until then.

## 10. Partial Evidence

* ACH filename and full filename-to-Type-1 identity contract: implementation and tests exist, but Local RAG did not retrieve the complete V35 filename clause safely.
* ACH trace/addenda sequencing: association and representative ordinals are tested; allocator boundary/exhaustion evidence is incomplete.
* ACH return/return-of-return: representative physical construction exists; full business semantics are deferred by scope.
* CENIT Type 7 PPD/CCD: positions appear compatible and generic generation exists, but service-specific required content/cardinality and May-2026 field-rule tests are absent.
* CENIT PPD exact transaction-per-file ceiling was fragmented in the indexed text; no numeric conclusion was inferred.
* Generic-writer isolation is demonstrated at the data-row level, while remaining hard-coded cross-field/orchestration behavior prevents a fully table-driven verdict.

## 11. External Validation

Internal conformance evidence cannot replace ACH Colombia certification or CENIT homologation. The existing `IsHomologated=false` tags and CENIT LIVE fail-closed gate are appropriate external-validation boundaries. No `VERIFIED`, `UAT_READY`, `GAUNTLET_PASSED`, or `RELEASE_READY` state is asserted.

## 12. Test Evidence

| Command | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~OfficialNachaGenerationTableDrivenTests` | 63 | 0 | 0 reported |
| `dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter FullyQualifiedName~NachaControlTotalsCalculatorTests` | 17 | 0 | 0 reported |
| `dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter FullyQualifiedName~NachaConfigOfficialProfilesSeederTests` | 33 | 0 | 0 reported |
| `dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter FullyQualifiedName~ExternalFileNamePolicyPhase1Tests` | 41 | 0 | 0 reported |
| `dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter FullyQualifiedName~NachaType7GenerationStrategyTests` | 2 | 0 | 0 reported |

Test failure classification: none. The green CENIT tests prove placeholder publication, generic physical length/order, isolation, and intentional LIVE blocking; they do not prove the May 2026 layouts or CTX contract.

## 13. Final Counts

* TOTAL RULES AUDITED: 38
* PASS: 15
* PARTIAL: 6
* GAP: 14
* NOT_APPLICABLE: 0
* EXTERNAL_VALIDATION_REQUIRED: 1
* DOCUMENTATION_EVIDENCE_INSUFFICIENT: 2

## 14. Phase 1 Decision

`Is Phase 1 sufficiently evidenced to proceed to Phase 2 — Inbound NACHA-M? NO`

Blockers:

* No authoritative May-2026 ordinary CENIT outbound profile exists; Types 1, 5, 6, 8, and 9 diverge.
* CENIT CTX layout, multi-addenda bounds, and independent-file policy are not implemented.
* CENIT CCD grouping/limit rules are not enforced.
* CENIT filename, daily bound, and filename-to-Type-1 identifier relationship diverge from the manual in audit scope.

## 15. CENIT-NACHA-OUT-001 Remediation Attempt

* JOB: `CENIT-NACHA-OUT-001`
* Base commit: `0b2de78544e4c8ff745c13228070b5670b115e8a`
* Date: 2026-08-29
* Status: `BLOCKED`
* Product code changed: NO
* Documentation blocker: `DOCUMENTATION_BLOCKER_FILE_IDENTIFIER_MAPPING`
* Normative evidence retrieved: CENIT Manual de Especificaciones Formato NACHA-M, 7 May 2026, §6.1 establishes filename `RRRRTTT.ZZZ.1`, daily consecutive `1` through `999`, correspondence with Type-1 field 7, and restart at `A` every 36 files.
* Missing normative fact: the indexed source omits the correspondence table introduced by “así:”; therefore the complete symbol order and transition mapping from `ZZZ` to the one-character Type-1 identifier cannot be established without inference.
* Implementation decision: no production or test changes were made because the JOB explicitly prohibits inventing this mapping and requires implementation to stop when it is unavailable.
* GAP status: `NACHA-GAP-001 OPEN`; `NACHA-GAP-004 OPEN`; `NACHA-GAP-005 BLOCKED`; `NACHA-GAP-006 OPEN`.
* Residual Phase-1 blockers: `NACHA-GAP-002`, `NACHA-GAP-003`, and the four gaps above remain open; Phase 2 readiness remains `NO`.
