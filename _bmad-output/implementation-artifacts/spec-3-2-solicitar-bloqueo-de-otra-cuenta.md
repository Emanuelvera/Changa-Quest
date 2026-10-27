---
title: 'Story 3.2 — Solicitar el bloqueo de otra cuenta'
type: 'feature'
created: '2026-10-27'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c3a4928'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/METODOLOGIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Una persona usuaria necesita registrar que no desea futuras interacciones con otra cuenta, pero la aplicación no puede presentar el bloqueo como una cancelación o resolución de una changa ya asignada.

**Approach:** Exponer una acción autenticada para solicitar el bloqueo de otra cuenta desde una superficie de usuario. Antes de persistir, mostrar una confirmación explícita que explique que el bloqueo no cancela ni resuelve changas asignadas. Confirmar registra la relación; cancelar no registra nada.

## Boundaries & Constraints

**Always:** Solo una persona autenticada puede iniciar la acción. La cuenta objetivo debe existir. La confirmación debe mencionar expresamente que no cancela ni resuelve una changa ya asignada. La relación queda registrada sin mutar changas ni asignaciones. Repetir el bloqueo de la misma cuenta es idempotente.

**Never:** No permitir auto-bloqueo. No aplicar todavía efectos del bloqueo sobre interacciones, postulaciones, toma, coordinación o changas existentes. No cancelar, liberar, reasignar ni resolver changas. No agregar desbloqueo, notificaciones, moderación administrativa o categorías de bloqueo.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| CONFIRM | Sesión elegible, cuenta objetivo existente, confirmación aceptada | Relación persistida; no cambia ninguna changa | HTTP 201, `account_blocked` |
| REPEAT | Ya existe la relación entre las cuentas | No duplica la relación y conserva el estado | HTTP 200, `account_already_blocked` |
| CANCEL | Diálogo cancelado | No se envía la solicitud ni se persiste relación | Sin llamada de registro |
| SELF | La persona intenta bloquearse | No se registra | HTTP 409, `cannot_block_self` |
| UNKNOWN_TARGET | Cuenta objetivo inexistente | No se registra | HTTP 404, `account_not_found` |
| UNAUTHENTICATED | Sin sesión | No se registra | HTTP 401 Problem Details |
| ASSIGNED | Existe una changa asignada entre las partes | Se registra la relación sin mutar la changa | HTTP 201, sin afirmación de cancelación |

</frozen-after-approval>

## Code Map

- `specs/api/openapi-v1.yaml` -- definir la solicitud, respuesta y Problem Details antes de API y cliente.
- `src/layers/ChangaQuest.Entities/Accounts/` -- agregar la relación de bloqueo y sus estados o claves.
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs` y migración -- persistir la relación con unicidad por par.
- `src/layers/ChangaQuest.DataAccess/Accounts/` -- agregar contrato y store de bloqueos.
- `src/layers/ChangaQuest.BusinessLogic/` -- autorizar, validar objetivo, auto-bloqueo e idempotencia sin aplicar enforcement.
- `src/layers/ChangaQuest.Presentation/Accounts/` -- endpoint autenticado y Problem Details.
- `src/client/ChangaQuest.Client/Pages/Perfil.razor` -- acción, confirmación explícita y mensajes sin prometer cancelación.
- `tests/ChangaQuest.UnitTests/`, `tests/ChangaQuest.IntegrationTests/`, `tests/ChangaQuest.AcceptanceTests/` -- cubrir la matriz y la UX.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- definir contrato de bloqueo y errores estables.
- [x] `tests/` -- escribir pruebas en rojo para confirmación, cancelación, idempotencia, auto-bloqueo, cuenta inexistente y changa asignada.
- [x] `Entities`, `DataAccess`, migración y `BusinessLogic` -- persistir la relación sin aplicar efectos del bloqueo.
- [x] `Presentation` y `Perfil.razor` -- exponer la acción y confirmar antes de registrar.
- [ ] Build, pruebas enfocadas y revisión -- bloqueado por la ausencia del SDK .NET 10.0.401 y pendiente de revisión.

**Acceptance Criteria:**
- Given una persona elige bloquear otra cuenta, when inicia la acción, then se explica que no cancela ni resuelve una changa asignada y se solicita confirmación.
- Given la persona confirma, when se registra la acción, then queda persistida la relación sin afirmar que una changa fue cancelada o resuelta.
- Given la persona cancela la confirmación, when cierra el diálogo, then no se registra el bloqueo.

## Fuera de alcance

El alcance efectivo del bloqueo y su efecto sobre interacciones o changas asignadas quedan pendientes de PRD OQ-5. Esta historia solo registra la relación y comunica sus límites.
