---
title: "Story 1.4 — Tomar una changa disponible sin duplicar asignaciones"
type: feature
created: 2026-10-22
status: in-review
route: dispatch
review_loop_iteration: 0
baseline_commit: e41680c71e6a93438f13a808766aacb678d5132a
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
  - "{project-root}/docs/METODOLOGIA.md"
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Una changa disponible debe poder reservarse por una persona realizadora elegible sin que la persona solicitante se autoasigne ni que solicitudes simultáneas creen más de una asignación.

**Approach:** Incorporar una operación explícita de toma bajo la autoridad de `ChangaManager`, con persistencia atómica, resultado idempotente para reintentos de la misma persona y conflicto estable para otra persona. El cliente separará la revisión del detalle de la confirmación y reflejará que la changa dejó de estar disponible.

## Boundaries & Constraints

**Always:** Solo cuentas autenticadas, adultas y con correo verificado pueden tomar. La persona realizadora debe ser distinta del autor. Una toma exitosa cambia el estado y crea una única asignación. Repetir la toma del mismo autor devuelve la asignación existente; otra persona recibe `409` y `changa_already_assigned`. Las reglas viven en BusinessLogic, EF Core en DataAccess y los errores HTTP usan Problem Details.

**Never:** No implementar selección entre postulaciones, GPS, categorías, teléfonos, finalización, cancelación ni pagos. No exponer dirección exacta en la respuesta pública. No resolver concurrencia con estado en memoria ni con lógica en el endpoint.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| HAPPY_PATH | Changa `available`, autor distinto y cuenta elegible | Se crea una asignación única y la changa deja de estar disponible | HTTP 200/201 según contrato |
| SELF_ASSIGNMENT | Autor intenta tomar su propia changa | No cambia el estado ni crea asignación | Problem Details, regla estable |
| CONCURRENT_TAKE | Dos personas toman la misma changa simultáneamente | Como máximo una asignación; la otra operación informa no disponibilidad | HTTP 409 `changa_already_assigned` |
| IDEMPOTENT_RETRY | Mismo realizador repite una toma ya confirmada | Devuelve la asignación existente sin duplicarla | Respuesta exitosa |
| CANCELLED | Changa no disponible/cancelada | No altera la asignación ni el estado | Problem Details |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- entidad y estados; extender el modelo con asignación sin exponer dirección.
- `src/layers/ChangaQuest.DataAccess/Changas/IChangaStore.cs` y `ChangaStore.cs` -- contrato y transacción/consulta atómica de toma.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- autorización, autoasignación, idempotencia y reglas de transición.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` -- endpoint HTTP y Problem Details.
- `src/client/ChangaQuest.Client/Pages/` -- detalle, confirmación y estado de disponibilidad.
- `specs/api/openapi-v1.yaml` -- contrato canónico antes de API y cliente.
- `tests/ChangaQuest.UnitTests/Changas/`, `IntegrationTests/Changas/`, `AcceptanceTests/Changas/` y `Contracts/` -- cobertura de reglas, concurrencia y contrato.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- documentar la operación de toma, asignación y errores -- alinear API y cliente.
- [x] `src/layers/ChangaQuest.Entities/`, `DataAccess/Changas/` -- persistir asignación única y operación atómica -- proteger la invariancia bajo concurrencia.
- [x] `src/layers/ChangaQuest.BusinessLogic/Changas/` -- implementar toma autorizada, autoasignación e idempotencia -- centralizar reglas.
- [x] `src/layers/ChangaQuest.Presentation/` y `src/backend/ChangaQuest.Api/` -- exponer y registrar el endpoint -- devolver códigos HTTP estables.
- [x] `src/client/ChangaQuest.Client/` -- agregar confirmación deliberada y actualización de disponibilidad -- evitar éxito falso ante conflicto.
- [x] `tests/` -- escribir y ejecutar pruebas antes de producción -- demostrar Red y Green para cada caso.

**Acceptance Criteria:**
- Given una cuenta elegible distinta del autor, when confirma tomar una changa disponible, then queda una asignación y deja de estar disponible.
- Given el autor de una changa, when intenta tomarla, then la operación se rechaza sin modificarla.
- Given tomas simultáneas, when compiten sobre la misma changa, then solo una asignación queda persistida.
- Given un reintento del mismo realizador, when repite el comando, then recibe la asignación existente sin duplicado.
- Given otra persona intenta una changa ya asignada, when procesa la toma, then recibe HTTP 409 con `changa_already_assigned`.
- Given el detalle cambia de estado, when la persona confirma, then la interfaz informa que ya no está disponible y no muestra éxito.

## Implementation Notes

- Se agregó `ChangaAssignment` con índice único por changa y una migración aditiva `AddChangaAssignments`.
- La toma usa transacción serializable y bloqueo `UPDLOCK`/`ROWLOCK` sobre la changa; la actualización de estado y la inserción de asignación se confirman juntas.
- El endpoint `POST /api/v1/changas/{id}/take` devuelve la asignación existente en reintentos del mismo realizador y `409` con códigos estables para conflictos.
- El cliente separa el detalle de la confirmación mediante `/tareas/{id}` y no muestra éxito ante conflictos.
- Red comprobado con compilación fallida por los tipos de asignación ausentes; Green comprobado con 70 pruebas aprobadas.

## Verification

**Commands:**
- `dotnet build ChangaQuest.sln --nologo` -- esperado: compilación sin errores.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- esperado: suite completa en verde.
