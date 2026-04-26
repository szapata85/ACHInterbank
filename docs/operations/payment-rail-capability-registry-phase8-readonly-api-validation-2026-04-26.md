# Validación técnica — Prompt 8 API administrativa read-only Capability Registry (2026-04-26)

## Alcance

Se agrega API administrativa de **solo lectura** para gobernanza multi-riel del capability registry.

Se mantiene explícitamente:

- legacy como source of truth operativo;
- PaymentRail en paralelo/shadow;
- sin cutover;
- sin cambios funcionales ACH/CENIT;
- sin cambios criptográficos.

## Endpoints agregados

Base route: `api/payment-rails/capability-registry`

- `GET /rails`
  - Lista rieles disponibles para gobernanza.
- `GET /rails/{railCode}/capabilities?asOfUtc=`
  - Lista capacidades efectivas por riel.
- `GET /rails/{railCode}/capabilities/{capabilityCode}?asOfUtc=`
  - Consulta una capability específica por riel.

## Cobertura de datos de consulta

Cada capability expone:

- `RailCode`;
- `CapabilityCode`;
- `State` (`Enabled|Disabled|ShadowOnly|NotSupported|Planned`);
- `Source` (`RegistryOverride|StrategyDefault`);
- `EffectiveFromUtc`/`EffectiveToUtc`;
- `Version`;
- metadata auditable segura (`ChangeSource`, `ChangeTicket`, `ChangedBy`, `ChangedAtUtc`) cuando aplica override de registry.

## Seguridad

- Endpoint protegido con autorización.
- Política fina: `CanViewPaymentRailCapabilityRegistry` con fallback a `CanManageAch` y `CanReadAch`.

## Validación ejecutada

```bash
dotnet build ACHInterbank.sln -c Release
```

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRailCapabilityRegistryServiceTests"
```

## Conclusión

Prompt 8 implementado en modo gobernanza read-only:

- API administrativa de consulta habilitada;
- sin endpoints de edición;
- sin alteración de decisiones operacionales legacy.
