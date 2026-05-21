# Barrido AG Grid SPA

Fecha: 2026-05-20  
Ambiente: Angular SPA  
Productivo: NO-GO

## Alcance

Se reviso `web/ach-interbank-ui/src` buscando:

- `checkboxSelection`
- `headerCheckboxSelection`
- `headerCheckboxSelectionFilteredOnly`
- `rowSelection`
- `AgGridAngular`
- `GridOptions`
- `ColDef`
- `columnDefs`

## Hallazgos

| Componente | Estado | Accion |
|---|---|---|
| `shared/components/ui/ui-grilla-empresarial.component.ts` | OK | Usa `RowSelectionOptions` centralizado |
| `transactions/components/ach-returns-management` | Corregido | Se removieron flags deprecados en `ColDef` y se migro a `rowSelection` v32.2+ |
| Pantallas que usan `ui-grilla-empresarial` sin flags de seleccion | Sin cambio | No tenian propiedades deprecadas |
| Pantallas AG Grid read-only | Sin cambio | No requieren checkbox/header checkbox |

## Configuracion aplicada

Para gestion de devoluciones se conserva seleccion multiple, checkbox por fila elegible, checkbox de header y seleccion filtrada mediante:

```ts
rowSelection: {
  mode: 'multiRow',
  checkboxes: (params) => !!params.data?.isEligible,
  headerCheckbox: true,
  selectAll: 'filtered',
  isRowSelectable: (rowNode) => !!rowNode.data?.isEligible
}
```

## Validacion

- `rg` no encontro `checkboxSelection`, `headerCheckboxSelection` ni `headerCheckboxSelectionFilteredOnly` en `web/ach-interbank-ui/src`.
- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK.

No se cambiaron endpoints, modelos funcionales ni reglas ACH/NACHA-M/CENIT/ROR.
