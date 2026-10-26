---
title: "Story 2.1 — Confirmar la realización dentro del plazo"
type: feature
created: 2026-10-25
status: done
route: dispatch
review_loop_iteration: 1
baseline_commit: c258311ebba6ad07d069f87f7e6108d7749029c8
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
  - "{project-root}/docs/METODOLOGIA.md"
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Una changa asignada no tiene forma de registrar que el trabajo terminó. Sin eso no hay constancia del resultado ni camino hacia el pago declarado y el cierre.

**Approach:** `ChangaManager` gestiona la finalización. La realizadora marca la changa como terminada y eso abre un plazo de 20 minutos. La solicitante confirma o rechaza dentro del plazo. El rechazo o el vencimiento dejan la changa en «pendiente de revisión por finalización», sin cierre ni reasignación. El vencimiento se evalúa y se persiste cuando alguien consulta o actúa (sin tareas en segundo plano).

## Boundaries & Constraints

**Always:** Solo la realizadora asignada marca terminada y solo la solicitante autora confirma o rechaza; cualquier otra cuenta, o la parte equivocada, recibe `403` con `completion_forbidden`. Las reglas viven en BusinessLogic y se comprueban en cada llamada. Las transiciones son atómicas y condicionadas al estado esperado: si dos acciones compiten, solo una queda persistida. El plazo es de 20 minutos desde que se marca terminada y se mide con un reloj inyectable (`TimeProvider`). Después del vencimiento, o con la revisión iniciada, confirmar y rechazar no cambian el estado. Confirmar deja la changa «confirmada» y no la cierra. La interfaz usa texto claro en español, neutral, sin culpas, no depende solo del color y se opera con teclado con foco visible. El estado nuevo no debe confundirse con `PendingReview` de moderación de publicaciones (Story 1.8): usar valores propios en el modelo y en la API.

**Never:** No cerrar la changa, no reasignarla ni liberarla por rechazo o vencimiento. No simular decisiones, evidencias ni plazos administrativos. No implementar declaraciones de pago (2.3), cancelación (2.2/2.5), calificaciones (2.4) ni notificaciones. No usar tareas en segundo plano. No exponer dirección ni teléfonos en las vistas nuevas.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| MARK_DONE | Realizadora marca una changa asignada | Estado `pending_response`, `responseDeadline` = ahora + 20 min | N/A |
| CONFIRM | Solicitante confirma antes del plazo | Estado `confirmed`, sin cierre | N/A |
| REJECT | Solicitante rechaza antes del plazo | Estado `completion_review`, causa `rejected` | N/A |
| EXPIRED | Pasan 20 minutos sin respuesta y alguien consulta | Estado `completion_review`, causa `expired`, persistido | N/A |
| LATE_ACTION | Confirmar o rechazar con el plazo vencido | La changa pasa a revisión (causa `expired`) y la acción no la modifica | `409` `completion_not_pending` |
| IN_REVIEW | Confirmar o rechazar en revisión o ya confirmada | Estado sin cambios | `409` `completion_not_pending` |
| MARK_AGAIN | Marcar terminada fuera del estado asignada | Estado sin cambios | `409` `completion_invalid_state` |
| WRONG_ROLE | Solicitante marca terminada, o realizadora confirma o rechaza | Sin cambios | `403` `completion_forbidden` |
| OUTSIDER | Cuenta ajena consulta o actúa con el id | Sin datos ni cambios | `403` `completion_forbidden` |
| NOT_ASSIGNED | Changa disponible o sin asignación | Sin finalización | `409` `changa_not_assigned` |
| UNKNOWN_CHANGA | Id inexistente | Sin datos | `404` `changa_not_found` |
| RACE | Confirmar y rechazar simultáneos | Una sola transición persistida | La otra recibe `409` `completion_not_pending` |

</frozen-after-approval>

## Code Map

