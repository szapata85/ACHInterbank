# Validación Registro Tipo 5 ACH – Fecha de Compensación Juliana

Este documento define la validación aplicada sobre las posiciones **80-82** del Registro Tipo 5 (Encabezado de Lote) bajo el formato NACHA-M.

## Reglas implementadas

- **Identificación del registro**: debe iniciar con `5` en la posición 1.
- **Longitud del registro**: el Registro Tipo 5 debe conservar longitud fija de `106` caracteres.
- **Campo Fecha de Compensación Juliana (80-82)**:
  - Longitud de `3` caracteres.
  - Opcional.
  - Si el valor está diligenciado, debe ser estrictamente numérico.
  - Si contiene caracteres no numéricos, se debe detener el proceso por riesgo de **Error Fatal 65**.
  - Si llega nulo o vacío, se insertan `3` espacios en blanco.
  - Si llega numérico, se alinea a la derecha y se completa con ceros a la izquierda (`1` → `001`).
  - Se valida rango operativo `001` a `366`.

## Componente técnico

Se implementó el helper:

- `BatchHeaderType5JulianDateValidator.ValidateAndFormat(...)`
- `BatchHeaderType5JulianDateValidator.ApplyToType5Record(...)`

Ubicación:

- `src/Cfa.ACHInterbank.Application/Helpers/ACH/BatchHeaderType5JulianDateValidator.cs`

## Resultado esperado

Con este control se evita el rechazo total del archivo por formato inválido de fecha juliana, especialmente por la validación fatal asociada al **Error 65** cuando el campo contiene alfanuméricos o símbolos.
