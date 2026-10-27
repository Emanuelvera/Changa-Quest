---
title: 'Reportar contenido o una interacción'
type: 'feature'
created: '2026-10-25'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '4658129'
context:
  - '{project-root}/docs/METODOLOGIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Una persona usuaria no tiene cómo reportar una changa o una interacción que considera inapropiada, y no existe un lugar donde la revisión administrativa pueda recibir esos reportes.

**Approach:** Registrar reportes autenticados (changa reportada, motivo obligatorio y evidencia opcional como texto o enlace) que no se publican a otras personas, y exponer su lectura solo a cuentas con el rol `Administrator`. Versión mínima de API: sin subida de archivos y sin pantalla de revisión (queda para el producto real; PRD OQ-5).

## Boundaries & Constraints

**Always:** Solo cuentas adultas con correo verificado pueden reportar. El motivo es obligatorio (máx. 500 caracteres tras recortar espacios); la evidencia es opcional (máx. 1000). El reporte queda `pending`. Solo `Administrator` lee un reporte y su evidencia; el resto recibe 403 aunque conozca el identificador. Un envío fallido nunca se presenta como enviado. Las reglas viven en BusinessLogic.

**Never:** Publicar o listar reportes para personas no administradoras; devolver motivo o evidencia en la respuesta del envío; cambiar el estado de la changa reportada; subir archivos; agregar resolución, bloqueo o notificaciones (historias 3.2-3.4); asignar el rol `Administrator` desde la API.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| REPORT_OK | Cuenta elegible, changa existente, motivo válido | `201`, `{id, status: "pending", createdAt}`; reporte persistido | — |
| REPORT_WITH_EVIDENCE | Igual, con evidencia | Evidencia persistida asociada al reporte | — |
| NO_EVIDENCE | Evidencia omitida o en blanco | Se registra sin evidencia | — |
| EMPTY_REASON | Motivo ausente o en blanco | Nada persistido | `400` validación, campo `reason` |
| TOO_LONG | Motivo > 500 o evidencia > 1000 | Nada persistido | `400` validación, campo `reason` o `evidence` |
| UNKNOWN_CHANGA | Changa inexistente | Nada persistido | `404`, `changa_not_found` |
| NOT_AUTHENTICATED | Sin sesión | — | `401` problem+json |
| NOT_ELIGIBLE | Cuenta ya no elegible | Nada persistido | `403` problem+json |
| ADMIN_READS | Rol `Administrator`, reporte existente | `200` con motivo, evidencia, estado, changa y autora del reporte | — |
| ADMIN_UNKNOWN | `Administrator`, reporte inexistente | — | `404`, `report_not_found` |
| NON_ADMIN_READS | Cuenta elegible sin rol, con o sin id válido | Sin datos | `403`, `report_forbidden` |
| REPORTER_READS | La propia autora del reporte sin rol | Sin datos | `403`, `report_forbidden` |

</frozen-after-approval>

## Code Map

- `specs/api/openapi-v1.yaml` -- definir `POST /api/v1/reports` y `GET /api/v1/reports/{id}` con sus errores antes de implementar.
- `src/layers/ChangaQuest.Entities/Reports/Report.cs` -- entidad `Report` y `ReportPolicy` (límites).
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs` y migración nueva -- tabla `Reports`.
- `src/layers/ChangaQuest.DataAccess/Reports/` -- `IReportStore`, `ReportStore` y registro.
- `src/layers/ChangaQuest.DataAccess/Accounts/IAccountIdentityStore.cs` e `IdentityAccountStore.cs` -- consulta de rol administrador.
- `src/layers/ChangaQuest.BusinessLogic/Reports/ReportManager.cs` -- elegibilidad, validación, existencia de la changa y autorización administrativa.
- `src/layers/ChangaQuest.Presentation/Reports/` -- endpoints con Problem Details y registro de servicios; `Program.cs` mapea y registra.
- `tests/` -- unidad (`ReportManagerTests`), integración (persistencia), aceptación (API) y contrato.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- contrato del envío y la lectura administrativa.
- [x] `tests/` -- pruebas en rojo derivadas de la matriz; confirmar que fallan por el comportamiento faltante.
- [x] `Entities`, `DataAccess`, migración y `BusinessLogic` -- implementación mínima.
- [x] `Presentation` y `Program.cs` -- endpoints y registro.
- [x] Build y suite completa; revisión; actualizar spec y `sprint-status.yaml`.

**Acceptance Criteria:**
- Given una cuenta autenticada elegible, when envía un reporte con motivo, then queda `pending` para revisión administrativa y no se expone a otras personas.
- Given evidencia opcional, when se incluye, then queda asociada al reporte; when se omite, then el reporte se registra igual.
- Given motivo vacío o envío fallido, when se intenta enviar, then se informa el error y no se registra ni se presenta como enviado.
- Given una persona sin rol administrador, when intenta leer un reporte o su evidencia, then se deniega.
- Given el rol `Administrator`, when lee un reporte, then ve motivo y evidencia.

## Fuera de alcance (producto real, PRD OQ-5)

Categorías de motivo, subida y límites de archivos, retención y acceso a evidencias, proceso de respuesta, pantalla de reporte en el cliente y alta de administradores.