- `specs/api/openapi-v1.yaml` -- contrato primero: `GET /api/v1/changas/{id}/completion`, `POST .../completion/mark-done`, `POST .../completion/confirm`, `POST .../completion/reject`; esquema `ChangaCompletion` (`changaId`, `description`, `role`, `status`, `markedDoneAt`, `responseDeadline`, `reviewCause`); estados y códigos de error nuevos. Ampliar el enum de `Changa.status` con los valores nuevos.
- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- `ChangaStatus` suma `PendingResponse`, `Confirmed` y `CompletionReview` (columna `Status` es `nvarchar(16)`: respetar ese largo). `ChangaAssignment` suma `CompletionRequestedAt`, `CompletionDeadline`, `CompletionRespondedAt` y `CompletionReviewCause`. `ChangaPolicy` suma `CompletionResponseMinutes = 20`.
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs`, `Migrations/` -- columnas nuevas y migración aditiva `AddCompletionFlow`. No modificar migraciones previas.
- `src/layers/ChangaQuest.DataAccess/Changas/IChangaStore.cs`, `ChangaStore.cs` -- transición atómica condicionada al estado esperado (patrón `UPDATE ... WHERE Status = ...` de `TakeAsync`), con resultado que distingue éxito y estado ya cambiado. Persistir, no decidir.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- `GetCompletionAsync`, `MarkDoneAsync`, `ConfirmCompletionAsync`, `RejectCompletionAsync`, evaluación de vencimiento, vista `ChangaCompletionView` y estados `CompletionForbidden`, `CompletionNotPending`, `CompletionInvalidState`. Recibe `TimeProvider`. Reutilizar `CheckAccessAsync` y `GetCoordinationRole`. Actualizar `Project` para los valores de estado nuevos.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaBusinessLogicServiceCollectionExtensions.cs` -- registrar `TimeProvider.System`.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` -- los cuatro endpoints con Problem Details y códigos `completion_*`.
- `src/client/ChangaQuest.Client/Pages/` -- nueva `Finalizacion.razor` (`/tareas/{id}/finalizacion`); enlace desde `Coordinacion.razor` y `Coordinaciones.razor`. Reutilizar el estilo y el manejo de errores y timeouts de `Coordinacion.razor`. No tocar `ChangaPublicView` más allá del estado.
- `tests/` -- unidad (`ChangaManagerTests`, con reloj controlado), aceptación (`ChangaApiTests`, `ChangaPageTests`), integración de persistencia (incluida la carrera) y contrato OpenAPI.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- definir endpoints, esquemas, estados y errores -- el contrato manda
- [x] `tests/` -- escribir primero las pruebas de la matriz y verlas fallar (Red) -- TDD obligatorio
- [x] `Entities`, `DataAccess` -- modelo, migración aditiva y transición atómica -- persistencia
- [x] `BusinessLogic`, `Presentation` -- reglas, vencimiento, roles, endpoints -- mínimo para Green
- [x] `Finalizacion.razor` y enlaces -- plazo y estado con texto neutral, accesible -- UX-DR9, UX-DR14..16
- [x] Refactor con la suite en verde y actualizar `sprint-status.yaml`

**Acceptance Criteria:**
- Given una changa asignada, when la realizadora la marca terminada, then pasa a pendiente de respuesta con plazo de 20 minutos.
- Given pendiente de respuesta dentro del plazo, when la solicitante confirma, then queda registrada y la changa no se cierra.
- Given pendiente de respuesta dentro del plazo, when la solicitante rechaza, then pasa a pendiente de revisión sin cierre ni reasignación.
- Given que vencen los 20 minutos, when se evalúa el estado, then pasa a pendiente de revisión sin cierre ni reasignación.
- Given pendiente de revisión o plazo vencido, when se intenta confirmar o rechazar, then el estado no cambia.
- Given una finalización pendiente o en revisión, when se consulta, then se ve el plazo o el estado en texto claro, sin culpas ni decisión administrativa simulada.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

El vencimiento es perezoso: toda consulta o acción sobre una changa `PendingResponse` con `now >= CompletionDeadline` primero la mueve a `CompletionReview` (causa `expired`) con la misma transición atómica, y recién después responde. Así no hace falta un proceso en segundo plano y las pruebas controlan el tiempo con un `TimeProvider` falso. Un ejemplo de contrato de lectura:

```json
{ "status": "pending_response", "role": "requester",
  "markedDoneAt": "2026-10-25T15:00:00Z", "responseDeadline": "2026-10-25T15:20:00Z" }
```

## Verification

**Commands:**
- `dotnet build ChangaQuest.sln --nologo` -- expected: 0 errores
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- expected: toda la suite en verde

**Manual checks (if no CLI):**
- Con dos cuentas: la realizadora marca terminada, la solicitante confirma y la changa sigue sin cerrarse; repetir y rechazar para ver la revisión. El plazo de 20 minutos se verifica con pruebas de reloj controlado.
