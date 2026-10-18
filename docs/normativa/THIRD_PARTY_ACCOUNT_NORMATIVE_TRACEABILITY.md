# Matriz normativa — Terceros, cuentas y prenotificaciones

**Programa:** `TPA-GOV-001`  
**Autoridades del baseline:** ACH Colombia V35 (abril 2026); CENIT DSP-152/Anexo 2; Manual NACHA-M CENIT (7 mayo 2026).

> Esta matriz traduce las fuentes al diseño. No reemplaza los documentos normativos del proyecto.

## 1. Regla arquitectónica derivada

Las fuentes describen Originadores/Receptores y sus cuentas, y regulan la prenotificación como una transacción de validación. **No establecen que la existencia del tercero o de su cuenta dependa de una cámara.** Por tanto, el maestro `Customer -> ThirdParty -> ThirdPartyAccount` es genérico; ACH Colombia/CENIT aparecen al validar o procesar una operación.

| Regla | ACH Colombia V35 | CENIT | Implicación |
|---|---|---|---|
| Originador se relaciona con múltiples receptores/cuentas | Definiciones y esquema 2.1.1 | Reglamento Originador/Receptor/Cuenta | Maestro genérico con múltiples cuentas por tercero. |
| Datos de cuenta receptora | 2.1.1.1; 6.1.11 | NACHA-M tipo 6 y 4.4 | Entidad, tipo, número e identidad se modelan como master/contexto de transacción. |
| Prenotificación | 2.10.3 / 2.11.4 | Terminología NACHA-M y Anexo 2 | Historial de validación separado del maestro. |

## 2. Crédito

### ACH Colombia V35

- 2.10.2/2.10.3: prenotificación crédito opcional/discrecional.
- 2.10.3.1: ausencia de devolución en el plazo se entiende exitosa.
- 2.10.3.2: cuando se usa, el manual indica mínimo tres días hábiles antes de la primera transacción crédito.
- 6.1.11.3: validación de identificación en crédito es opcional.

**Diseño:** una cuenta puede existir y ser usada para crédito sin crear validación cuando la policy efectiva no la exige. Si se solicita prenotificación, se crea un contexto/intento asociado a esa cuenta.

### CENIT

- Anexo 2, 4.7: antes de una Entrada Crédito el Originador **podrá** enviar prenotificación; no es obligatoria.
- Manual NACHA-M 4.4.2: receptor valida tipo+número de cuenta, estados y la identificación cuando se solicita.

**Diseño:** `AllowedWithoutPrenotification` es una decisión contextual posible. No se persiste `NotRequired` como estado universal de la cuenta.

## 3. Débito

### ACH Colombia V35

- 2.11.4: la Entidad Participante Originadora debe exigir prenotificación para cada Usuario Receptor y garantizar el soporte de autorización.
- 2.11.4.1: ausencia de devolución en horarios/ciclos definidos se entiende exitosa.
- 2.11.4.2: al menos tres días hábiles antes de la primera transacción débito; operativamente puede enviarse monetaria una vez transcurridos cuatro ciclos si no fue rechazada.
- 6.1.11.1/6.1.11.4: identificación obligatoria en prenotificación y monetaria débito.

**Diseño:** elegibilidad débito falla cerrado hasta que el contexto cumpla la policy. El soporte de autorización es prerequisito separado del simple alta de cuenta.

### CENIT

- Anexo 2, 4.7: prenotificación obligatoria antes de Entrada Débito.
- 4.8: devolución dentro de la misma Fecha Valor, máximo último ciclo; vencido el plazo sin devolución, el Originador queda autorizado.
- NACHA-M 4.4.3: tipo+número, identificación vs cuenta obligatoria, estados y adenda.
- NACHA-M 4.4.4: cuando se administra base de prenotificados, comparación prenote vs monetaria; ausencia/mismatch puede conducir a R10 según el caso.

**Diseño:** deadline/madurez CENIT se resuelve con su policy/calendario/ciclos, no con `+3 días` global.

## 4. Causales relevantes

| Causal | Significado | Implicación |
|---|---|---|
| R02 | Cuenta cerrada | Intento rechazado; master se conserva. |
| R03 | Cuenta no abierta | Intento rechazado; revisar datos. |
| R04 | Número de cuenta inválido | Corregir master y crear nuevo intento si corresponde. |
| R10 | No existe prenotificación/autorización | No habilitar débito en contexto que la exija. |
| R16 | Cuenta inactiva/bloqueada | No elegible/rechazo según transacción. |
| R17 | Identificación no coincide con cuenta | Corregir identidad/cuenta; nuevo intento. |
| R20 | Cuenta no habilitada | Contexto no elegible hasta nueva condición. |
| R31 | Prenotificación débito sin información requerida | Corregir payload/adenda; master no se duplica. |
| R32 | Crédito no procesado por información requerida | Resultado contextual. |
| R35 | Tipo de cuenta errado | Corregir `ThirdPartyAccount.AccountType`; invalidar contextos afectados. |

La causal real proviene del catálogo/resultado existente; esta tabla no autoriza hardcodes.

## 5. Conservación/expiración

CENIT NACHA-M 4.4.3 establece conservación de prenotificaciones débito hasta trece meses después de la última monetaria; cumplida la condición se repite autorización/prenotificación. Esto pertenece a `ThirdPartyAccountValidation`/policy (`LastMonetaryUseAtUtc`, expiración derivada), **no a `ThirdPartyAccount`**. No extrapolar esta regla a ACH Colombia sin fuente.

## 6. Gap de metadata del baseline

`RegulatoryCatalogSeeder` conserva referencias textuales V32 para reglas ACH Colombia. TPA0 debe comparar valores contra V35 y separar actualización de metadata de cualquier cambio real de comportamiento.

## 7. Precedencia

1. normativa vigente aplicable;
2. decisiones durables en `MEMORY.md`;
3. configuración/reglas efectivas del sistema;
4. esta matriz de diseño.
