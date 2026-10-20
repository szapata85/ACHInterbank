# JOB 5 — Matriz de evidencia normativa

Fecha de corte: 2026-07-23

| Cámara | Perfil | Regla | Fuente | Versión | Página/sección | Archivo | Clasificación | SHA-256 | Estado | Observación |
|---|---|---|---|---|---|---|---|---|---|---|
| ACHCOL | Diferencial devolución entrada, no publicado | Secuencia física 1/5/6/7/8/9 y devolución única | Manual de Servicio ACH Transferencias Interbancarias | Portada V32; sección V31 | 6.6.1, págs. internas 162–163 | `docs/normativa/pdf/ACH-Colombia-V32.pdf` | Official | `D83585B53B31A3A70E4861412F48FF8306ED0D2F439A7443C05E540F6B5736EE` | NO-GO | La discrepancia V32/V31 impide fijar versión y vigencia contractual. |
| ACHCOL | Diferencial devolución entrada, no publicado | Addenda 99, causal 4–6, traza original 7–21, 106 caracteres | Manual de Servicio ACH Transferencias Interbancarias | Sección V31, agosto 2024 | 6.6.2, pág. interna 169 | `docs/normativa/pdf/ACH-Colombia-V32.pdf` | Official | `D83585B53B31A3A70E4861412F48FF8306ED0D2F439A7443C05E540F6B5736EE` | NO-GO | Regla identificable, pero sin vector oficial/referencia verificada ni confirmación de aplicabilidad a CFA. |
| ACHCOL | Diferencial devolución entrada, no publicado | Bytes, encoding, terminador y naming diferencial | Evidencia local | N/D | N/D | No disponible | — | — | NO-GO | Falta archivo diferencial oficial o referencia real verificada con propósito, origen y versión. |
| ACHCOL | Fixture de retorno existente | Regresión sintética de retorno | Suite interna | Sin versión normativa demostrada | TestData | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Returns/ACH_COL_RET_001.RET` | SyntheticFixture | `FDE736A96C1C24BE0392E1E56BDD71E4910B63B287D5E8C47479A93AFD7B96EE` | NO HABILITA | No es golden oficial ni referencia verificada; se conserva sin cambios. |
| CENIT | Diferencial devolución entrada, no publicado | Reglas operativas de devolución y prenotificación | Reglamento operativo CENIT DSP-152 Anexo 2 | Documento local sin layout STA incluido | 2, 4.2–4.8; remisión STA en pág. transcrita 1050 | `docs/normativa/pdf/CENIT-DSP-152-Anexo-2.pdf` | Official | `AD6BB2FC48CCF78CE0BDB980BBFFFCAF9D42E52882CC16559A9336F41CFC902D` | NO-GO | El documento remite al Manual de Especificaciones STA, ausente. |
| CENIT | Catálogo causal, perfil no publicado | Causales de devolución | Banco de la República, Anexo A | Documento local | Documento completo | `docs/normativa/pdf/CENIT-Anexo-A-Causales-Devolucion.pdf` | Official | `D3A8F12EC49876CBFF516DAA3A1651693C5DAE0D75DC2DF8FFE733C1A8A00EFE` | PARCIAL | Sustenta catálogo, no posiciones, encoding, controles ni nombre de archivo. |
| CENIT | Catálogo causal, perfil no publicado | Causales de rechazo | Banco de la República, Anexo B | Documento local | Documento completo | `docs/normativa/pdf/CENIT-Anexo-B-Causales-Rechazo.pdf` | Official | `ADF05ED85BF8EF136C61A1560D073EB6B375D50571530DC679164AF78C2A530A` | PARCIAL | Sustenta catálogo, no layout diferencial. |
| CENIT | Diferencial devolución entrada, no publicado | Layout físico, encoding, terminador, controles y naming | Manual de Especificaciones STA | No disponible | No disponible | No disponible | — | — | NO-GO | Se requiere el manual aplicable y su versión/vigencia. |
| CENIT | Fixture de retorno existente | Regresión sintética de retorno | Suite interna | Sin versión normativa demostrada | TestData | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/CENIT/Returns/CENIT_RET_001.RET` | SyntheticFixture | `047FAB8F6A35A4E063974C0DABE9F736CD90FD0840E75AB672C831BFDC40CA95` | NO HABILITA | No es golden oficial ni referencia verificada; se conserva sin cambios. |

## Resultado por cámara

| Cámara | Código de perfil ejecutable | Finalidad | Dirección | Proceso | Resultado |
|---|---|---|---|---|---|
| ACHCOL | Ninguno | Respuesta/devolución diferencial | Entrada | RETORNO | **NO-GO NORMATIVO** |
| CENIT | Ninguno | Respuesta/devolución diferencial | Entrada | RETORNO | **NO-GO NORMATIVO** |

No se copió un perfil entre cámaras, no se publicaron placeholders y no se infirieron reglas desde los fixtures.
