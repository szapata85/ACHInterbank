# State memory — TPA-GOV-001

```yaml
program: TPA-GOV-001
program_status: IN_PROGRESS
baseline_package: ACHInterbank-R10-NACHA-CONFIG-UX-FINAL
baseline_date: 2026-10-11
baseline_git_sha: UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA
active_phase: TPA9B4
active_phase_status: CLOSED_DOCUMENTATION_READY_RUNTIME_DEFERRED_ENVIRONMENT
phase_9_status: CLOSED
next_allowed_phase: TPA10
phase_7a_source_package: ACHInterbank-R12-TPA6-ADMIN-UX.zip
phase_7b_source_package: ACHInterbank-R13-TPA7A-TRANSACTION-ELIGIBILITY.zip
phase_7c_source_package: ACHInterbank-R14-TPA7B-CANONICAL-SELECTOR.zip
phase_8a_source_package: ACHInterbank-R15-TPA7C-PARITY-CLOSURE.zip
phase_8b_source_package: ACHInterbank-R16-TPA8A-SAFE-BACKFILL.zip
phase_8c_source_package: ACHInterbank-R17-TPA8B-CANONICAL-READ-CUTOVER.zip
phase_9a1_source_package: ACHInterbank-R18-TPA8C-COHERENCE-CLOSURE.zip
phase_9a2_1_source_package: ACHInterbank-R19-TPA9A1-SINGLE-HARDENING.zip
phase_9a2_2_source_package: ACHInterbank-R20-TPA9A2-1-BULK-ATOMICITY-HARDENING.zip
phase_9a2_3_source_package: ACHInterbank-R21-TPA9A2-2-BULK-CONCURRENCY-HARDENING.zip
phase_9a3_1_source_package: ACHInterbank-R22-TPA9A2-3-CUSTOMER-THIRD-PARTY-CONCURRENCY-HARDENING.zip
phase_9a3_2_source_package: ACHInterbank-R23-TPA9A3-1-VALIDATION-LIFECYCLE-HARDENING.zip
phase_9a4_source_package: ACHInterbank-R24-TPA9A3-2-ELIGIBILITY-CONTEXT-HARDENING.zip
phase_9b1_source_package: ACHInterbank-R25-TPA9A4-BACKFILL-READ-CUTOVER-HARDENING.zip
phase_9b2_1_source_package: ACHInterbank-R26-TPA9B1-SINGLE-BULK-OBSERVABILITY-HARDENING.zip
phase_9b3_1_source_package: ACHInterbank-R29-TPA9B2-3-BACKFILL-QUERY-RESUME-EFFICIENCY-HARDENING.zip
phase_9b3_2_source_package: ACHInterbank-R30-TPA9B3-1-ADMIN-SECURITY-HARDENING.zip
phase_9b3_3_source_package: ACHInterbank-R31-TPA9B3-2-REVALIDATION-PRENOTIFICATION-SECURITY-HARDENING.zip
phase_9b3_4_source_package: ACHInterbank-R32-TPA9B3-3-BACKFILL-SECURITY-HARDENING.zip
phase_9b4_source_package: ACHInterbank-R33-TPA9B3-4-SELECTOR-CANONICAL-READ-SECURITY-HARDENING.zip

design_revision:
  id: TPA-DESIGN-R2
  date: 2026-10-11
  reason: Onboarding must be generic and independent of clearing house; clearing-house context belongs to validation and eligibility.

invariants:
  - id: INV-TPA-001
    text: Silent onboarding from single and bulk transaction creation remains supported and must not be replaced by mandatory manual preregistration.
  - id: INV-TPA-002
    text: Third-party/account onboarding is clearing-house agnostic; ThirdParty and ThirdPartyAccount identity never depend on ACH Colombia, CENIT or transaction nature.
  - id: INV-TPA-003
    text: Single, bulk and manual master-data creation converge on the same onboarding service.
  - id: INV-TPA-004
    text: No human API/UI action may directly set an interbank prenotification result to Validated or Rejected.
  - id: INV-TPA-005
    text: ACH Colombia and CENIT rules are resolved separately by effective policy only in eligibility/validation.
  - id: INV-TPA-006
    text: Mandatory debit prenotification/authorization fails closed; optional credit prenotification is not converted into a regulatory requirement.
  - id: INV-TPA-007
    text: Validation attempt history is append-only.
  - id: INV-TPA-008
    text: Silent onboarding performs no external network I/O.
  - id: INV-TPA-009
    text: PostgreSQL and SQL Server remain supported with EF Code First migrations.
  - id: INV-TPA-010
    text: CustomerThirdParty remains available for compatibility and rollback through TPA8 closure; TPA8 does not remove legacy storage or code.
  - id: INV-TPA-011
    text: Eligibility is contextual and derived; a global account Active flag is not an authorization authority.

phase_0_status: CLOSED

phase_0_characterization:
  source_archive_matches_packaged_baseline: true
  clearing_house_coupling_in_master_onboarding: NOT_FOUND
  single_customer_account_onboarding_before_batch_resolution: true
  bulk_customer_account_onboarding_before_batch_resolution: true
  legacy_customer_third_party_creation_is_prenotification_gated: true
  legacy_customer_third_party_requires_source_customer_resolution: true
  automatic_result_resolution: true
  manual_interbank_decision_endpoint: false
  onboarding_network_io_found: false
  ach_colombia_debit_prenotification: MANDATORY_3_BUSINESS_DAYS_CONFIGURED
  ach_colombia_credit_prenotification: OPTIONAL_BUT_OPTIONAL_PRENOTE_3_DAY_LEAD_NOT_ENFORCED
  cenit_debit_prenotification: MANDATORY_NO_GLOBAL_LEAD_CONFIGURED
  cenit_credit_prenotification: OPTIONAL

open_gaps:
  - id: GAP-TPA-001
    text: Legacy CustomerThirdParty and CustomerAccount lack the canonical master split, AccountType, and multi-attempt validation history; global Pending/Active/Rejected mixes master and validation semantics.
  - id: GAP-TPA-008
    text: Legacy CustomerThirdParty onboarding is coupled to prenotification and to successful source-account-to-Customer resolution, even though no clearing-house coupling was found.
  - id: GAP-TPA-012
    text: No reusable at-rest protection pattern exists today for customer/third-party account numbers; TPA1 therefore preserves the current plain normalized-number posture instead of introducing unapproved cryptography. Protection/masking hardening remains follow-up work.
  - id: GAP-TPA-013
    text: Current transaction ingress does not expose recipient document type even though canonical ThirdParty identity requires it. TPA2 reuses the document type resolved on the legacy recipient Customer and skips canonical onboarding when recipient identity itself is insufficient; explicit ingress/backfill remains follow-up.
  - id: GAP-TPA-016
    text: The baseline seed had a single historical ACH Colombia transaction-rule row whose EffectiveFrom predates V35, while the supplied V35 authority identifies April 2026 but not an exact effective day. TPA3 corrects current metadata/behavior to V35 without inventing a historical split date; precise historical version backfill remains deferred.

investigated_gaps:
  - id: GAP-TPA-007
    status: CLOSED_NOT_FOUND
    text: No clearing-house dependency was found inside current Customer/CustomerAccount onboarding or PrenotificationHandler. Chamber resolution happens later in single/bulk orchestration.

closed_gaps:
  - id: GAP-TPA-009
    status: CLOSED_TPA9A2_3
    text: TPA9A1 closes the SINGLE legacy onboarding race, TPA9A2.2 closes BULK Customer/CustomerAccount concurrency while preserving canonical ThirdParty/ThirdPartyAccount winner re-read, and TPA9A2.3 closes the remaining compatibility CustomerThirdParty check-then-add race with an in-transaction savepoint flush, classified unique-conflict rollback, winner re-read, losing-entry detach and final-state preservation.
  - id: GAP-TPA-015
    status: CLOSED_TPA8A_SAFE_BACKFILL_IMPLEMENTED_RUNTIME_DEFERRED
    text: TPA8A adds an idempotent, dry-run capable, resumable legacy backfill that converges master data through the TPA1/TPA2 natural-key onboarding path and reconstructs validation history only from exact linked evidence; incomplete or ambiguous rows are reported without invented identity, account type, chamber, nature or policy history.
  - id: GAP-TPA-004
    status: CLOSED_TPA7B
    text: Transaction creation selector now queries canonical ThirdPartyAccount rows by Customer and destination institution, resolves chamber/nature/effective date/service type in backend, and consumes contextual EligibilityDecision; it no longer uses CustomerThirdParty global Active as selector authority.
  - id: GAP-TPA-014
    status: CLOSED_TPA7A
    text: SINGLE and BULK monetary creation now record LastMonetaryUseAtUtc on the exact contextual ThirdPartyAccountValidation that authorized the transaction, inside the same unit of work.
  - id: GAP-TPA-005
    status: CLOSED_TPA5
    text: Canonical ThirdParty/ThirdPartyAccount administrative command APIs now exist, use the same onboarding identity/concurrency path as silent onboarding, and keep interbank validation decisions separate.
  - id: GAP-TPA-002
    status: CLOSED_TPA3
    text: Prenotification maturity now resolves the effective chamber/nature policy instead of applying a global +3-business-day rule.
  - id: GAP-TPA-003
    status: CLOSED_TPA3
    text: Prenotification tacit acceptance now uses an explicit policy/cycle response deadline and never uses AchTransaction.SlaDeadlineAtUtc or a UTC-date fallback.
  - id: GAP-TPA-006
    status: CLOSED_TPA3_CURRENT_AUTHORITY
    text: Current ACH Colombia transaction-rule metadata and behavior now reference V35; exact historical V35 activation-day reconstruction remains GAP-TPA-016.
  - id: GAP-TPA-010
    status: CLOSED_TPA3_CANONICAL
    text: Canonical validation supports append-only multiple prenotification attempts and re-prenotification after accepted/rejected outcomes while legacy final CustomerThirdParty status remains compatibility-only.
  - id: GAP-TPA-011
    status: CLOSED_TPA3
    text: ACH Colombia credit remains optional, but when a prenotification exists its configured three-business-day lead is enforced by prerequisite and contextual eligibility policy.

characterization_tests_added:
  - PrenotificationHandlerTests.HandleAsync_CreatesPendingThirdParty_WhenSourceCustomerExistsAndIdentityIsNew
  - PrenotificationHandlerTests.HandleAsync_ReusesPendingThirdParty_AndAddsOnlyOnceAcrossRetry
  - PrenotificationHandlerTests.HandleAsync_DoesNothing_WhenSourceCustomerCannotBeResolved
  - AchBulkTransactionServiceTests.RegisterBulkAsync_Prenotification_OnboardsMissingCustomersAccountsAndThirdParty
  - AchBulkTransactionServiceTests.RegisterBulkAsync_Prenotification_ReusesExistingCustomersAccountsAndPendingThirdParty
  - TransactionsApiService.getActiveThirdParties_ShouldKeepLegacyGlobalActiveSelector

existing_characterization_reused:
  - CustomerThirdPartyGovernanceTests.Controller_DoesNotExposeManualStatusWriteEndpoint
  - CustomerThirdPartyGovernanceTests domain final-result/idempotency checks
  - DifferentialPrenotificationResponseProcessorTests automatic Active/Rejected and duplicate/correlation checks
  - TransactionPrerequisitePolicyServiceTests ACH Colombia debit/credit and CENIT debit/credit

tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: source-archive byte comparison for src, migrations, normativa and SPA production files
    result: PASS

tests_not_executed:
  - command: dotnet focal characterization tests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet command not found
  - command: Docker/dual-provider runtime evidence
    result: NOT_RUN_ENVIRONMENT
    reason: docker command not found
  - command: Angular focal characterization test
    result: NOT_RUN_ENVIRONMENT_DEPENDENCY_INSTALL_TIMEOUT
    reason: npm ci timed out and node_modules was not produced

files_touched:
  - docs/architecture/third-party-account-tpa0-baseline.md
  - docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md
  - tests/Cfa.ACHInterbank.Tests/PrenotificationHandlerTests.cs
  - tests/Cfa.ACHInterbank.Tests/AchBulkTransactionServiceTests.cs
  - web/ach-interbank-ui/src/app/features/transactions/services/transactions-api.service.spec.ts
  - MEMORY.md


phase_1_status: CLOSED

phase_1_master_data:
  scope: ThirdParty + ThirdPartyAccount only
  third_party_identity:
    - CustomerId
    - DocumentType
    - DocumentNumberNormalized
  third_party_account_identity:
    - ThirdPartyId
    - FinancialInstitutionId
    - AccountType
    - AccountNumberNormalized
  normalization:
    document_type: TRIM_UPPER_INVARIANT
    person_type: TRIM_UPPER_INVARIANT
    document_number: TRIM_UPPER_INVARIANT
    account_number: TRIM_ONLY
    display_name: TRIM_ONLY
  account_type_authority: existing AccountTypeEnum
  administrative_statuses:
    - Draft
    - Active
    - Inactive
  forbidden_master_dimensions_confirmed_absent:
    - ClearingHouseId
    - TransactionNature
    - ServiceType
    - Prenotification
    - Cycle
    - SLA/deadline
  customer_third_party_preserved: true
  onboarding_v2_implemented: false
  dual_write_implemented: false
  validation_schema_implemented: false
  eligibility_implemented: false
  spa_changed: false
  application_behavior_cutover: false
  migrations:
    postgres: 20261011173000_AddThirdPartyMasterData
    sql_server: 20261011173100_AddThirdPartyMasterData
  tests_added:
    - ThirdPartyMasterDataModelTests
    - ThirdPartyMasterDataMigrationTests
  documentation:
    - docs/architecture/third-party-account-tpa1-master-data.md

phase_1_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS

phase_1_tests_deferred:
  - command: dotnet focal TPA1 tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet command not found
  - command: PostgreSQL + SQL Server runtime migration execution
    result: NOT_RUN_ENVIRONMENT
    reason: docker command not found

phase_2_status: CLOSED

phase_2_silent_onboarding:
  service_contract: IThirdPartyOnboardingService
  implementation: ThirdPartyOnboardingService
  request_mapper: ThirdPartyOnboardingRequestMapper
  shared_by:
    - AchTransactionService
    - AchBulkTransactionService
  canonical_identity_source:
    customer_owner: existing Customer/CustomerAccount onboarding
    recipient_document_type: resolved legacy recipient Customer
    insufficient_recipient_identity: SKIP_CANONICAL_WITHOUT_SYNTHETIC_IDENTITY
  single_integration:
    database_transaction_starts_before_master_onboarding: true
    customer_ids_materialized_inside_same_transaction: true
    clearing_house_passed_to_onboarding: false
  bulk_integration:
    per_chunk_database_transaction: true
    uses_same_onboarding_service: true
    clearing_house_passed_to_onboarding: false
  idempotency:
    sequential_natural_key_reuse: true
    third_party_unique_conflict_reread: true
    third_party_account_unique_conflict_reread: true
    change_tracker_global_clear_in_onboarding: false
  legacy_compatibility:
    customer_third_party_preserved: true
    prenotification_handler_preserved: true
    read_cutover: false
    spa_changed: false
  safe_observability:
    structured_logs_exclude_document_name_account: true
    canonical_audit_payload_excludes_document_name_account: true
  chamber_validation_implemented: false
  eligibility_implemented: false
  new_prenotification_lifecycle_implemented: false
  migrations_added: false
  documentation:
    - docs/architecture/third-party-account-tpa2-onboarding.md
  tests_added:
    - ThirdPartyOnboardingServiceTests
    - AchTransactionNachaTests.RegisterTransactionAsync_SilentlyCreatesCanonicalThirdPartyAndAccount
    - AchTransactionNachaTests.RegisterTransactionAsync_Prenotification_KeepsLegacyThirdPartyAndCanonicalMasterInParallel
    - AchBulkTransactionServiceTests.RegisterBulkAsync_SilentlyCreatesCanonicalThirdPartyAndAccount_ThroughSharedOnboardingService

phase_2_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA2 static diff/migration/contract guard
    result: PASS

phase_2_tests_deferred:
  - command: dotnet focal TPA2 tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet command not found
  - command: PostgreSQL + SQL Server concurrency/runtime evidence
    result: NOT_RUN_ENVIRONMENT
    reason: docker command not found

phase_3_status: CLOSED

phase_3_contextual_validation:
  master_identity_unchanged: true
  validation_context:
    - ThirdPartyAccountId
    - ClearingHouseId
    - TransactionNature
    - ServiceType
    - ValidationType
    - EffectiveDate
    - PolicyRuleId
    - PolicyEffectiveFrom
  validation_statuses:
    - NotStarted
    - PendingSend
    - PendingResponse
    - Accepted
    - TacitlyAccepted
    - Rejected
    - RequiresCorrection
    - TechnicalReview
    - Expired
    - Suspended
  attempt_history: APPEND_ONLY
  automatic_resolution_states:
    Certified: Accepted
    AppliedTacitly: TacitlyAccepted
    ReturnedByOperator: Rejected
    ReturnedByEpr: Rejected
  eligibility_authority: ThirdPartyAccountEligibilityService
  eligibility_is_contextual: true
  customer_third_party_source_of_truth: false
  read_cutover: false
  spa_changed: false
  onboarding_changed: false
  policy_resolution:
    service: ClearingHouseValidationPolicyService
    effective_by_date: true
    ach_colombia_credit: OPTIONAL_PRENOTE_IF_USED_LEAD_3_BUSINESS_DAYS_RESPONSE_4_CYCLES
    ach_colombia_debit: MANDATORY_PRENOTE_LEAD_3_BUSINESS_DAYS_RESPONSE_4_CYCLES
    cenit_credit: OPTIONAL_PRENOTE_RESPONSE_SAME_OPERATIONAL_DAY
    cenit_debit: MANDATORY_PRENOTE_RESPONSE_SAME_OPERATIONAL_DAY_RETENTION_13_MONTHS_WHEN_LAST_USE_EVIDENCE_EXISTS
  tacit_acceptance:
    automatic: true
    response_deadline_separate_from_return_sla: true
    modes:
      - CycleCount
      - OperationalDayEnd
    sla_deadline_fallback_used_for_prenotification: false
  legacy_bridge:
    state_transition_service_updates_canonical_validation: true
    existing_customer_third_party_sync_preserved: true
    manual_interbank_result_endpoint_added: false
  migrations:
    postgres: 20261011184000_AddThirdPartyAccountValidationLifecycle
    sql_server: 20261011184100_AddThirdPartyAccountValidationLifecycle
  documentation:
    - docs/architecture/third-party-account-tpa3-validation.md
  tests_added:
    - ThirdPartyAccountValidationLifecycleTests
    - TransactionPrerequisitePolicyServiceTests.ValidateForNachaExportAsync_AchColombiaCreditWithPrenote_EnforcesConfiguredThreeBusinessDayLead

phase_3_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA3 static diff/schema/policy/migration guard
    result: PASS

phase_3_tests_deferred:
  - command: dotnet focal TPA3 tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet command not found
  - command: PostgreSQL + SQL Server migration/concurrency/runtime validation
    result: NOT_RUN_ENVIRONMENT
    reason: docker command not found


phase_4_status: CLOSED

phase_4_read_model_shadow:
  canonical_read_service: ThirdPartyAccountReadService
  canonical_read_contract: IThirdPartyAccountReadService
  canonical_read_endpoint:
    - GET /api/third-party-accounts
    - GET /api/third-party-accounts/{thirdPartyAccountId}
  authorization_policy: CanReadAch
  storage: QUERY_TIME_PROJECTION_NO_NEW_TABLE
  max_page_size: 100
  reads:
    - Customer
    - ThirdParty
    - ThirdPartyAccount
    - FinancialInstitution
    - AccountType
    - administrative_statuses
    - contextual_validations_by_clearing_house_nature_service
    - latest_attempt_per_validation
    - return_code_and_resolution
    - contextual_eligibility_when_explicitly_requested
    - safe_trace_ids
  privacy:
    account_number: MASKED_LAST4
    document_number: MASKED_LAST4
    excluded_attempt_fields:
      - ReturnDetail
      - EvidenceReference
      - CorrelationReference
      - SourceFileReference
  eligibility:
    authority: IThirdPartyAccountEligibilityService
    requires_explicit_context:
      - ClearingHouseId
      - TransactionNature
      - EffectiveDate
    service_type_optional: true
    partial_context_response: HTTP_400
    default_context_invented: false
  shadow:
    service_contract: IThirdPartyAccountShadowComparisonService
    implementation: ThirdPartyAccountShadowComparisonService
    adapter: CustomerThirdPartyAppService
    legacy_response_authority_preserved: true
    shadow_exception_changes_productive_response: false
    strong_link: PrenotificationValidationAttempt.AchTransactionId
    identity_fallback:
      - CustomerId
      - FinancialInstitutionId
      - AccountNumberNormalized
      - ThirdParty.DocumentNumberNormalized
    account_type_guessed_from_legacy: false
    service_type_fallback: CompanyEntryDescriptionCatalog.StandardEntryClassCode
    canonical_only_discovery: BOUNDED_SAFE_SCOPE_ONLY
    classifications:
      - MATCH
      - LEGACY_ONLY
      - CANONICAL_ONLY
      - STATUS_MISMATCH
      - ELIGIBILITY_MISMATCH
      - IDENTITY_INCOMPLETE
      - POLICY_CONTEXT_MISSING
  observability:
    metric: achinterbank.third_party_account.shadow_comparison
    metric_tags: [discrepancy]
    structured_logs: aggregate_counts_only
    pii_or_full_account_in_logs: false
  compatibility:
    customer_third_party_operational: true
    read_cutover: false
    spa_selector_cutover: false
    spa_functional_changes: false
    onboarding_single_bulk_changed: false
    manual_prenotification_decision_added: false
  migrations_added: false
  open_gaps_observed_not_closed:
    - GAP-TPA-004
    - GAP-TPA-013
    - GAP-TPA-014
    - GAP-TPA-015
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa4-read-model-shadow.md
  tests_added:
    - ThirdPartyAccountReadShadowTests
  existing_characterization_preserved:
    - TransactionsApiService.getActiveThirdParties_ShouldKeepLegacyGlobalActiveSelector

phase_4_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA4 static diff/no-migration/no-SPA-write-endpoint/privacy guard
    result: PASS

phase_4_tests_deferred:
  - command: dotnet focal TPA4 tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet command not found
  - command: PostgreSQL + SQL Server runtime evidence
    result: NOT_RUN_ENVIRONMENT
    reason: docker command not found


phase_5_status: CLOSED

phase_5_administration_api:
  canonical_controller: ThirdPartiesController
  route_root: /api/customers/{customerId}/third-parties
  read_policy: CanReadAch
  write_policy: CanManageAch
  master_creation:
    shared_identity_service: IThirdPartyOnboardingService
    explicit_third_party_create: EnsureThirdPartyAsync
    silent_single_bulk_preserved: true
    clearing_house_dimension_in_master_commands: false
    unique_conflict_re_read: true
  third_party_commands:
    - create
    - update_display_name
    - activate_deactivate
  third_party_identity_update:
    document_type_mutable: false
    document_number_mutable: false
    person_type_mutable: false
    strategy: EXPLICIT_REPLACEMENT_OR_CORRECTION_FLOW_REQUIRED
  account_commands:
    - create
    - update_administrative_status
    - activate_deactivate
  account_identity_update:
    financial_institution_mutable: false
    account_type_mutable: false
    account_number_mutable: false
    strategy: CREATE_REPLACEMENT_AND_DEACTIVATE_OLD
  validation_history:
    endpoint: GET nested account validation-history
    all_attempts: true
    excludes_sensitive_technical_fields: true
  revalidation:
    endpoint: POST nested account revalidations
    requires_context:
      - ClearingHouseId
      - TransactionNature
      - EffectiveDate
      - ServiceType
    policy_authority: IClearingHouseValidationPolicyService
    actual_route_guard: true
    actual_effective_date_guard: true
    not_applicable_policy_rejected: true
    idempotency_key_max_length: 160
    concurrent_loser_transaction_rolled_back: true
    new_attempt_append_only: true
    manual_result_decision_endpoint: false
  eligibility:
    account_inactive_blocks: true
    parent_third_party_inactive_blocks: true
    administrative_active_implies_interbank_eligible: false
  audit_privacy:
    ef_audit_scope_from_controller: true
    document_number_suppressed: true
    account_numbers_suppressed: true
    sensitive_names_suppressed: true
    validation_evidence_and_idempotency_suppressed: true
    new_structured_logs_pii_free: true
  compatibility:
    customer_third_party_operational: true
    legacy_productive_read_authority_preserved: true
    spa_selector_changed: false
    global_read_cutover: false
  migrations_added: false
  closed_gaps:
    - GAP-TPA-005
  open_gaps_observed_not_closed:
    - GAP-TPA-004
    - GAP-TPA-013
    - GAP-TPA-014
    - GAP-TPA-015
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa5-administration-api.md
  tests_added:
    - ThirdPartyAdministrationApiTests

phase_5_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA5 static phase guard
    result: PASS

phase_5_tests_deferred:
  - command: dotnet focal TPA5 tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet command not found
  - command: PostgreSQL + SQL Server runtime/concurrency validation
    result: NOT_RUN_ENVIRONMENT
    reason: docker command not found

phase_6_status: CLOSED

phase_6_administration_ux:
  customer_routes:
    - /customers/{customerId}/third-parties
    - /customers/{customerId}/third-parties/{thirdPartyId}
  read_policy: CanReadAch
  write_controls_permission: CanManageAch
  canonical_operations_use_tpa5_api: true
  third_party_master_form:
    clearing_house_field: false
    transaction_nature_field: false
    editable_identity_fields: false
    editable_administrative_fields:
      - DisplayName
  account_master_form:
    clearing_house_field: false
    transaction_nature_field: false
    bank_identity_editable_after_create: false
    identity_change_strategy: CREATE_REPLACEMENT_AND_DEACTIVATE_OLD
  administrative_status:
    explicitly_labeled_administrative: true
    presented_as_transfer_eligibility: false
  validation_history:
    clearing_house_visible: true
    transaction_nature_visible: true
    status_visible: true
    return_code_visible: true
    timestamps_visible: true
    safe_trace_identifiers_visible: true
    full_account_or_document_visible: false
  revalidation:
    clearing_house_only_in_validation_action: true
    transaction_nature_only_in_validation_action: true
    frontend_policy_calculation: false
    frontend_sla_or_cycle_calculation: false
    manual_result_decision_controls: false
    idempotency_key_generated_and_not_rendered: true
    source_account_display_masked: true
  compatibility:
    legacy_customer_third_parties_ux_unchanged: true
    transaction_selector_changed: false
    global_read_cutover: false
    silent_single_bulk_changed: false
  gap_tpa_004:
    administration_dependency_on_legacy_global_active_removed: true
    transaction_selector_dependency_on_legacy_global_active_removed: false
    status: REMAINS_OPEN_FOR_TPA7
  closed_gaps: []
  open_gaps_observed_not_closed:
    - GAP-TPA-004
    - GAP-TPA-013
    - GAP-TPA-014
    - GAP-TPA-015
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa6-administration-ux.md
  tests_added:
    - ThirdPartyAdministrationApiService Angular specs
    - CustomerThirdPartiesAdminComponent Angular specs
    - ThirdPartyAdminDetailComponent Angular specs
    - ThirdParty administration presentation Angular specs

phase_6_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA6 static phase guard
    result: PASS

phase_6_tests_deferred:
  - command: focal Angular tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: web/ach-interbank-ui/node_modules is absent in the supplied source archive; Node is available but project dependencies are not installed

phase_7a_status: CLOSED_IMPLEMENTED_TESTS_DEFERRED_ENVIRONMENT

phase_7a_transaction_integration:
  scope: BACKEND_ONLY
  source_package: ACHInterbank-R12-TPA6-ADMIN-UX.zip
  single:
    generic_onboarding_before_routing: true
    effective_policy_after_routing: true
    contextual_eligibility_after_policy: true
    prerequisite_before_transaction_persistence: true
    monetary_use_evidence_same_unit_of_work: true
  bulk:
    generic_onboarding_before_routing: true
    effective_policy_after_routing: true
    contextual_eligibility_after_policy: true
    prerequisite_before_transaction_persistence: true
    monetary_use_evidence_same_unit_of_work: true
    item_failure_semantics_preserved: true
  policy_semantics:
    mandatory_without_canonical_eligibility: FAIL_CLOSED
    optional_without_canonical_eligibility: ALLOWED_WITHOUT_INVENTING_PRENOTE_REQUIREMENT
    legacy_catalog_pre_routing_authoritative: false
    policy_version_mismatch: FAIL_CLOSED
    contextual_non_eligible_states: FAIL_BEFORE_TRANSACTION_PERSISTENCE
  compatibility:
    legacy_customer_third_party_removed: false
    spa_changed: false
    selector_cutover: false
    backfill: false
    schema_migration: false
  gaps_closed:
    - GAP-TPA-014
  gaps_remaining:
    - GAP-TPA-004
    - GAP-TPA-013
    - GAP-TPA-015
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa7-transaction-integration.md
  tests_added:
    - TransactionPrerequisitePolicyService registration fail-closed/optional/pending cases
    - AchTransactionNachaTests TPA7A mandatory debit/optional credit/monetary-use cases
    - AchBulkTransactionServiceTests TPA7A mandatory debit/optional credit/monetary-use cases
    - ThirdPartyAccountValidationLifecycleTests monetary-use evidence ownership

phase_7a_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA7A static phase guard
    result: PASS

phase_7a_tests_deferred:
  - command: dotnet test
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_7b_status: CLOSED_IMPLEMENTED_TESTS_DEFERRED_ENVIRONMENT

phase_7b_transaction_selector:
  scope: CANONICAL_CONTEXTUAL_SELECTOR_BACKEND_PLUS_SPA_CONSUMER
  source_package: ACHInterbank-R13-TPA7A-TRANSACTION-ELIGIBILITY.zip
  backend_query:
    customer_source: source account resolved through IAchCustomerRepository
    destination_institution: active FinancialInstitution requested by the transaction form
    routing_authority: IRoutingStrategyService used by BatchResolver
    resolved_context:
      - ClearingHouseId
      - ClearingHouseCode
      - cycle ProcessingDate as EffectiveDate
      - TransactionNature
      - CompanyEntryDescriptionCatalog.StandardEntryClassCode as ServiceType
    canonical_accounts: ThirdPartyAccount by ThirdParty.CustomerId plus FinancialInstitutionId
    eligibility_authority: IThirdPartyAccountEligibilityService
    returns_non_eligible_accounts_with_backend_reason: true
    selectable_decisions:
      - Allowed
      - AllowedWithoutPrenotification
    customer_third_party_status_consulted: false
    batch_created_by_selector_read: false
  spa:
    regulatory_logic: false
    policy_or_sla_calculation: false
    legacy_active_filter: false
    displays_backend_eligibility_code_and_message: true
    disables_backend_non_selectable_accounts: true
    final_authorization_authority: TPA7A server-side registration guard
  semantics:
    optional_credit_remains_optional: true
    mandatory_debit_final_fail_closed_preserved: true
    silent_onboarding_changed: false
  compatibility:
    customer_third_party_removed: false
    backfill: false
    global_cutover: false
    schema_migration: false
    prenotification_manual_account_entry_changed: false
  gaps_closed:
    - GAP-TPA-004
  gaps_remaining:
    - GAP-TPA-013
    - GAP-TPA-015
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa7-transaction-integration.md
  tests_added:
    - TransactionThirdPartyAccountSelectorTests ACH Colombia/CENIT routing and effective context
    - TransactionThirdPartyAccountSelectorTests credit/debit nature and optional-credit selection
    - TransactionThirdPartyAccountSelectorTests legacy Active does not override rejected canonical eligibility
    - TransactionsControllerTests contextual selector forwarding
    - TransactionsApiService contextual selector endpoint without legacy Active status
    - TransactionCreateComponent backend isSelectable/reason consumption without frontend eligibility rules

phase_7b_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA7B static phase guard
    result: PASS
  - command: TypeScript parser/static syntax guard over touched transaction SPA files
    result: PASS

phase_7b_tests_deferred:
  - command: dotnet focal tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: Angular focal tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: web/ach-interbank-ui/node_modules is absent in the supplied source archive

phase_7_status: CLOSED
phase_7c_status: CLOSED_PARITY_VERIFIED_TESTS_DEFERRED_ENVIRONMENT

phase_7c_parity_closure:
  scope: TPA7A_TPA7B_COHERENCE_ONLY
  source_package: ACHInterbank-R14-TPA7B-CANONICAL-SELECTOR.zip
  production_refactor_required: false
  selector_registration_context_parity:
    clearing_house: SAME_ROUTING_AUTHORITY_RE_RESOLVED_AT_REGISTRATION
    transaction_nature: SAME_RESOLVE_NATURE
    service_type: SAME_STANDARD_ENTRY_CLASS_CONTEXT
    effective_date: SAME_RESOLVED_CYCLE_PROCESSING_DATE_CONTEXT
    eligibility_authority: IThirdPartyAccountEligibilityService
  final_authorization:
    selector_is_ux_read_only: true
    registration_rechecks_server_side: true
    stale_selector_can_bypass_backend: false
  single_bulk:
    same_policy_authority: true
    same_eligibility_authority: true
    same_prerequisite_authority: true
    optional_credit_remains_optional: true
    mandatory_debit_fail_closed: true
  onboarding:
    generic_and_clearing_house_agnostic: true
    silent_single_bulk_preserved: true
  legacy:
    customer_third_party_removed: false
    selector_legacy_active_authority: false
    global_cutover: false
  prohibited_changes:
    backfill: false
    schema_migration: false
    tpa8_executed: false
  gaps_remaining:
    - GAP-TPA-013
    - GAP-TPA-015
    - GAP-TPA-016
  tests_added_or_strengthened:
    - AchTransactionNachaTests exact eligibility tuple for mandatory debit and optional credit
    - AchBulkTransactionServiceTests exact eligibility tuple for mandatory debit and optional credit

phase_7c_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA7C static selector/registration parity guard
    result: PASS

phase_7c_tests_deferred:
  - command: dotnet focal tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: Angular focal tests/build
    result: NOT_RUN_ENVIRONMENT
    reason: web/ach-interbank-ui/node_modules is absent in the supplied source archive

phase_8_status: CLOSED_TPA8C_COHERENCE_VERIFIED_RUNTIME_DEFERRED_ENVIRONMENT
phase_8a_status: COMPLETE_IMPLEMENTED_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_8a_safe_backfill:
  scope: TPA8A_BACKFILL_ONLY
  source_package: ACHInterbank-R15-TPA7C-PARITY-CLOSURE.zip
  master_path:
    source: CustomerThirdParty
    target: Customer -> ThirdParty -> ThirdPartyAccount
    onboarding_authority: IThirdPartyOnboardingService
    origin_source: Backfill
    natural_key_normalization_reused: true
    account_type_default_or_guess: false
    recipient_identity_default_or_guess: false
  execution:
    dry_run: true
    resumable_keyset_checkpoint: AfterLegacyThirdPartyId
    batch_size_max: 1000
    idempotent_replay: true
    metrics:
      - CREATED
      - REUSED
      - SKIPPED_INCOMPLETE
      - AMBIGUOUS
      - FAILED
  validation_history:
    independent_from_master_backfill: true
    requires_linked_prenotification_transaction: true
    requires_exact_destination_identity_match: true
    account_type_and_nature_from_exact_transaction_code_semantics: true
    chamber_from_linked_transaction_cycle_only: true
    service_type_from_company_entry_description_catalog: true
    inconsistent_status_state_is_ambiguous: true
    historical_policy_rule_id: null
    historical_policy_effective_from: null
    fabricated_deadline_or_expiry: false
    partial_context_restart_state: NotStarted
  compatibility:
    customer_third_party_operational: true
    customer_third_party_modified_by_backfill: false
    read_authority_cutover: false
    tpa7_selector_changed: false
    legacy_removed: false
    schema_migration: false
  privacy:
    logs_include_full_account_or_document: false
    issue_payload_contains_internal_id_and_code_only: true
  gaps_closed:
    - GAP-TPA-015
  gaps_remaining:
    - GAP-TPA-013
    - GAP-TPA-016
  tpa8b_executed: true
  tpa8c_executed: false
  tpa9_executed: false
  documentation:
    - docs/architecture/third-party-account-tpa8-backfill-cutover.md
  tests_added:
    - ThirdPartyAccountBackfillServiceTests dry-run
    - ThirdPartyAccountBackfillServiceTests repeated-apply idempotency
    - ThirdPartyAccountBackfillServiceTests existing-master reuse
    - ThirdPartyAccountBackfillServiceTests incomplete and ambiguous evidence
    - ThirdPartyAccountBackfillServiceTests normalized no-duplication
    - ThirdPartyAccountBackfillServiceTests no invented validation history
    - ThirdPartyAccountBackfillServiceTests neutral-context restart
    - ThirdPartyAccountBackfillServiceTests keyset checkpoint resume and replay

phase_8a_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA8A static phase guard
    result: PASS

phase_8a_tests_deferred:
  - command: dotnet focal ThirdPartyAccountBackfillServiceTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: Docker/provider runtime backfill verification
    result: NOT_RUN_ENVIRONMENT
    reason: docker command is not installed in the supplied execution environment


phase_8b_status: COMPLETE_IMPLEMENTED_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_8b_controlled_read_cutover:
  scope: CUSTOMER_THIRD_PARTY_COMPATIBILITY_READ_CUTOVER_ONLY
  source_package: ACHInterbank-R16-TPA8A-SAFE-BACKFILL.zip
  legacy_reads_cut_over:
    - GET /api/customer-third-parties via CustomerThirdPartyAppService.GetAsync
  unchanged_reads:
    - GET /api/third-party-accounts canonical TPA4 read model
    - TPA5/TPA6 canonical administration reads
    - TPA7 transaction ThirdPartyAccount selector
  canonical_authority:
    master: Customer -> ThirdParty -> ThirdPartyAccount
    contextual_validation: ThirdPartyAccountValidation -> PrenotificationValidationAttempt
    source_account_customer_resolution: UNIQUE_CUSTOMER_ID_ONLY
    customer_isolation_by_customer_id: true
    legacy_active_status_authority: false
    status_compatibility_projection:
      Accepted: Active
      TacitlyAccepted: Active
      Rejected: Rejected
      all_other_or_unknown: Pending
    compatibility_status_is_transaction_authorization: false
    multi_context_without_exact_link: Pending_without_context_inference
    exact_link_preference: PrenotificationValidationAttempt.AchTransactionId
  legacy_compatibility:
    customer_third_party_retained: true
    legacy_code_retained: true
    legacy_only_rows_visible: true
    legacy_only_status_projection: Pending
    legacy_only_can_match_active_or_rejected_filter: false
    legacy_removed: false
  shadow:
    retained: true
    previous_legacy_slice_reconstructed_for_comparison: true
    canonical_response_changes_on_shadow_failure: false
    pii_or_full_account_in_logs: false
  exclusions:
    backfill_repeated: false
    onboarding_changed: false
    tpa7_selector_changed: false
    schema_migration: false
    tpa8c_executed: false
    tpa9_executed: false
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-009
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa8-backfill-cutover.md
  tests_added_or_updated:
    - ThirdPartyAccountReadShadowTests CutoverRead_UsesCanonicalValidationAsAuthority
    - ThirdPartyAccountReadShadowTests CutoverRead_LegacyActiveCannotOverrideCanonicalRejected
    - ThirdPartyAccountReadShadowTests CutoverRead_IsolatesCanonicalAccountsByResolvedCustomer
    - ThirdPartyAccountReadShadowTests CutoverRead_LegacyOnlyFallbackStaysVisibleButNeverBecomesActiveAuthority
    - ThirdPartyAccountReadShadowTests CutoverRead_MultipleCanonicalValidationContextsDoNotInferOne
    - ThirdPartyAccountReadShadowTests CutoverRead_PreservesRelevantLegacyFiltersOverCanonicalMaster
    - ThirdPartyAccountReadShadowTests CutoverRead_RemainsCanonicalAuthority_WhenShadowComparisonFails

phase_8b_tests_executed:
  - command: python scripts/validate-solution-projects.py
    result: PASS
  - command: TPA8B static canonical-authority/no-selector/no-backfill/no-migration guard
    result: PASS

phase_8b_tests_deferred:
  - command: dotnet focal ThirdPartyAccountReadShadowTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: Docker/provider runtime cutover verification
    result: NOT_RUN_ENVIRONMENT
    reason: docker command is not installed in the supplied execution environment

phase_8c_status: COMPLETE_COHERENCE_VERIFIED_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_8c_closure:
  scope: TPA8_COHERENCE_CLOSURE_ONLY
  source_package: ACHInterbank-R17-TPA8B-CANONICAL-READ-CUTOVER.zip
  production_code_changed: false
  reviewed_components:
    - ThirdPartyAccountBackfillService
    - CustomerThirdPartyAppService
    - ThirdPartyAccountReadService
    - ThirdPartyAccountShadowComparisonService
    - direct TPA8A/TPA8B tests
  coherence:
    backfill_idempotent_and_resumable: true
    backfill_invents_missing_data: false
    productive_read_authority: CANONICAL_MASTER_PLUS_CONTEXTUAL_VALIDATION
    legacy_active_is_functional_authority: false
    unreconciled_legacy_projection: Pending
    shadow_changes_productive_response: false
    legacy_available_for_rollback: true
    tpa7_selector_changed: false
    onboarding_changed: false
    schema_migration: false
  cross_phase_tests_added:
    - ThirdPartyAccountBackfillServiceTests Tpa8Closure_BackfilledCanonicalHistoryFeedsCutoverCompatibilityRead
    - ThirdPartyAccountBackfillServiceTests Tpa8Closure_IncompleteBackfillRemainsConservativeLegacyFallback
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-009
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  tpa8_status: CLOSED
  next_allowed_phase: TPA9
  tpa9_executed: false

phase_8c_tests_executed:
  - command: TPA8C focal static coherence guard
    result: PASS

phase_8c_tests_deferred:
  - command: dotnet focal ThirdPartyAccountBackfillServiceTests and ThirdPartyAccountReadShadowTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: Docker/provider runtime coherence verification
    result: NOT_RUN_ENVIRONMENT
    reason: docker command is not installed in the supplied execution environment

phase_9a1_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9a1_single_hardening:
  scope: TPA9A1_SINGLE_ONLY
  source_package: ACHInterbank-R18-TPA8C-COHERENCE-CLOSURE.zip
  production_code_changed: true
  services_changed:
    - AchTransactionService
    - ThirdPartyOnboardingService
  atomicity:
    explicit_database_transaction_before_legacy_onboarding: true
    canonical_internal_savechanges_inside_same_transaction: true
    unit_of_work_commit_before_database_commit: true
    failed_attempt_rolls_back_legacy_canonical_transaction_and_dispatch_state: true
  idempotency_and_concurrency:
    canonical_unique_conflict_reread_preserved: true
    single_whole_attempt_unique_conflict_retry_count: 1
    failed_attempt_tracker_cleared_before_retry: true
    persistent_duplicate_is_not_hidden: true
    gap_tpa_009_single_portion: CLOSED_TPA9A1
    gap_tpa_009_bulk_portion: OPEN_FOR_TPA9A2
  cancellation:
    transaction_preflight_check: true
    per_attempt_check: true
    pre_unit_of_work_commit_check: true
    pre_database_commit_check: true
    onboarding_preflight_check: true
  error_handling:
    failed_single_attempt_tracker_cleanup: true
    original_nonrecoverable_exception_preserved: true
    execution_strategy_retry_uses_clean_tracker: true
  business_rule_changes: false
  ach_cenit_rule_changes: false
  schema_migration: false
  spa_changed: false
  bulk_changed: false
  backfill_changed: false
  tpa9a2_executed: false
  tpa10_executed: false
  tests_added_or_updated:
    - AchTransactionNachaTests RegisterTransactionAsync_WhenDispatchPersistenceFails_RollsBackSilentOnboarding
    - AchTransactionNachaTests RegisterTransactionAsync_RecoversLegacyOnboardingUniqueConflict_WithCleanRetry
    - AchTransactionNachaTests RegisterTransactionAsync_WhenCancellationArrivesBeforeCommit_RollsBackAndClearsTracker
    - AchTransactionNachaTests RegisterTransactionAsync_WhenUnitOfWorkCommitFails_RollsBackAndClearsTracker
    - ThirdPartyOnboardingServiceTests EnsureAccountAsync_PreCanceledToken_DoesNotCreateCanonicalRows
  existing_tests_reused:
    - AchTransactionNachaTests RegisterTransactionAsync_WhenPolicyRejectsDuplicate_DoesNotCreateSecondInitialStateEvent
    - AchTransactionNachaTests RegisterTransactionAsync_Prenotification_OnboardsParticipantsIdempotently
    - ThirdPartyOnboardingServiceTests EnsureAccountAsync_RecoversUniqueConflict_WithIdempotentReread
    - ThirdPartyOnboardingServiceTests EnsureAccountAsync_RecoversConcurrentAccountUniqueConflict_WithIdempotentReread
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9A2

phase_9a1_tests_executed:
  - command: TPA9A1 targeted source/test/static scope guard
    result: PASS

phase_9a1_tests_deferred:
  - command: dotnet focal AchTransactionNachaTests and ThirdPartyOnboardingServiceTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment


phase_9a2_1_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9a2_1_bulk_atomicity_hardening:
  scope: TPA9A2_1_BULK_ATOMICITY_ONLY
  source_package: ACHInterbank-R19-TPA9A1-SINGLE-HARDENING.zip
  production_code_changed: true
  services_changed:
    - AchBulkTransactionService
  atomicity:
    existing_database_transaction_per_chunk_preserved: true
    item_savepoint_before_mutating_orchestration: true
    handled_item_failure_rolls_back_to_item_savepoint: true
    successful_item_flush_inside_open_chunk_transaction: true
    unit_of_work_commit_once_per_chunk_preserved: true
    database_commit_after_unit_of_work_preserved: true
    prior_committed_chunks_survive_later_chunk_failure_or_cancellation: true
  rollback_and_isolation:
    failed_item_legacy_rows_do_not_leak_into_successful_sibling_item: true
    failed_item_canonical_rows_do_not_leak_into_successful_sibling_item: true
    unexpected_chunk_failure_explicitly_rolls_back_current_chunk: true
    unit_of_work_failure_rolls_back_current_chunk: true
  cancellation:
    request_preflight_check: true
    per_chunk_check: true
    per_item_check: true
    pre_unit_of_work_commit_check: true
    pre_database_commit_check: true
    cleanup_rollback_uses_non_cancelable_token: true
  tracker_cleanup:
    item_failure_clears_tracker_after_savepoint_rollback: true
    customer_cache_rehydrated_after_item_failure: true
    customer_cache_scoped_per_chunk: true
    chunk_failure_clears_tracker: true
    chunk_commit_or_empty_rollback_clears_tracker: true
    touched_batches_rehydrated_by_persisted_id_after_tracker_cleanup: true
  concurrency_unique_conflict_reviewed: false
  gap_tpa_009_bulk_concurrency_portion: OPEN_FOR_TPA9A2_2
  business_rule_changes: false
  ach_cenit_rule_changes: false
  schema_migration: false
  spa_changed: false
  single_changed: false
  backfill_changed: false
  selector_changed: false
  tpa9a2_2_executed: false
  tpa9a3_executed: false
  tpa10_executed: false
  tests_added_or_updated:
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenItemFailsAfterCanonicalOnboarding_RollsBackOnlyThatItemAndDoesNotContaminateNextItem
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenCancellationOccursInLaterChunk_PreservesCommittedPriorChunkAndRollsBackCurrentChunk
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenUnitOfWorkCommitFails_RollsBackChunkAndClearsTracker
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9A2.2

phase_9a2_1_tests_executed:
  - command: TPA9A2.1 targeted source/test/static scope guard
    result: PASS

phase_9a2_1_tests_deferred:
  - command: dotnet focal AchBulkTransactionServiceTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment


phase_9a2_2_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9a2_2_bulk_concurrency_hardening:
  scope: TPA9A2_2_BULK_CONCURRENCY_ONLY
  source_package: ACHInterbank-R20-TPA9A2-1-BULK-ATOMICITY-HARDENING.zip
  production_code_changed: true
  services_changed:
    - AchBulkTransactionService
  services_reviewed_unchanged:
    - ThirdPartyOnboardingService
  legacy_customer_account_concurrency:
    bounded_unique_conflict_retry_count: 1
    retry_scope: LEGACY_CUSTOMER_CUSTOMERACCOUNT_ENSURE_AND_IMMEDIATE_SAVE_ONLY
    item_savepoint_rollback_before_retry: true
    tracker_clear_before_reread: true
    database_winner_reread_via_customer_cache_rebuild: true
    persistent_second_unique_conflict_not_hidden: true
    downstream_transaction_orchestration_retried: false
  canonical_concurrency:
    third_party_unique_conflict_reread_preserved: true
    third_party_account_unique_conflict_reread_preserved: true
    duplicated_bulk_specific_canonical_retry_added: false
  item_chunk_isolation:
    tpa9a2_1_item_savepoint_semantics_preserved: true
    tpa9a2_1_chunk_commit_semantics_preserved: true
    prior_successful_items_survive_loser_retry_inside_chunk: true
    stale_failed_item_entities_cleared_before_reread: true
    no_blind_retry_after_canonical_or_transaction_persistence: true
  gap_tpa_009:
    bulk_customer_customeraccount_portion: CLOSED_TPA9A2_2
    canonical_thirdparty_account_portion: CLOSED_EXISTING_SHARED_ONBOARDING_EVIDENCE
    compatibility_customerthirdparty_portion: OPEN_SCOPE_EXCLUDED
    overall_status: OPEN_NARROWED
  business_rule_changes: false
  ach_cenit_rule_changes: false
  schema_migration: false
  spa_changed: false
  single_changed: false
  backfill_changed: false
  selector_changed: false
  tpa9a3_executed: false
  tpa10_executed: false
  tests_added_or_updated:
    - AchBulkTransactionServiceTests RegisterBulkAsync_RecoversConcurrentLegacyCustomerUniqueConflict_ByRereadingWinner
    - AchBulkTransactionServiceTests RegisterBulkAsync_RecoversConcurrentLegacyCustomerAccountUniqueConflict_WithoutDuplicate
  existing_tests_reused:
    - ThirdPartyOnboardingServiceTests EnsureAccountAsync_RecoversUniqueConflict_WithIdempotentReread
    - ThirdPartyOnboardingServiceTests EnsureAccountAsync_RecoversConcurrentAccountUniqueConflict_WithIdempotentReread
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenItemFailsAfterCanonicalOnboarding_RollsBackOnlyThatItemAndDoesNotContaminateNextItem
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenCancellationOccursInLaterChunk_PreservesCommittedPriorChunkAndRollsBackCurrentChunk
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenUnitOfWorkCommitFails_RollsBackChunkAndClearsTracker
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9A3

phase_9a2_2_tests_executed:
  - command: TPA9A2.2 targeted source/test/static scope guard
    result: PASS

phase_9a2_2_tests_deferred:
  - command: dotnet focal AchBulkTransactionServiceTests and ThirdPartyOnboardingServiceTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment


phase_9a2_3_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9a2_3_customer_third_party_concurrency_hardening:
  scope: TPA9A2_3_CUSTOMER_THIRD_PARTY_CONCURRENCY_ONLY
  source_package: ACHInterbank-R21-TPA9A2-2-BULK-CONCURRENCY-HARDENING.zip
  production_code_changed: true
  services_changed:
    - PrenotificationHandler
  customer_third_party_concurrency:
    existing_natural_key_lookup_preserved: true
    explicit_transaction_required_for_conflict_detection_flush: true
    local_savepoint_before_conflict_detection_flush: true
    database_commit_performed_by_handler: false
    unique_conflict_classification:
      postgresql_sqlstate_23505: true
      sql_server_2601_2627: true
      sqlite_constraint_19_test_evidence: true
    rollback_to_local_savepoint_before_reread: true
    natural_key_winner_reread: true
    losing_attempt_detached: true
    duplicate_left_tracked_after_recovery: false
    winner_missing_rethrows_original_failure: true
  idempotency:
    sequential_existing_reuse_preserved: true
    pending_winner_relinked_with_existing_compatibility_semantics: true
    active_winner_not_reset: true
    rejected_winner_not_reset: true
    non_unique_failure_not_retried_or_reread: true
    blind_orchestration_retry_added: false
  canonical_isolation:
    third_party_onboarding_changed: false
    third_party_account_onboarding_changed: false
    canonical_unique_conflict_logic_duplicated: false
  gap_tpa_009:
    single_legacy_portion: CLOSED_TPA9A1
    bulk_customer_customeraccount_portion: CLOSED_TPA9A2_2
    canonical_thirdparty_account_portion: CLOSED_EXISTING_SHARED_ONBOARDING_EVIDENCE
    compatibility_customerthirdparty_portion: CLOSED_TPA9A2_3
    overall_status: CLOSED_TPA9A2_3
  business_rule_changes: false
  ach_cenit_rule_changes: false
  schema_migration: false
  spa_changed: false
  single_or_bulk_orchestration_changed: false
  backfill_changed: false
  selector_changed: false
  tpa9a3_executed: false
  tpa10_executed: false
  tests_added_or_updated:
    - PrenotificationHandlerTests HandleAsync_RecoversConcurrentCustomerThirdPartyUniqueConflict_ByRereadingWinner
    - PrenotificationHandlerTests HandleAsync_ConcurrentWinnerWithFinalStatus_IsRereadWithoutReset
    - PrenotificationHandlerTests HandleAsync_DoesNotRetryOrReread_ForNonUniquePersistenceFailure
  existing_tests_reused:
    - PrenotificationHandlerTests HandleAsync_ReusesPendingThirdParty_AndAddsOnlyOnceAcrossRetry
    - PrenotificationHandlerTests HandleAsync_DoesNotResetDefinitiveThirdPartyStatus
    - PrenotificationHandlerTests HandleAsync_CreatesThirdParty_WithCustomerNavigation_WhenCustomerIsTrackedWithTemporaryKey
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9A3

phase_9a2_3_tests_executed:
  - command: TPA9A2.3 targeted source/test/static scope guard
    result: PASS

phase_9a2_3_tests_deferred:
  - command: dotnet focal PrenotificationHandlerTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment



phase_9a3_1_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9a3_1_validation_lifecycle_hardening:
  scope: TPA9A3_1_VALIDATION_PRENOTIFICATION_LIFECYCLE_ONLY
  source_package: ACHInterbank-R22-TPA9A2-3-CUSTOMER-THIRD-PARTY-CONCURRENCY-HARDENING.zip
  production_code_changed: true
  services_changed:
    - ThirdPartyAccountValidationService
  attempt_history:
    append_only_preserved: true
    same_idempotency_key_replay_reuses_existing: true
    same_ach_transaction_with_different_retry_key_reuses_existing: true
    incompatible_key_or_transaction_context_fails_safe: true
    duplicate_attempt_on_replay_prevented: true
  reprenotification:
    distinct_ach_transaction_appends_new_attempt: true
    next_attempt_number_preserved: true
    current_attempt_id_moves_to_new_attempt: true
    prior_attempt_not_reset_or_deleted: true
  persistence_atomicity:
    local_relational_transaction_when_no_caller_transaction: true
    local_savepoint_inside_existing_transaction_when_supported: true
    attempt_insert_and_current_attempt_pointer_one_persistence_scope: true
    rollback_uses_non_cancelable_cleanup_token: true
    failed_attempt_detached: true
    validation_reloaded_after_rollback: true
  unique_conflict_recovery:
    bounded_retry_preserved: true
    replay_winner_reread_first: true
    sequence_retry_requires_exact_validation_attempt_number_winner: true
    unrelated_unique_conflict_not_recovered: true
    validation_reread_before_sequence_retry: true
  state_transitions:
    terminal_attempts_immutable:
      - Accepted
      - TacitlyAccepted
      - Rejected
    duplicate_terminal_event_does_not_overwrite_evidence: true
    conflicting_terminal_event_does_not_overwrite_evidence_or_causal: true
    stale_older_attempt_resolution_does_not_replace_current_attempt_id: true
    missing_current_pointer_repaired_only_when_attempt_is_latest: true
  rejection_causal:
    first_terminal_return_code_preserved: true
    first_terminal_return_detail_preserved: true
    first_terminal_evidence_preserved_against_later_events: true
  tacit_acceptance:
    automatic_applied_tacitly_mapping_preserved: true
    manual_approval_rejection_added: false
  technical_review:
    missing_effective_policy_fail_safe_preserved: true
    required_deadline_context_missing_routes_to_technical_review: true
    independent_of_tacit_acceptance_flag: true
  cancellation:
    registration_preflight_check: true
    pre_persistence_checks: true
    pre_canceled_registration_creates_no_validation_or_attempt: true
    pre_canceled_resolution_does_not_mutate_attempt_or_parent: true
  business_rule_changes: false
  ach_cenit_rule_changes: false
  eligibility_changed: false
  single_changed: false
  bulk_changed: false
  spa_changed: false
  backfill_changed: false
  schema_migration: false
  manual_result_endpoint_added: false
  memory_changed: false
  tests_added_or_updated:
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_IsIdempotent_StoresRejectionCause_AndAllowsReprenotification
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_IdempotencyKeyCollisionWithDifferentTransaction_FailsSafeWithoutDuplicate
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_TerminalAttempt_IsImmutableAgainstReplayOrConflictingLateResolution
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_LateResolutionOfOlderAttempt_DoesNotCorruptCurrentReprenotificationState
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_MissingDeadlineContext_RoutesToTechnicalReviewEvenWhenTacitAcceptanceIsDisabled
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_WhenCurrentAttemptPointerPersistenceFails_RollsBackAttemptAndParentState
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_PreCanceledRegistration_DoesNotCreateValidationOrAttempt
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_PreCanceledResolution_DoesNotMutateAttemptOrCurrentState
  existing_tests_reused:
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_MissingEffectivePolicy_RoutesToTechnicalReviewWithoutInventingOutcome
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_TacitAcceptance_IsAutomaticState_NotManualApproval
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  tpa9a3_2_executed: false
  tpa10_executed: false
  next_allowed_phase: TPA9A3.2

phase_9a3_1_tests_executed:
  - command: TPA9A3.1 targeted four-file static scope/structure guard
    result: PASS

phase_9a3_1_tests_deferred:
  - command: dotnet focal ThirdPartyAccountValidationLifecycleTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_9a3_2_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9a3_2_contextual_eligibility_hardening:
  scope: TPA9A3_2_CONTEXTUAL_ELIGIBILITY_ONLY
  source_package: ACHInterbank-R23-TPA9A3-1-VALIDATION-LIFECYCLE-HARDENING.zip
  production_code_changed: true
  services_changed:
    - ThirdPartyAccountEligibilityService
  fail_closed:
    pre_canceled_call_stops_before_reads: true
    missing_canonical_account_or_inactive_account_blocks: true
    missing_or_inactive_canonical_third_party_blocks: true
    missing_effective_policy_is_unsupported_non_authorizing: true
    inconsistent_resolved_policy_routes_to_technical_review: true
    duplicate_contextual_validation_rows_not_arbitrarily_selected: true
    positive_parent_state_requires_positive_append_only_attempt_evidence: true
    read_failures_are_not_converted_to_allow: true
  contextual_matching:
    canonical_account_id_required: true
    clearing_house_required: true
    transaction_nature_required: true
    normalized_service_type_required: true
    validation_type_prenotification_required: true
    future_dated_validation_cannot_authorize_earlier_effective_date: true
  administrative_authority:
    canonical_third_party_status_required_active: true
    canonical_third_party_account_status_required_active: true
    legacy_customer_third_party_active_used_as_authority: false
  policy_version:
    effective_rule_id_mismatch_requires_revalidation: true
    resolved_rule_active_verified: true
    resolved_rule_clearing_house_verified: true
    resolved_rule_transaction_nature_verified: true
    resolved_rule_effective_date_window_verified: true
    ach_cenit_rule_values_changed: false
  state_handling:
    PendingSend: PrenotificationPending
    PendingResponse: PrenotificationPending
    Rejected: Rejected
    RequiresCorrection: RequiresRevalidation
    TechnicalReview: TechnicalReview
    Suspended: TechnicalReview
    Expired: ValidationExpired
    Accepted_requires_consistent_attempt_evidence: true
    TacitlyAccepted_requires_consistent_attempt_evidence: true
  determinism:
    eligibility_is_read_only: true
    latest_attempt_order: AttemptNumber_DESC_THEN_Id_DESC
    repeated_unchanged_context_same_decision: true
    duplicate_context_rows_fail_closed: true
  cancellation_and_read_errors:
    cancellation_propagates: true
    master_read_failure_propagates: true
    policy_read_failure_propagates: true
    failure_downgraded_to_allow: false
  lifecycle_tpa9a3_1_changed: false
  single_changed: false
  bulk_changed: false
  spa_changed: false
  selector_changed: false
  backfill_changed: false
  schema_migration: false
  memory_changed: false
  tpa9a4_executed: false
  tpa10_executed: false
  tests_added_or_updated:
    - ThirdPartyAccountValidationLifecycleTests Eligibility_MissingPolicy_FailsClosedWithoutInventingAuthorization
    - ThirdPartyAccountValidationLifecycleTests Eligibility_InactiveCanonicalMaster_BlocksAndDoesNotUseLegacyActiveAsAuthority
    - ThirdPartyAccountValidationLifecycleTests Eligibility_InactiveCanonicalThirdPartyMaster_BlocksBeforePolicyResolution
    - ThirdPartyAccountValidationLifecycleTests Eligibility_ResolvedPolicyOutsideRequestedEffectiveContext_FailsClosedToTechnicalReview
    - ThirdPartyAccountValidationLifecycleTests Eligibility_NonPositiveValidationStates_AreAlwaysNonAuthorizing
    - ThirdPartyAccountValidationLifecycleTests Eligibility_ValidationMustMatchChamberNatureServiceTypeAndRequestedDate
    - ThirdPartyAccountValidationLifecycleTests Eligibility_PositiveParentStateWithoutMatchingAppendOnlyEvidence_FailsClosedToTechnicalReview
    - ThirdPartyAccountValidationLifecycleTests Eligibility_RepeatedEvaluation_IsDeterministicAndReadOnly
    - ThirdPartyAccountValidationLifecycleTests Eligibility_DuplicateContextRows_FailClosedRatherThanPickArbitraryValidation
    - ThirdPartyAccountValidationLifecycleTests Eligibility_PreCanceledEvaluation_PropagatesCancellationBeforeAnyReadOrPolicyDecision
    - ThirdPartyAccountValidationLifecycleTests Eligibility_PolicyReadFailure_PropagatesAndNeverTurnsIntoAllow
    - ThirdPartyAccountValidationLifecycleTests Eligibility_MasterReadFailure_PropagatesAndNeverTurnsIntoAllow
  existing_tests_reused:
    - ThirdPartyAccountValidationLifecycleTests Eligibility_FunctionalPolicyVersionChange_RequiresRevalidation
    - ThirdPartyAccountValidationLifecycleTests Eligibility_IsContextual_NotGlobalBoolean_AndAchCreditLeadAppliesOnlyWhenPrenotified
    - ThirdPartyAccountValidationLifecycleTests Eligibility_CenitDebitRetentionExpiresFromLastMonetaryUse_WithoutLeakingToAchColombia
    - ThirdPartyAccountValidationLifecycleTests PolicyResolution_UsesEffectiveVersionByDate
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9A4

phase_9a3_2_tests_executed:
  - command: TPA9A3.2 targeted four-file static scope/structure guard
    result: PASS

phase_9a3_2_tests_deferred:
  - command: dotnet focal ThirdPartyAccountValidationLifecycleTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

migrations:
  - 20261011173000_AddThirdPartyMasterData
  - 20261011173100_AddThirdPartyMasterData
  - 20261011184000_AddThirdPartyAccountValidationLifecycle
  - 20261011184100_AddThirdPartyAccountValidationLifecycle
phase_9a4_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9a4_backfill_read_cutover_shadow_hardening:
  scope: TPA9A4_TPA8A_TPA8B_SHADOW_HARDENING_ONLY
  source_package: ACHInterbank-R24-TPA9A3-2-ELIGIBILITY-CONTEXT-HARDENING.zip
  production_code_changed: true
  services_changed:
    - ThirdPartyAccountBackfillService
    - CustomerThirdPartyAppService
  backfill_resilience:
    checkpoint_semantics: LAST_CONTIGUOUS_CHECKPOINT_SAFE_LEGACY_ID
    unexpected_master_failure_blocks_checkpoint: true
    unexpected_history_failure_blocks_checkpoint: true
    checkpoint_block_forces_has_more: true
    later_rows_may_be_attempted_without_advancing_checkpoint: true
    replay_after_failure_uses_existing_idempotency_and_natural_keys: true
    per_record_failure_clears_change_tracker: true
    cancellation_during_record_clears_change_tracker_before_rethrow: true
    pre_canceled_batch_creates_canonical_rows: false
    linked_historical_outcome_conflict_overwrites_evidence: false
    linked_historical_outcome_conflict_classification: CANONICAL_LINKED_HISTORY_STATE_CONFLICT
    concurrent_attempt_outcome_conflict_overwrites_evidence: false
    malformed_linked_transaction_identity_is_guessed: false
    malformed_linked_transaction_identity_classification: LINKED_TRANSACTION_IDENTITY_INVALID
  read_cutover:
    productive_authority: CANONICAL_FIRST
    source_account_customer_resolution: UNIQUE_CUSTOMER_ID_ONLY
    canonical_read_failure_fallback: LEGACY_VISIBLE_PENDING_ONLY
    pre_canceled_read_stops_before_reads: true
    cancellation_downgraded_to_fallback: false
    legacy_active_status_authority: false
    malformed_legacy_natural_key_inferred: false
    positive_parent_state_requires_matching_latest_attempt_evidence: true
    rejected_parent_state_requires_matching_latest_attempt_evidence: true
    current_attempt_pointer_when_present_must_match_latest: true
    inconsistent_final_evidence_projection: Pending
  shadow:
    retained: true
    source_customer_first_match_inference: false
    ambiguous_source_shadow_slice: EMPTY
    shadow_failure_changes_productive_response: false
    legacy_available_for_rollback: true
  privacy:
    backfill_failure_logs_include_exception_object_or_message: false
    cutover_shadow_failure_logs_include_exception_object_or_message: false
    logs_include_full_account_or_document: false
    logs_include_customer_names: false
    logged_failure_detail: exception_type_only
  exclusions:
    lifecycle_tpa9a3_changed: false
    eligibility_tpa9a3_changed: false
    single_changed: false
    bulk_changed: false
    spa_changed: false
    selector_changed: false
    schema_migration: false
    ach_cenit_rule_values_changed: false
    legacy_removed: false
    shadow_removed: false
    tpa9b_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - ThirdPartyAccountBackfillServiceTests RunAsync_UnexpectedRecordFailure_DoesNotAdvanceCheckpoint_AndReplayCanRecover
    - ThirdPartyAccountBackfillServiceTests RunAsync_ReplayWithConflictingLegacyOutcome_DoesNotOverwriteCanonicalHistory
    - ThirdPartyAccountBackfillServiceTests RunAsync_PreCanceledBatch_DoesNotCreateCanonicalData
    - ThirdPartyAccountReadShadowTests CutoverRead_CanonicalFinalStateRequiresMatchingLatestAttemptEvidence
    - ThirdPartyAccountReadShadowTests CutoverRead_CanonicalProjectionFailureFallsBackToLegacyPendingOnly
    - ThirdPartyAccountReadShadowTests CutoverRead_AmbiguousSourceDoesNotLetShadowInferOneCustomer
    - ThirdPartyAccountReadShadowTests CutoverRead_ShadowFailureDoesNotLogExceptionPii
    - ThirdPartyAccountReadShadowTests CutoverRead_PreCanceledRequestPropagatesAndDoesNotRunShadow
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9B

phase_9a4_tests_executed:
  - command: TPA9A4 targeted six-file static scope/structure guard
    result: PASS

phase_9a4_tests_deferred:
  - command: dotnet focal ThirdPartyAccountBackfillServiceTests and ThirdPartyAccountReadShadowTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment



phase_9b1_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b1_single_bulk_observability:
  scope: TPA9B1_SINGLE_BULK_OPERATIONAL_OBSERVABILITY_ONLY
  source_package: ACHInterbank-R25-TPA9A4-BACKFILL-READ-CUTOVER-HARDENING.zip
  production_code_changed: true
  services_changed:
    - AchTransactionService
    - AchBulkTransactionService
  single:
    terminal_outcomes:
      - accepted
      - rejected
      - canceled
      - failed
    rejection_stages:
      - request_validation
      - transaction_policy
      - canonical_prerequisite
      - classification
    unique_conflict_retry_observed: true
    rollback_reasons:
      - unique_conflict
      - canceled
      - failed
    behavior_changed: false
  bulk:
    terminal_outcomes:
      - accepted
      - partial
      - rejected
      - canceled
      - failed
    per_item_outcome_observed: true
    per_item_stages:
      - request_validation
      - transaction_policy
      - onboarding
      - canonical_prerequisite
      - classification
      - processing
      - cancellation
      - unexpected
      - committed
    unique_conflict_retry_observed: true
    item_savepoint_rollback_observed: true
    chunk_rollback_observed: true
    successful_item_observed_only_after_chunk_commit: true
    behavior_changed: false
  metrics:
    single_meter: Cfa.ACHInterbank.ACH.Single
    bulk_meter: Cfa.ACHInterbank.ACH.Bulk
    instruments:
      - ach_single_operations_total
      - ach_single_retries_total
      - ach_single_rollbacks_total
      - ach_bulk_operations_total
      - ach_bulk_items_total
      - ach_bulk_retries_total
      - ach_bulk_rollbacks_total
    bounded_metric_tags:
      - outcome
      - stage
      - reason
      - scope
    high_cardinality_identifiers_in_metric_tags: false
  logging_privacy:
    request_payload_logged: false
    account_number_logged: false
    document_logged: false
    customer_or_recipient_name_logged: false
    exception_message_logged: false
    exception_object_logged: false
    exception_type_only_on_failure: true
    bulk_item_index_logged: true
    telemetry_best_effort_no_throw: true
  exclusions:
    lifecycle_tpa9a3_changed: false
    eligibility_tpa9a3_changed: false
    backfill_read_cutover_tpa9a4_changed: false
    spa_changed: false
    schema_migration: false
    ach_cenit_rule_values_changed: false
    tpa9b2_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - AchTransactionNachaTests RegisterTransactionAsync_CreatesTransactionAndBatch observability assertions
    - AchTransactionNachaTests RegisterTransactionAsync_MandatoryDebitEligibilityFailure_StopsBeforePersistence observability assertions
    - AchTransactionNachaTests RegisterTransactionAsync_RecoversLegacyOnboardingUniqueConflict_WithCleanRetry observability assertions
    - AchTransactionNachaTests RegisterTransactionAsync_WhenCancellationArrivesBeforeCommit_RollsBackAndClearsTracker observability assertions
    - AchTransactionNachaTests RegisterTransactionAsync_WhenUnitOfWorkCommitFails_RollsBackAndClearsTracker privacy/observability assertions
    - AchBulkTransactionServiceTests RegisterBulkAsync_ReturnsTotalSuccess_WhenAllItemsAreValid observability assertions
    - AchBulkTransactionServiceTests RegisterBulkAsync_ReturnsPartialSuccess_WhenOneItemFailsValidation observability assertions
    - AchBulkTransactionServiceTests RegisterBulkAsync_MandatoryDebitEligibilityFailure_FailsItemBeforePersistence observability assertions
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenCancellationOccursInLaterChunk_PreservesCommittedPriorChunkAndRollsBackCurrentChunk observability assertions
    - AchBulkTransactionServiceTests RegisterBulkAsync_WhenUnitOfWorkCommitFails_RollsBackChunkAndClearsTracker privacy/observability assertions
    - AchBulkTransactionServiceTests RegisterBulkAsync_RecoversConcurrentLegacyCustomerUniqueConflict_ByRereadingWinner observability assertions
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9B2

phase_9b1_tests_executed:
  - command: TPA9B1 targeted six-file static scope/structure guard
    result: PASS

phase_9b1_tests_deferred:
  - command: dotnet focal AchTransactionNachaTests and AchBulkTransactionServiceTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment


phase_9b2_1_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b2_1_selector_read_query_efficiency:
  scope: TPA9B2_1_SELECTOR_CANONICAL_READ_QUERY_EFFICIENCY_ONLY
  source_package: ACHInterbank-R26-TPA9B1-SINGLE-BULK-OBSERVABILITY-HARDENING.zip
  production_code_changed: true
  services_changed:
    - TransactionThirdPartyAccountSelectorService
    - ThirdPartyAccountReadService
  selector:
    as_no_tracking_preserved: true
    customer_and_destination_filters_in_database: true
    candidate_entity_includes_removed: true
    scalar_projection_only_for_response_shape: true
    validation_history_loaded: false
    attempt_history_loaded: false
    eligibility_evaluations_per_candidate: EXACTLY_ONE_EXISTING_SERVICE_CALL
    selector_limit_or_pagination_added: false
    semantics_changed: false
  canonical_read:
    as_no_tracking_preserved: true
    filters_applied_before_pagination: true
    max_page_size: 100
    page_id_prequery_removed: true
    graph_loaded_only_for_paged_roots: true
    summary_attempt_loading: LATEST_ONLY_BY_ATTEMPT_NUMBER_DESC_ID_DESC
    full_history_endpoint_preserved: true
    validation_history_query_behavior_changed: false
    semantics_changed: false
  n_plus_one:
    canonical_relationship_lazy_queries: false
    duplicate_eligibility_evaluation_per_item: false
    scalar_eligibility_service_call_per_item_preserved: true
    paged_read_eligibility_calls_bounded_by_max_page_size: true
    selector_candidate_cap_added: false
    note: Eligibility batching/API redesign is outside TPA9B2.1 and no selector truncation was introduced because semantics must remain unchanged.
  exclusions:
    eligibility_changed: false
    lifecycle_changed: false
    single_bulk_changed: false
    spa_changed: false
    backfill_changed: false
    schema_migration: false
    cache_added: false
    ach_cenit_rule_values_changed: false
    tpa9b2_2_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - TransactionThirdPartyAccountSelectorTests Selector_ProjectsCandidatesWithoutMaterializingCanonicalEntityGraphs
    - TransactionThirdPartyAccountSelectorTests Selector_EvaluatesEligibilityExactlyOncePerProjectedCandidate
    - ThirdPartyAccountReadShadowTests CanonicalRead_LoadsOnlyLatestAttemptPerValidation
    - ThirdPartyAccountReadShadowTests CanonicalRead_ClampsPageSizeAndPagesBeforeLoadingCanonicalGraphs
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9B2.2

phase_9b2_1_tests_executed:
  - command: TPA9B2.1 targeted six-file static scope/structure guard
    result: PASS

phase_9b2_1_tests_deferred:
  - command: dotnet focal TransactionThirdPartyAccountSelectorTests and ThirdPartyAccountReadShadowTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_9b2_2_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b2_2_administration_query_command_efficiency:
  scope: TPA9B2_2_CANONICAL_ADMINISTRATION_QUERY_COMMAND_EFFICIENCY_ONLY
  source_package: ACHInterbank-R27-TPA9B2-1-SELECTOR-READ-QUERY-EFFICIENCY-HARDENING.zip
  production_code_changed: true
  services_changed:
    - ThirdPartyAdministrationService
  controller_opened_or_changed: false
  administrative_reads:
    as_no_tracking_preserved: true
    customer_filter_in_database: true
    status_and_search_filters_before_pagination: true
    max_page_size: 100
    deterministic_ordering_preserved: true
    relationship_includes_added: false
    create_post_onboarding_duplicate_reread_removed: true
    create_post_onboarding_projection_reused_for_response: true
  commands:
    create_account_owned_third_party_read: AS_NO_TRACKING_SCALAR_IDENTITY_PROJECTION
    account_status_parent_include_removed: true
    account_status_ownership_single_predicate_preserved: true
    update_third_party_noop_save_skipped: true
    status_noop_save_behavior_preserved: true
    onboarding_behavior_changed: false
    idempotency_changed: false
    concurrency_behavior_changed: false
    audit_semantics_changed: false
    banking_identity_mutability_changed: false
  ownership:
    customer_third_party_scope_preserved: true
    customer_third_party_account_scope_preserved: true
    parent_entity_materialization_required_for_account_status: false
    isolation_weakened: false
  n_plus_one:
    third_party_list_account_count: SERVER_SIDE_AGGREGATE
    per_row_relationship_queries_added: false
    validation_history_loaded: false
    account_relationship_graph_loaded_for_ownership_only: false
  exclusions:
    onboarding_changed: false
    eligibility_changed: false
    lifecycle_changed: false
    selector_changed: false
    spa_changed: false
    backfill_changed: false
    schema_migration: false
    cache_added: false
    ach_cenit_rule_values_changed: false
    tpa9b3_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - ThirdPartyAdministrationApiTests AdministrativeRead_DoesNotTrackCanonicalMasterGraph
    - ThirdPartyAdministrationApiTests AccountStatusCommand_ValidatesOwnershipWithoutMaterializingThirdPartyRelationship
    - ThirdPartyAdministrationApiTests UpdateThirdParty_NoOpPreservesAdministrativeResultWithoutAdditionalPersistenceWork
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9B3

phase_9b2_2_tests_executed:
  - command: TPA9B2.2 targeted four-file static scope/structure guard
    result: PASS

phase_9b2_2_tests_deferred:
  - command: dotnet focal ThirdPartyAdministrationApiTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_9b2_3_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b2_3_backfill_query_resume_efficiency:
  scope: TPA9B2_3_TPA8A_BACKFILL_QUERY_RESUME_EFFICIENCY_ONLY
  source_package: ACHInterbank-R28-TPA9B2-2-ADMIN-QUERY-COMMAND-EFFICIENCY-HARDENING.zip
  production_code_changed: true
  services_changed:
    - ThirdPartyAccountBackfillService
  batching:
    checkpoint_key: AfterLegacyThirdPartyId
    keyset_filter_preserved: true
    deterministic_id_order_preserved: true
    max_batch_size: 1000
    lookahead_strategy: TAKE_BATCH_SIZE_PLUS_ONE
    lookahead_row_processed: false
    duplicate_batch_array_removed: true
    checkpoint_semantics_changed: false
  query_efficiency:
    canonical_account_lookup: AS_NO_TRACKING_SCALAR_ID_THIRD_PARTY_ID_PROJECTION
    canonical_account_type_filter_pushed_to_database_when_known: true
    canonical_account_candidate_limit: 2
    recipient_customer_projection_only: true
    recipient_customer_candidate_limit: 2
    ambiguous_recipient_short_circuits_third_party_lookup: true
    canonical_third_party_projection_only: true
    canonical_third_party_candidate_limit: 2
    linked_attempt_candidate_limit: 2
    linked_attempt_master_graph: VALIDATION_ACCOUNT_THIRD_PARTY_REQUIRED_FOR_IDENTITY
    linked_attempt_history_graph: VALIDATION_ONLY
    repeated_linked_attempt_read_removed: false
    repeated_linked_attempt_read_reason: PRESERVE_CONCURRENT_WINNER_VISIBILITY_BETWEEN_MASTER_AND_HISTORY
    validation_attempt_collection_materialized_for_existence_check: false
    validation_attempt_existence_check: SERVER_SIDE_ANY
    concurrent_attempt_probe_as_no_tracking: true
  write_efficiency:
    savechanges_boundaries_removed: false
    reason: Existing saves materialize canonical ids, neutral resumable validation context, append-only attempt evidence, or CurrentAttemptId and are not proven redundant under concurrency/replay.
    idempotency_changed: false
    checkpoint_safety_changed: false
    historical_evidence_mutability_changed: false
  cancellation:
    request_entry_check_preserved: true
    per_record_check_preserved: true
    post_master_pre_history_check_added: true
    tracker_cleared_on_observed_cancellation: true
    persisted_master_rollback_invented: false
  exclusions:
    read_cutover_changed: false
    administration_changed: false
    single_bulk_changed: false
    eligibility_changed: false
    lifecycle_changed: false
    selector_changed: false
    spa_changed: false
    schema_migration: false
    cache_added: false
    ach_cenit_rule_values_changed: false
    tpa9b3_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - ThirdPartyAccountBackfillServiceTests RunAsync_BatchLookahead_DoesNotProcessBeyondRequestedPage_AndResumesFromCheckpoint
    - ThirdPartyAccountBackfillServiceTests RunAsync_IncompleteIdentity_DoesNotInvokeOnboarding
    - ThirdPartyAccountBackfillServiceTests RunAsync_ExistingValidationHistory_ChecksExistenceWithoutMaterializingAttempts
    - ThirdPartyAccountBackfillServiceTests RunAsync_CancellationAfterMasterPersistence_SkipsHistoryAndClearsTracker
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
  next_allowed_phase: TPA9B3

phase_9b2_3_tests_executed:
  - command: TPA9B2.3 targeted four-file static scope/structure guard
    result: PASS

phase_9b2_3_tests_deferred:
  - command: dotnet focal ThirdPartyAccountBackfillServiceTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_9b3_1_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b3_1_administration_security_hardening:
  scope: TPA9B3_1_CANONICAL_ADMINISTRATION_SECURITY_ONLY
  source_package: ACHInterbank-R29-TPA9B2-3-BACKFILL-QUERY-RESUME-EFFICIENCY-HARDENING.zip
  production_code_changed: true
  controller_changed: true
  administration_service_changed: false
  authorization:
    controller_authenticated: true
    read_policy: CanReadAch
    write_policy: CanManageAch
    allow_anonymous_actions: false
    policy_handler_internals_reviewed: false
    policy_handler_reason: OUTSIDE_EXPLICIT_FIVE_FILE_SCOPE
  ownership_idor:
    customer_third_party_read_scope_preserved: true
    customer_third_party_command_scope_preserved: true
    customer_third_party_account_command_scope_preserved: true
    create_account_parent_owner_check_preserved: true
    create_account_resolved_third_party_id_check_preserved: true
    foreign_customer_commands_fail_closed: true
    wrong_parent_account_commands_fail_closed: true
    foreign_attempts_mutate_state: false
  masking_dto:
    document_response_masked: true
    account_response_masked: true
    full_document_serialized: false
    full_account_serialized: false
    raw_normalized_identity_dto_fields_guarded: true
    technical_validation_audit_fields_guarded: true
    dto_shape_expanded: false
  audit_privacy:
    audit_action_scope_preserved: true
    caller_correlation_header_persisted: false
    correlation_source: ASPNETCORE_SERVER_TRACE_IDENTIFIER
    service_structured_logs_include_document: false
    service_structured_logs_include_account: false
    service_structured_logs_include_display_name: false
    ef_audit_pii_suppression_evidence_preserved: true
  error_disclosure:
    argument_exception_message_reflected: false
    conflict_exception_message_reflected: false
    invalid_request_message_generic: true
    not_found_message_generic: true
    conflict_message_generic: true
    known_bounded_non_not_found_administration_codes_preserved: true
    all_not_found_codes_collapsed_to: NOT_FOUND
    unknown_conflict_code: CONFLICT
    cross_customer_owner_details_disclosed: false
    forbidden_403_body_added: false
  exclusions:
    onboarding_changed: false
    eligibility_changed: false
    lifecycle_changed: false
    selector_changed: false
    spa_changed: false
    backfill_changed: false
    schema_migration: false
    gap_tpa_012_cryptography_added: false
    general_refactor: false
    tpa9b3_2_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - ThirdPartyAdministrationApiTests ReadsAndCommands_AreIsolatedByCustomerThirdPartyAndAccount
    - ThirdPartyAdministrationApiTests AuditAndAdministrativeReads_DoNotLeakFullDocumentOrAccountNumbers
    - ThirdPartyAdministrationApiTests Api_UsesExistingReadManagePolicies_AndExposesNoManualApproveRejectCommand
    - ThirdPartyAdministrationApiTests ForeignAndMissingThirdPartyCommands_ReturnIndistinguishableNotFound
    - ThirdPartyAdministrationApiTests CommandErrors_DoNotReflectSensitiveExceptionMessages_AndAuditCorrelationIsServerOwned
    - ThirdPartyAdministrationApiTests AdministrativeDtos_DoNotExposeRawOrTechnicalSensitiveFields
  phase_local_findings_closed:
    - ADMIN_ERROR_EXCEPTION_MESSAGE_AND_UNBOUNDED_CODE_DISCLOSURE
    - CALLER_CONTROLLED_AUDIT_CORRELATION_PII_INGESTION
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
    - docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md
  next_allowed_phase: TPA9B3.2

phase_9b3_1_tests_executed:
  - command: TPA9B3.1 targeted five-file static scope/security guard
    result: PASS

phase_9b3_1_tests_deferred:
  - command: dotnet focal ThirdPartyAdministrationApiTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_9b3_2_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b3_2_revalidation_prenotification_security_hardening:
  scope: TPA9B3_2_REVALIDATION_PRENOTIFICATION_SECURITY_ONLY
  source_package: ACHInterbank-R30-TPA9B3-1-ADMIN-SECURITY-HARDENING.zip
  production_code_changed: true
  revalidation_service_changed: true
  validation_service_changed: true
  controller_changed: false
  authorization:
    revalidation_endpoint_policy: CanManageAch
    allow_anonymous: false
    manual_approve_route: false
    manual_reject_route: false
  ownership_idor:
    full_customer_third_party_account_predicate: true
    foreign_customer_returns_not_found: true
    wrong_parent_returns_not_found: true
    wrong_account_returns_not_found: true
    source_account_rechecked_against_customer: true
    idempotent_replay_bypasses_source_account_ownership: false
  idempotency_security:
    administrative_replay_bound_to_account: true
    administrative_replay_bound_to_clearing_house: true
    administrative_replay_bound_to_transaction_nature: true
    administrative_replay_bound_to_validation_type: true
    administrative_replay_bound_to_effective_date: true
    administrative_replay_bound_to_normalized_service_type: true
    lifecycle_replay_bound_to_effective_date: true
    cross_account_key_reuse_allowed: false
    incompatible_context_key_reuse_allowed: false
    schema_fingerprint_added: false
    cryptographic_fingerprint_added: false
  backend_context_validation:
    source_account_customer_owned: true
    service_type_checked_against_active_company_entry_description_catalog: true
    resolved_policy_must_be_active: true
    resolved_policy_clearing_house_must_match: true
    resolved_policy_transaction_nature_must_match: true
    resolved_policy_effective_from_to_must_cover_request_date: true
    prenotification_not_applicable_admin_request_blocked: true
    undefined_transaction_nature_rejected_before_policy: true
    default_effective_date_rejected_before_policy: true
    inconsistent_lifecycle_policy_context: TechnicalReview
    inconsistent_context_deadline_resolution_called: false
  manual_decision_protection:
    admin_manual_approve_reject_endpoint: false
    transaction_resolution_bridge_remains_automatic: true
    technical_review_can_be_promoted_by_terminal_callback: false
  error_privacy:
    invalid_request_response: GENERIC_400
    ownership_miss_response: GENERIC_OR_PLAIN_404
    conflict_response: GENERIC_409
    exception_message_reflected: false
    document_or_account_reflected: false
    evidence_or_payload_reflected: false
  audit_logging:
    audit_action: ThirdPartyAccount.Revalidation.Request
    correlation_source: ASPNETCORE_SERVER_TRACE_IDENTIFIER
    caller_correlation_header_persisted: false
    structured_logs_include_document: false
    structured_logs_include_destination_account: false
    structured_logs_include_source_account: false
    structured_logs_include_idempotency_key: false
    structured_logs_include_evidence_reference: false
    structured_logs_include_source_file_reference: false
    structured_logs_include_return_detail: false
    request_payload_logged: false
  technical_review_fail_closed:
    missing_policy: true
    missing_required_deadline: true
    inconsistent_policy_context: true
    prenotification_not_applicable_lower_boundary: true
    automatic_terminal_callback_promotion_blocked: true
  exclusions:
    ach_colombia_rules_changed: false
    cenit_rules_changed: false
    general_administration_changed: false
    single_bulk_changed: false
    selector_spa_changed: false
    backfill_changed: false
    schema_migration: false
    cryptography_added: false
    general_refactor: false
    tpa9b3_3_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - ThirdPartyAdministrationApiTests Revalidation_UsesPolicyResolver_IsIdempotent_AndCannotTargetAnotherCustomer
    - ThirdPartyAdministrationApiTests Revalidation_RejectsResolverContextMismatch_AndDoesNotTrustClientContext
    - ThirdPartyAdministrationApiTests Api_UsesExistingReadManagePolicies_AndExposesNoManualApproveRejectCommand
    - ThirdPartyAdministrationApiTests RevalidationEndpoint_UsesGenericErrors_NotFoundAndServerOwnedAuditScope
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_IdempotencyReplay_RejectsEffectiveDateContextMismatch
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_InvalidNatureOrEffectiveDate_IsRejectedBeforePolicyResolution
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_ResolverContextMismatch_RoutesToTechnicalReviewAndSkipsDeadlineResolution
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_NotApplicablePolicy_RoutesToTechnicalReview
    - ThirdPartyAccountValidationLifecycleTests ValidationLifecycle_TechnicalReview_CannotBePromotedToAcceptedOrRejectedByResolutionCallback
  phase_local_findings_closed:
    - REVALIDATION_IDEMPOTENCY_KEY_INCOMPATIBLE_CONTEXT_REUSE
    - PRENOTIFICATION_RESOLVER_CONTEXT_TRUST
    - TECHNICAL_REVIEW_TERMINAL_CALLBACK_PROMOTION
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
    - docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md
  next_allowed_phase: TPA9B3.3

phase_9b3_2_tests_executed:
  - command: TPA9B3.2 targeted seven-file static scope/security guard
    result: PASS

phase_9b3_2_tests_deferred:
  - command: dotnet focal ThirdPartyAdministrationApiTests + ThirdPartyAccountValidationLifecycleTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_9b3_3_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b3_3_backfill_security_hardening:
  scope: TPA9B3_3_BACKFILL_OPERATION_SECURITY_ONLY
  source_package: ACHInterbank-R31-TPA9B3-2-REVALIDATION-PRENOTIFICATION-SECURITY-HARDENING.zip
  production_code_changed: true
  controller:
    authorize_policy: CanManageAch
    anonymous_path_added: false
    reflected_argument_exception_message: false
    invalid_request_code: INVALID_BACKFILL_REQUEST
    invalid_request_message_generic: true
    audit_correlation_source: HttpContext.TraceIdentifier
    caller_correlation_header_persisted: false
  input_validation:
    batch_size_min: 1
    batch_size_max: 1000
    negative_checkpoint_rejected: true
    positive_checkpoint_above_legacy_high_water_rejected: true
    out_of_range_checkpoint_reaches_onboarding: false
    keyset_filter_changed: false
  dry_run:
    onboarding_invoked: false
    canonical_master_written: false
    canonical_validation_or_attempt_written: false
    current_attempt_pointer_repaired: false
    query_evidence_evaluation_preserved: true
  checkpoint_replay:
    checkpoint_semantics: LAST_CONTIGUOUS_CHECKPOINT_SAFE_LEGACY_ID
    replay_overwrites_conflicting_status_or_resolution: false
    linked_history_context_must_match_backend_evidence: true
    linked_context_dimensions:
      - ClearingHouseId
      - TransactionNature
      - ServiceType
      - ValidationType
      - EffectiveDate
    linked_context_mismatch_classification: CANONICAL_LINKED_HISTORY_CONTEXT_CONFLICT
    linked_context_mismatch_repairs_current_attempt: false
    signed_or_persisted_checkpoint_added: false
  cancellation:
    request_entry_check_preserved: true
    per_record_check_preserved: true
    post_master_pre_history_check_preserved: true
    tracker_clear_on_observed_cancellation_preserved: true
    rollback_semantics_added: false
  privacy:
    controller_400_includes_exception_message: false
    failure_logs_include_exception_object_or_message: false
    failure_logs_include_full_account_or_document: false
    failure_logs_include_customer_or_third_party_name: false
    failure_logs_include_evidence_or_payload: false
    summary_logs_aggregate_or_technical_only: true
    issue_payload_internal_id_and_stable_codes_only: true
  exclusions:
    read_cutover_changed: false
    single_bulk_changed: false
    eligibility_lifecycle_changed: false
    selector_spa_changed: false
    schema_migration: false
    cryptography_added: false
    ach_colombia_cenit_rules_changed: false
    general_refactor: false
    tpa9b3_4_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - ThirdPartyAccountBackfillServiceTests RunAsync_DryRun_ReportsWouldCreateWithoutWritingCanonicalRows
    - ThirdPartyAccountBackfillServiceTests RunAsync_RejectsBatchSizeOutsideSecurityBounds
    - ThirdPartyAccountBackfillServiceTests RunAsync_RejectsCheckpointBeyondLegacyHighWaterBeforeCanonicalWrites
    - ThirdPartyAccountBackfillServiceTests RunAsync_DryRun_DoesNotRepairExistingCanonicalHistory
    - ThirdPartyAccountBackfillServiceTests RunAsync_ReplayWithMismatchedCanonicalContext_DoesNotRepairOrRebindHistory
  phase_local_findings_closed:
    - BACKFILL_ARGUMENT_EXCEPTION_MESSAGE_DISCLOSURE
    - BACKFILL_CALLER_CONTROLLED_AUDIT_CORRELATION
    - BACKFILL_OUT_OF_RANGE_CHECKPOINT_ACCEPTANCE
    - BACKFILL_LINKED_HISTORY_CONTEXT_REPAIR
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
    - docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md
  next_allowed_phase: TPA9B3.4

phase_9b3_3_tests_executed:
  - command: TPA9B3.3 targeted five-file static scope/security guard
    result: PASS

phase_9b3_3_tests_deferred:
  - command: dotnet focal ThirdPartyAccountBackfillServiceTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment

phase_9b3_4_status: COMPLETE_STATIC_CHECKS_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b3_4_selector_canonical_read_security_hardening:
  scope: TPA9B3_4_SELECTOR_CANONICAL_READ_SECURITY_ONLY
  source_package: ACHInterbank-R32-TPA9B3-3-BACKFILL-SECURITY-HARDENING.zip
  production_code_changed: true
  authorization:
    controller_authenticated: true
    read_policy: CanReadAch
    anonymous_read_path_added: false
  customer_isolation:
    canonical_list_requires_positive_customer_id: true
    canonical_service_list_requires_positive_customer_id: true
    public_by_id_uses_unscoped_service_lookup: false
    public_by_id_scope:
      - CustomerId
      - ThirdPartyAccountId
    scoped_pair_miss_response: PLAIN_404
    selector_customer_authority: BACKEND_SOURCE_ACCOUNT_TO_CUSTOMER_RESOLUTION
    selector_candidate_filter_customer_scoped: true
    foreign_customer_candidate_evaluated_for_eligibility: false
  selector_masking:
    raw_account_number_returned: false
    raw_document_number_returned: false
    account_mask_shape: LAST4_WITH_FOUR_ASTERISKS_OR_NA
    document_mask_shape: LAST4_WITH_FOUR_ASTERISKS_OR_NA
    canonical_read_existing_masking_preserved: true
    eligibility_code_message_exact_account_document_redaction: true
  context_trust:
    public_canonical_read_accepts_client_eligibility_context: false
    client_context_rejection_code: CLIENT_ELIGIBILITY_CONTEXT_NOT_ALLOWED
    selector_transaction_nature_authority: BACKEND_RESOLVE_FROM_TRANSACTION_TYPE
    selector_service_type_authority: ACTIVE_COMPANY_ENTRY_DESCRIPTION_CATALOG
    selector_clearing_house_authority: BACKEND_ROUTING_STRATEGY
    selector_effective_date_authority: RESOLVED_ACH_CYCLE_PROCESSING_DATE
    selectable_authority: IThirdPartyAccountEligibilityService
    client_selectable_flag_accepted: false
    selectable_decisions:
      - Allowed
      - AllowedWithoutPrenotification
  error_privacy:
    missing_customer_scope_response: GENERIC_400
    manipulated_eligibility_context_response: GENERIC_400
    scoped_account_miss_response: PLAIN_404
    client_search_or_service_type_reflected: false
    raw_document_or_account_reflected: false
    exception_message_reflected: false
    new_conflict_409_path_added: false
  logging_privacy:
    reviewed_production_files_have_application_logger: false
    request_payload_logged: false
    document_logged: false
    account_logged: false
    name_logged: false
    exception_message_logged: false
  exclusions:
    ach_colombia_rules_changed: false
    cenit_rules_changed: false
    single_bulk_changed: false
    administration_changed: false
    backfill_changed: false
    validation_lifecycle_changed: false
    spa_changed: false
    schema_migration: false
    cryptography_added: false
    general_refactor: false
    tpa9b4_executed: false
    tpa10_executed: false
    memory_changed: false
  tests_added_or_updated:
    - TransactionThirdPartyAccountSelectorTests Selector_ResolvesAchColombiaAndCenitContext
    - TransactionThirdPartyAccountSelectorTests Selector_IsolatesCandidatesToCustomerResolvedFromSourceAccount
    - ThirdPartyAccountReadShadowTests CanonicalRead_RequiresPositiveCustomerScope
    - ThirdPartyAccountReadShadowTests CanonicalRead_CustomerScopedQueryCannotReturnForeignAccount
    - ThirdPartyAccountReadShadowTests CanonicalReadController_UsesReadPolicy_RequiresCustomerScope_AndRejectsClientEligibilityContext
    - ThirdPartyAccountReadShadowTests CanonicalReadController_GetByIdScopesLookupByCustomerAndReturnsPlainNotFound
  phase_local_findings_closed:
    - SELECTOR_RAW_BANKING_IDENTITY_EXPOSURE
    - CANONICAL_READ_UNSCOPED_CUSTOMER_IDOR
    - CANONICAL_READ_CLIENT_ELIGIBILITY_CONTEXT_TRUST
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
    - docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md
  next_allowed_phase: TPA9B4

phase_9b3_4_tests_executed:
  - command: TPA9B3.4 targeted seven-file static scope/security guard
    result: PASS

phase_9b3_4_tests_deferred:
  - command: dotnet focal TransactionThirdPartyAccountSelectorTests + ThirdPartyAccountReadShadowTests
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment
  - command: dotnet build ACHInterbank.sln -c Release
    result: NOT_RUN_ENVIRONMENT
    reason: dotnet runtime/SDK is not installed in the supplied execution environment


phase_9b4_status: CLOSED_DOCUMENTATION_READY_RUNTIME_DEFERRED_ENVIRONMENT

phase_9b4_operational_readiness_closure:
  scope: TPA9B4_DOCUMENTATION_AND_OPERATIONAL_READINESS_ONLY
  source_package: ACHInterbank-R33-TPA9B3-4-SELECTOR-CANONICAL-READ-SECURITY-HARDENING.zip
  production_code_changed: false
  schema_migration_changed: false
  ach_colombia_cenit_rules_changed: false
  features_added: false
  tpa10_executed: false
  tpa9_status: CLOSED
  next_allowed_phase: TPA10
  closure_basis:
    static_hardening_record_complete: true
    phase_local_static_guards_documented_pass: true
    runtime_dotnet_execution_during_tpa9: NOT_RUN_ENVIRONMENT
    runtime_certification_deferred_to: TPA10
    documentary_or_architectural_blocker_found: false
  hardening_summary:
    transaction_atomicity_concurrency:
      - SINGLE rollback/cancellation/tracker cleanup and bounded unique-conflict recovery
      - BULK item/chunk savepoint isolation, rollback/cancellation and legacy/canonical concurrency recovery
      - CustomerThirdParty compatibility unique-conflict closure
    lifecycle_eligibility:
      - append-only prenotification attempt protection
      - idempotent replay bound to stable validation context
      - fail-closed TechnicalReview for insufficient required policy/deadline context
      - backend contextual eligibility remains authority
    backfill_read_cutover:
      - checkpoint-safe replay and partial-failure recovery
      - no historical evidence rewrite
      - conservative legacy fallback and observational shadow isolation
    observability:
      single_meter: Cfa.ACHInterbank.ACH.Single
      bulk_meter: Cfa.ACHInterbank.ACH.Bulk
      bounded_metric_tags:
        - outcome
        - stage
        - reason
        - scope
      pii_or_exception_message_metric_labels: false
      telemetry_no_throw_contract: true
    efficiency:
      - bounded selector/canonical-read queries
      - bounded administration queries/commands
      - bounded keyset backfill/resume queries
    security:
      - Customer-scoped authorization and IDOR resistance
      - backend context authority and selector eligibility authority
      - masked document/account response surfaces
      - generic error disclosure and PII-safe audit/logging
      - backfill dry-run/checkpoint/replay integrity
  rollback_readiness:
    pre_tpa9_application_baseline_documented: ACHInterbank-R18-TPA8C-COHERENCE-CLOSURE.zip
    tpa9_database_downgrade_required: false
    legacy_customer_third_party_storage_retained: true
    shadow_retained_non_authoritative: true
    canonical_history_must_not_be_deleted_or_rewritten: true
    backfill_resume_authority: LAST_CONTIGUOUS_CHECKPOINT_SAFE_LEGACY_ID
  backfill_operational_contract:
    endpoint_policy: CanManageAch
    dry_run_first: true
    batch_size_min: 1
    batch_size_max: 1000
    negative_checkpoint_rejected: true
    checkpoint_above_legacy_high_water_rejected: true
    dry_run_writes_canonical_master_or_history: false
    replay_rewrites_conflicting_canonical_history: false
    cancellation_propagates: true
  selector_read_operational_contract:
    read_policy: CanReadAch
    positive_customer_scope_required: true
    by_id_customer_scoped: true
    raw_document_account_exposed: false
    client_eligibility_context_authoritative: false
    selector_customer_authority: BACKEND_SOURCE_ACCOUNT_TO_CUSTOMER
    selector_eligibility_authority: BACKEND_CONTEXTUAL_ELIGIBILITY
  technical_review_contract:
    manual_approve_reject_allowed: false
    insufficient_required_context: FAIL_CLOSED_TECHNICAL_REVIEW
    terminal_attempt_evidence_append_only: true
  gaps_closed: []
  gaps_remaining:
    - GAP-TPA-001
    - GAP-TPA-008
    - GAP-TPA-012
    - GAP-TPA-013
    - GAP-TPA-016
  runtime_risks_deferred_to_tpa10:
    - focal .NET tests/build were not executable in the supplied TPA9 environment
    - relational provider/savepoint/unique-conflict behavior requires PostgreSQL and SQL Server runtime verification
    - telemetry provider emission/no-throw/privacy requires runtime verification
    - end-to-end authorization, IDOR, masking and generic-error HTTP behavior requires deployment-environment smoke verification
    - backfill dry-run/apply/checkpoint/resume/cancellation requires representative-data runtime verification
    - rollback procedure requires a deployment/certification drill or explicit operational approval
  tpa10_verification_scope:
    - Release build and all deferred focal TPA9 tests
    - PostgreSQL and SQL Server transaction/savepoint/concurrency/cancellation behavior
    - SINGLE/BULK metric and structured-log privacy/cardinality
    - backfill dry-run/apply/checkpoint/resume/cancellation/history-conflict behavior
    - CanReadAch/CanManageAch and Customer isolation/IDOR at HTTP boundary
    - masked DTO/error/audit/log privacy verification
    - TechnicalReview fail-closed and terminal append-only evidence behavior
    - selector/canonical-read backend context authority and foreign-Customer rejection
    - application rollback drill with no TPA9 schema downgrade
  documentation:
    - docs/architecture/third-party-account-tpa9-hardening.md
    - docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md
    - docs/runbooks/third-party-account-tpa9-operations.md
    - MEMORY.md

phase_9b4_tests_executed:
  - command: TPA9B4 documentation/state/runbook consistency guard
    result: PASS
  - command: TPA9B4 package delta guard
    result: PASS

phase_9b4_tests_deferred:
  - command: dotnet focal TPA9 runtime suites + Release build
    result: NOT_RUN_ENVIRONMENT
    reason: TPA9B4 is documentation-only and the supplied environment has no dotnet runtime/SDK; runtime certification is the explicit TPA10 scope
  - command: PostgreSQL and SQL Server provider/runtime verification
    result: NOT_RUN_ENVIRONMENT
    reason: provider/runtime certification is the explicit TPA10 scope

behavioral_diff: NONE_TPA9B4_DOCUMENTATION_AND_OPERATIONAL_READINESS_CLOSURE_ONLY
normative_diff: NONE_TPA9B4_DOES_NOT_CHANGE_PRODUCTIVE_CODE_SCHEMA_ACH_CENIT_RULES_OR_EXISTING_TPA9_BEHAVIOR
blocked_by: []
environment_constraints:
  - dotnet runtime/SDK is unavailable; TPA9 runtime focal tests/build remain deferred as NOT_RUN_ENVIRONMENT and are part of TPA10 verification
  - PostgreSQL/SQL Server deployment-provider verification was not executed in TPA9B4 and is part of TPA10
next_action: TPA9=CLOSED. NEXT_ALLOWED_PHASE=TPA10. Execute runtime/provider/deployment verification only in TPA10; do not treat deferred runtime certification as already passed.
```

## Baseline fingerprints

`docs/ai/third-party-accounts/TPA0_BASELINE_INVENTORY.json` was revalidated against the source archive before TPA0 edits. The detailed characterization and GAP evidence is in `docs/architecture/third-party-account-tpa0-baseline.md`.
