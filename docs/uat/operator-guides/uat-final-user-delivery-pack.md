# Paquete de entrega final para usuarios UAT — PDF y Excel

## 1. Entregables finales esperados

PDF:
`UAT_ACHInterbank_Guia_Operativa_Usuarios.pdf`

Excel:
`UAT_ACHInterbank_Set_Pruebas_Operativas.xlsx`

Aclaraciones:
- En este commit solo se versionan fuentes Markdown.
- El PDF y Excel son entregables derivados para distribución.
- No subir datos reales sensibles en el Excel.
- Si se generan versiones diligenciadas, deben guardarse en ubicación documental segura aprobada.

## 2. Contenido del PDF
- guía de ejecución;
- reglas de protección de datos;
- explicación de estados;
- guía de evidencias;
- guía de defectos;
- guía de aprobación/firma;
- advertencia NO-GO productivo.

## 3. Estructura del Excel
Hojas obligatorias:
1. Instrucciones
2. Casos_UAT
3. Evidencias
4. Defectos
5. Aprobadores
6. Resumen_Ejecucion
7. Scorecard_UAT

## 4. Columnas por hoja

### Instrucciones
- Campo.
- Descripción.
- Responsable.

### Casos_UAT
- ID Caso.
- Dominio S1.
- Cámara.
- Nombre del caso.
- Objetivo.
- Datos requeridos.
- Pasos operativos.
- Resultado esperado.
- Resultado obtenido.
- Estado.
- Evidencia requerida.
- ID evidencia.
- Defecto asociado.
- Aprobador.
- Observaciones.

### Evidencias
- ID Evidencia.
- ID Caso.
- Dominio S1.
- Cámara.
- Tipo de evidencia.
- Descripción.
- Hash / referencia.
- Ubicación segura.
- ¿Datos enmascarados?.
- Responsable.
- Fecha.
- Estado.
- Observaciones.

### Defectos
- ID Defecto.
- ID Caso.
- Dominio S1.
- Cámara.
- Descripción.
- Resultado esperado.
- Resultado obtenido.
- Severidad.
- Impacto operativo.
- ¿Bloquea aprobación?.
- Responsable.
- Fecha objetivo.
- Estado.
- Workaround.
- Observaciones.

### Aprobadores
- Rol.
- Nombre.
- Área.
- Dominio que aprueba.
- Decisión.
- Fecha.
- Firma / trazabilidad.
- Observaciones.

### Resumen_Ejecucion
- Total casos.
- Pendientes.
- En ejecución.
- Aprobados.
- Aprobados con observaciones.
- Rechazados.
- Bloqueados.
- P0 abiertos.
- P1 abiertos.
- P2/P3 abiertos.
- GO UAT formal.
- GO productivo.

### Scorecard_UAT
- Dominio.
- Bloqueante.
- Estado.
- Evidencia mínima.
- Aprobación requerida.
- Decisión.
- Observaciones.

## 5. Estados permitidos en Excel
- Pendiente.
- En ejecución.
- Aprobado.
- Aprobado con observaciones.
- Rechazado.
- Bloqueado.

## 6. Severidades permitidas
- P0.
- P1.
- P2.
- P3.

## 7. Regla final
El Excel diligenciado no habilita producción por sí solo.
Debe existir acta humana, evidencia suficiente y scorecard actualizado.
