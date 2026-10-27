---
title: "Story 2.3 — Declarar por separado el pago externo"
type: feature
created: 2026-10-25
status: in-review
route: dispatch
review_loop_iteration: 0
baseline_commit: df364f6f2e4d9f499ceca91604d8050872c7def2
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md"
  - "{project-root}/_bmad-output/implementation-artifacts/spec-2-1-confirmar-la-realizacion-dentro-del-plazo.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
  - "{project-root}/docs/METODOLOGIA.md"
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Después de confirmar la realización, la aplicación necesita registrar por separado que la persona solicitante pagó y que la realizadora recibió la recompensa externa, sin presentar esas declaraciones como pagos verificados.

**Approach:** Agregar una operación autenticada `POST /api/v1/changas/{id}/payment-declaration`. El servidor determina el rol de la persona asignada: la solicitante declara pago y la realizadora declara recepción. Cada parte puede declarar una sola vez; la segunda declaración cierra la changa inmediatamente en una transición atómica. La interfaz muestra claramente qué declaración falta y que la plataforma no procesa, custodia, verifica ni garantiza el pago.

## Boundaries & Constraints

**Always:** Solo las partes asignadas pueden consultar o registrar el estado. La declaración solo está disponible cuando la realización está `Confirmed`. No se solicita monto, comprobante ni dato de transacción. Las declaraciones son afirmaciones individuales y se persisten separadamente. La segunda declaración cambia la changa a cerrada/completada y habilita el flujo posterior de calificación. Las reglas viven en `ChangaManager`, la persistencia en DataAccess y los errores usan Problem Details con códigos estables.

**Never:** No procesar, custodiar, verificar ni garantizar pagos. No aceptar declaraciones antes de la confirmación, de cuentas ajenas o duplicadas. No agregar pagos reales, selección, cancelación, revisión administrativa ni calificaciones dentro de esta historia.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| REQUESTER_DECLARATION | `Confirmed`, solicitante autenticada sin declarar | Registra `paid=true` y mantiene abierta la changa si falta recepción | N/A |
| WORKER_DECLARATION | `Confirmed`, realizadora autenticada sin declarar | Registra `received=true` y mantiene abierta la changa si falta pago | N/A |
| SECOND_DECLARATION | `Confirmed`, una declaración existente | Registra la segunda y cierra atómicamente | N/A |
| PENDING_CONFIRMATION | Estado asignado o `PendingResponse` | No permite declarar ni altera el estado | `409 payment_declaration_unavailable` |
| OUTSIDER | Cuenta ajena a la asignación | No revela estado ni modifica datos | `403 payment_declaration_forbidden` |
| DUPLICATE | Parte que ya declaró repite la operación | No crea otra declaración ni cambia el cierre | `409 payment_declaration_already_recorded` |
| CANCELLED_OR_REVIEW | Changa cancelada o pendiente de revisión | No permite declarar | `409 payment_declaration_unavailable` |
| CONCURRENT_SECOND | Ambas partes declaran simultáneamente | Solo una transición de cierre queda persistida y ambas declaraciones quedan consistentes | La operación perdedora relee estado; no hay cierre parcial |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- `ChangaStatus` y `ChangaAssignment`; agregar datos de pago externo separados sin guardar montos.
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs` y `Accounts/Migrations/` -- mapear columnas y generar migración aditiva.
- `src/layers/ChangaQuest.DataAccess/Changas/IChangaStore.cs` y `ChangaStore.cs` -- consulta autorizada y transición atómica de declaración/cierre; no decidir reglas de negocio.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- roles, elegibilidad, duplicados, estados, vista y cierre.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` -- endpoint y Problem Details.
- `specs/api/openapi-v1.yaml` -- contrato canónico antes de API y cliente.
- `src/client/ChangaQuest.Client/Pages/Finalizacion.razor` y `Coordinaciones.razor` -- mostrar declaraciones pendientes, acción separada y estado cerrado.
- `tests/ChangaQuest.UnitTests/Changas/`, `IntegrationTests/Changas/`, `AcceptanceTests/Changas/`, `Client/` y `Contracts/` -- cobertura de reglas, persistencia, concurrencia y contrato.
- Reutilizar roles, asignación y estado `Confirmed` de Story 2.1; no modificar el comportamiento de finalización ya cubierto.

## Tasks & Acceptance

**Execution:**
- [ ] `specs/api/openapi-v1.yaml` -- documentar consulta/registro, respuesta, estados y errores -- contrato primero.
- [ ] `tests/` -- agregar pruebas de la matriz antes del código y ejecutar Red -- demostrar TDD.
- [ ] `src/layers/ChangaQuest.Entities/`, `DataAccess/` -- persistir dos declaraciones y transición atómica -- evitar cierre parcial o duplicados.
- [ ] `src/layers/ChangaQuest.BusinessLogic/Changas/` -- implementar autorización, roles, disponibilidad y cierre -- centralizar reglas.
- [ ] `src/layers/ChangaQuest.Presentation/` y `src/backend/ChangaQuest.Api/` -- exponer la operación -- mantener Problem Details.
- [ ] `src/client/ChangaQuest.Client/Pages/` -- mostrar y registrar las declaraciones -- texto claro sin prometer pago verificado.
- [ ] `sprint-status.yaml` y documentación de la spec -- registrar verificación y dejar la historia en `review`.

**Acceptance Criteria:**
- Given realización confirmada y solicitante sin declaración, when registra que pagó, then se guarda solo su afirmación sin monto ni transacción.
- Given realización confirmada y realizadora sin declaración, when registra que recibió, then se guarda solo su afirmación sin monto ni transacción.
- Given una sola declaración, when se consulta el estado, then se identifica la declaración pendiente y la changa no se cierra.
- Given ambas declaraciones, when se registra la segunda, then la changa se cierra normalmente y queda habilitado el flujo de calificación.
- Given una consulta de declaración, when se muestra, then se aclara que es una afirmación individual y no un pago procesado, verificado o garantizado.
- Given realización no confirmada, when una parte intenta declarar, then la acción no está disponible y el estado no cambia.

## Implementation Notes

## Verification

**Commands:**
- `dotnet build ChangaQuest.sln --nologo` -- esperado: build sin errores.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- esperado: suite completa en verde.
