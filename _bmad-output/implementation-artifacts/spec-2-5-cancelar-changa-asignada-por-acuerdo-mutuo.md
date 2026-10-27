---
title: 'Story 2.5 — Cancelar una changa asignada por acuerdo mutuo'
type: 'feature'
created: '2026-10-26'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '85c8a81eb310d10ebf823557e568858feb4653f8'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
  - '{project-root}/docs/METODOLOGIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Una changa asignada no tiene un flujo explícito para que ambas partes acuerden cancelarla sin liberar la asignación unilateralmente ni dejar estados incompatibles.

**Approach:** Incorporar solicitudes de cancelación dirigidas a la otra parte. La solicitud mantiene activa la asignación; solo la aceptación explícita de la otra parte cancela la changa y libera la asignación de forma atómica.

## Boundaries & Constraints

**Always:** Solo la persona solicitante o la realizadora asignada puede proponer o responder una solicitud de su changa. Una solicitud pendiente no cambia `Assigned`. El rechazo y la falta de respuesta mantienen la asignación activa. La aceptación mutua cambia la changa a `Cancelled` y libera su asignación en una única transición persistida. Las operaciones deben validar el estado persistido bajo concurrencia y usar Problem Details con códigos estables.

**Never:** No permitir cancelación unilateral después de la asignación. No agregar vencimientos, liberación automática, abandono, mediación ni decisiones administrativas de OQ-7. No permitir operaciones de personas ajenas. No habilitar calificaciones, pagos o reasignación como consecuencia de esta cancelación. Después de un rechazo se permite una nueva propuesta posterior, pero nunca más de una solicitud pendiente activa.

**Decisión aprobada:** Después de un rechazo, cualquiera de las partes puede volver a proponer la cancelación mientras la changa continúe asignada.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| PROPOSE | Changa `Assigned`, parte asignada, sin solicitud pendiente | Se crea solicitud `Pending`; asignación sigue activa | HTTP 201, código `cancellation_request_created` |
| ACCEPT | Solicitud `Pending`, responde la otra parte, changa aún `Assigned` | Solicitud aceptada, changa `Cancelled`, asignación liberada atómicamente | HTTP 200 |
| REJECT | Solicitud `Pending`, responde la otra parte | Solicitud rechazada; changa y asignación siguen activas | HTTP 200 |
| OUTSIDER | Persona ajena intenta proponer o responder | Estado sin cambios | HTTP 403, `cancellation_request_forbidden` |
| STALE | Changa o solicitud ya transicionó | No se sobrescribe el estado persistido | HTTP 409, `cancellation_request_conflict` |
| NO_RESPONSE | Solicitud pendiente consultada sin respuesta | Sigue pendiente y la asignación sigue activa | HTTP 200, sin automatismo |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- agregar entidad y estados de solicitud sin mover reglas de transición fuera de BusinessLogic.
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs` -- mapear la entidad, índices y relación con la asignación.
- `src/layers/ChangaQuest.DataAccess/Changas/IChangaStore.cs` y `ChangaStore.cs` -- persistir solicitudes y ejecutar la aceptación atómica bajo transacción.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- autorizar participantes y decidir propuestas, respuestas y transiciones.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` -- exponer endpoints autenticados y Problem Details.
- `specs/api/openapi-v1.yaml` -- contrato canónico antes de API y cliente.
- `src/client/ChangaQuest.Client/Pages/Coordinacion.razor` -- mostrar propuesta, espera, aceptación, rechazo y cancelación.
- `tests/ChangaQuest.UnitTests/Changas/ChangaManagerTests.cs`, `ChangaPersistenceTests.cs`, `ChangaApiTests.cs`, `ChangaPageTests.cs` -- pruebas por capa y criterios.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- definir requests, respuestas, estados y códigos de cancelación antes de implementar endpoints.
- [x] `Changa.cs`, `ApplicationDbContext.cs` y migración -- persistir solicitudes y estados con relación e índices consistentes.
- [x] `IChangaStore.cs` y `ChangaStore.cs` -- agregar operaciones transaccionales y proteger carreras con estado persistido.
- [x] `ChangaManager.cs` -- implementar autorización y reglas de transición de acuerdo mutuo.
- [x] `ChangaEndpointExtensions.cs` y `Coordinacion.razor` -- integrar API y UX accesible con confirmaciones deliberadas.
- [x] Pruebas unitarias, integración, aceptación, contrato y cliente -- cubrir matriz, concurrencia y errores sin falsear éxito.

**Acceptance Criteria:**
- Given una changa asignada, when una parte propone cancelar, then la solicitud queda pendiente y la asignación permanece activa.
- Given una solicitud pendiente, when la otra parte acepta explícitamente, then la changa queda cancelada y la asignación se libera atómicamente.
- Given una solicitud pendiente, when la otra parte rechaza o no responde, then la asignación permanece activa y no hay liberación automática.
- Given una persona ajena, when intenta proponer o responder, then recibe denegación y el estado no cambia.
- Given una transición incompatible concurrente, when se procesan las operaciones, then solo una transición válida queda persistida.

## Implementation Notes

## Verification

**Commands:**
- `dotnet build --no-restore` -- esperado: compilación sin errores.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- esperado: suite completa en verde.
