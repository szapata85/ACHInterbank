"""I05 static invariants. NOT compilation, tests of DB semantics or runtime certification."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
read = lambda p: (root / p).read_text(encoding='utf-8-sig')
service = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.TacitAcceptance.cs')
job = read('src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs')
models = read('src/Cfa.ACHInterbank.Application/ACH/Models/NachaInboundSimulationModels.cs')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs')
interface = read('src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaInboundSimulationService.cs')
policy = read('src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationTacitAcceptanceDecision.cs')
tests = read('tests/Cfa.ACHInterbank.Tests/PrenotificationSimulatorTacitAcceptanceI05Tests.cs')
checks = {
    'preview route': 'prenotifications/tacit-acceptance/preview' in controller,
    'apply route': 'prenotifications/tacit-acceptance/apply-due' in controller,
    'preview permission': controller.count('P1Policies.NachaSimulatorRead') >= 2,
    'apply differential permission': controller.count('P1Policies.NachaSimulatorGenerateDifferential') >= 2,
    'preview API': 'PreviewTacitAcceptanceAsync' in interface,
    'apply API': 'ApplyDueTacitAcceptanceAsync' in interface,
    'clock is read only': 'IsVirtualTime' in models and 'bool ReadOnly' in models,
    'virtual time range limit': 'SIMULATED_TIME_OUT_OF_RANGE' in service,
    'virtual preview uses no state transition': '_stateTransitionService.TransitionAsync' not in service.split('public async Task<PrenotificationTacitAcceptanceApplyResponse>')[0],
    'real time from provider': 'var realNow = _timeProvider.GetUtcNow();' in service,
    'job uses provider': 'var utcNow = _timeProvider.GetUtcNow();' in job,
    'job still uses canonical deadline': 'x.ResponseDeadlineAtUtc.Value <= utcNow' in job,
    'job protects effective policy': 'x.ThirdPartyAccountValidation.PolicyRule.TacitAcceptanceEnabled' in job,
    'job compatibility legacy': 'legacyCandidates' in job,
    'no NACHA file in apply': 'false, false);' in service,
    'apply uses physical clock': 'var now = DateTimeOffset.UtcNow;' in service,
    'apply cross checks actual deadline': 'x.DeadlineAtUtc.Value > now' in service,
    'apply non-production gate': '_hostEnvironment.IsProduction()' in service,
    'apply UAT gate': '_options.IsUatLike()' in service,
    'apply no external': '_options.AllowExternalTransmission' in service,
    'apply no autoimport': '_options.AllowAutoImport' in service,
    'apply transaction': 'IsolationLevel.Serializable' in service,
    'apply revalidates': 'PreviewTacitAcceptanceAsync(new()' in service,
    'uses official state service': '_stateTransitionService.TransitionAsync' in service,
    'state target tacit': 'AchTransferStateEnum.AppliedTacitly' in service,
    'prohibits duplicate prior return': 'RETURN_SIMULATION_ALREADY_GENERATED' in service,
    'requires rule enabled': 'TACIT_POLICY_DISABLED' in service,
    'requires real deadline': 'EligibleAtRealTime' in service,
    'requires matching original bank': 'RespondingFinancialInstitutionId' in service,
    'requires matching business date': 'TACIT_BUSINESS_DATE_MISMATCH' in service,
    'no mixed terminal': 'TACIT_MIXED_TERMINAL_STATES' in service,
    'stable idempotency': 'uat-prenotification-tacit:{id}:{attempt}' in service,
    'pure decision': 'public static bool IsDue' in policy,
    'deadline exact boundary tests': 'Deadline.AddTicks(-1)' in tests,
    'generated return block tested': 'GeneratedReturnBlocksTacit' in tests,
    'contract no fictitious response': 'NO_RETURN_HAS_NO_RESPONSE_CYCLE' in tests,
}
for name, passed in checks.items():
    print(f'{"PASS_STATIC" if passed else "FAIL"}: {name}')
print(f'RESULT: {sum(checks.values())}/{len(checks)} PASS_STATIC')
raise SystemExit(0 if all(checks.values()) else 1)
