# ExternalFileNameSequence multi-provider refactor (2026-04-20)

## Problema inicial

La reserva de secuencia de `ExternalFileName` estaba acoplada a PostgreSQL dentro de una clase genérica (`ExternalFileNameSequenceService`) que mezclaba:

- lógica EF genérica,
- detección de provider (`if provider contains Npgsql`),
- SQL nativo PostgreSQL (`ON CONFLICT ... RETURNING`).

Ese diseño dificultaba escalar a SQL Server sin tocar la clase central.

## Patrón elegido

Se aplicó **Ports & Adapters (Hexagonal) + Resolver**:

- **Puerto de aplicación** estable: `IExternalFileNameSequenceService` (sin dependencias de provider).
- **Servicio de orquestación**: `ExternalFileNameSequenceService` que delega en un resolver.
- **Adapters por proveedor**:
  - `PostgresExternalFileNameSequenceService`
  - `SqlServerExternalFileNameSequenceService`
  - `EfGenericExternalFileNameSequenceService` (fallback)
- **Resolver de adapters**: `ExternalFileNameSequenceProviderResolver`.

## Por qué este patrón

- Evita `if(provider)` dentro de la clase operacional de negocio.
- Encapsula SQL nativo PostgreSQL en un adapter dedicado.
- Permite agregar SQL Server real como nuevo adapter sin cambiar el puerto.
- Mantiene limpia Application/Domain de referencias a Npgsql.

## Diseño final

1. `IExternalFileNameSequenceService` sigue siendo el contrato consumido por la policy/builder.
2. `ExternalFileNameSequenceService` obtiene `Database.ProviderName` desde `AchDbContext`, resuelve adapter y delega la reserva.
3. `PostgresExternalFileNameSequenceService` implementa el upsert atómico:
   - `INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING`.
4. `SqlServerExternalFileNameSequenceService` es placeholder controlado:
   - lanza `NotSupportedException` explícita.
5. `EfGenericExternalFileNameSequenceService` implementa fallback con EF.

## Ubicación de Npgsql

`Npgsql` queda exclusivamente en:

- `PostgresExternalFileNameSequenceService`.

No se usa en Application ni Domain.

## Cómo agregar SQL Server real

Reemplazar el placeholder actual `SqlServerExternalFileNameSequenceService` por implementación concreta con estrategia de concurrencia robusta (ej. `MERGE`/`UPDLOCK` + `OUTPUT`) y mantener el mismo contrato `IExternalFileNameSequenceProvider`.

## Limitaciones

- El adapter SQL Server está marcado como no implementado (controlado).
- El fallback EF puede no ser óptimo para escenarios multi-instancia de alta concurrencia.

## Pruebas ejecutadas

- Build Release de solución.
- `ExternalFileName` filter.
- No regresión núcleo (60/60).
- No regresión amplia NACHA (154/154).

## Riesgos residuales

- Falta implementación SQL Server concurrente optimizada para producción.
- Fallback EF debe usarse con cautela en despliegues multi-instancia.
