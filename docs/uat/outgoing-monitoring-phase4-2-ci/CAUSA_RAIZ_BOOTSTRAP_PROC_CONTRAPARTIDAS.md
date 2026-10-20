# Causa raiz - bootstrap de Proc_Contrapartidas

## Diff responsable

El cambio entre `eb15a5938f82e927bbbe43f9acf86b6bb59cb640` y `64f05fef108c6e76881f04a77e0f95dc3cb16629` publico como mapping base:

- `OFDD` como constante `TRANSFER`;
- `OFMONDEB` con default `0`;
- `OFIDTX` como constante `0`;
- `OFIDEBAPLI` como constante `1`;
- otros campos funcionales con defaults genericos.

Readiness aceptaba expresamente esas constantes o degradaba defaults a advertencias. El resolver, a su vez, convertia fuentes ausentes a cadena vacia, entero cero o decimal cero. Las tres capas formaban una causa comun: un mapping publicado incompleto parecia utilizable y resolvia la referencia como `0`.

No se observo una diferencia Linux/Windows en los tres fallos: se reprodujeron localmente en ejecucion serial con el mismo comportamiento de CI.

## Correccion

La definicion canonica usa:

- `OFCTA` desde `transaction.sourceAccountNumber`;
- `OFDD` desde `transaction.debitCreditIndicator`;
- `OFMONDEB` desde `transaction.amount` sin default;
- `OFIDTX` desde `transaction.reference` sin default;
- IDs de archivo/lote desde `batch.id` sin default;
- `OFIDEBAPLI` desde `transaction.id`;
- `OFIDCAMCOMPE` desde `clearinghouse.id`.

Solo permanecen constantes contractuales explicitas: componente credito no aplicable `0`, estado `OO`, reverso `0` e IP tecnica local `0.0.0.0`. Esta ultima produce una advertencia no financiera.

## Reparacion e idempotencia

El bootstrap identifica ownership del sistema por `MethodId`, estado publicado/activo y `PublishedBy=seed`. Repara las reglas en sitio, conserva el `MappingSetId` y los IDs de reglas cuando es posible, agrega historial y archiva duplicados inequívocamente pertenecientes al sistema.

Si existe cualquier mapping publicado que no pertenece al bootstrap, retorna sin sobrescribirlo. Las pruebas verificaron primera, segunda y tercera ejecucion, estabilidad de identificadores, ausencia de duplicados, reparacion del seed antiguo y preservacion de mappings de usuario.
