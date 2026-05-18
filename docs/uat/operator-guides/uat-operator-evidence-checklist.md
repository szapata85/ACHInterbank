# Checklist operativo de evidencias UAT

## 1. Evidencia mínima por caso

| ID caso | Dominio S1 | Cámara | Fecha | Usuario ejecutor | Dato usado | Evidencia capturada | Hash o referencia | Ubicación segura | Datos enmascarados | Resultado esperado | Resultado obtenido | Estado | Defectos | Aprobador |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
|  |  |  |  |  |  |  |  |  |  |  |  | Pendiente |  |  |

## 2. Tipos de evidencia aceptados
- captura enmascarada;
- PDF de reporte;
- CSV/Excel exportado;
- nombre de archivo;
- hash;
- referencia interna;
- acta;
- correo de aprobación;
- soporte externo en ubicación segura;
- bitácora de ejecución.

## 3. Evidencia no aceptada
- captura sin fecha/contexto;
- archivo sin hash o referencia;
- evidencia con datos sensibles visibles;
- aprobación verbal sin trazabilidad;
- soporte externo sin responsable;
- saldos CUD visibles sin autorización;
- certificados privados;
- PFX;
- passwords;
- llaves privadas.

## 4. Reglas de protección de datos
- No subir datos sensibles al repositorio.
- Usar datos anonimizados o enmascarados.
- No exponer cuentas o identificaciones completas.
- Registrar hash o referencia interna en lugar de datos completos.
- Guardar soportes sensibles en ubicación segura aprobada.
