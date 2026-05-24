# Fase 6A - Evaluacion separacion ACH Colombia vs CENIT

| Camara | Existe `CatClearingHouse` | Perfil publicado | Vigente | Registros 1/5/6/7/8/9 | Uso en generacion actual | Estado |
|---|---|---|---|---|---|---|
| ACH Colombia | Si, code `ACH` | Parcial por backfill legacy | Parcial | Parcial si legacy tiene todos los layouts | Solo si modo/flags usan resolver; default legacy | Parcial |
| CENIT | Si, code `CENIT` | No evidenciado | No evidenciado | No evidenciado | Seleccion por nombre de clearing house, pero sin perfil completo | Bloqueado |

| Pregunta | Respuesta |
|---|---|
| ACH Colombia y CENIT comparten perfil | No deberian; hoy no se evidencio perfil CENIT completo |
| Tienen perfiles separados | Parcial/no evidenciado |
| Se puede cambiar ACH sin afectar CENIT | No garantizado por tests ni seeds oficiales |
| Se puede cambiar CENIT sin afectar ACH | No garantizado |
| Que falta | Perfiles publicados completos por camara, tests de aislamiento y resolucion estricta por `CatClearingHouse` |

La seleccion actual de camara en builder usa deteccion por nombre que contiene `CENIT`, y si no usa `ACH`. Esta logica debe reemplazarse o blindarse con identificador/codigo de camara controlado para garantizar aislamiento normativo.

