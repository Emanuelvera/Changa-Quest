---
title: 'Cancelar una changa antes de su asignación'
type: 'feature'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'df364f6f2e4d9f499ceca91604d8050872c7def2'
context:
  - '{project-root}/docs/METODOLOGIA.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La persona solicitante no puede retirar una changa disponible que ya no necesita, y la pantalla «Mis changas» no muestra sus publicaciones sin asignación.

**Approach:** Extender «Mis changas» con una sección de publicaciones propias sin asignación y permitir cancelar allí una publicación solo cuando esté disponible. La persona revisa una explicación clara y confirma deliberadamente antes de enviar la cancelación.

## Boundaries & Constraints

**Always:** Solo la autora puede cancelar; la changa debe estar disponible y no tener asignación. La cancelación y la toma deben competir mediante una transición atómica: solo una puede persistirse. Mantener la publicación y su estado cancelado; excluirla de búsquedas disponibles. Ocultar dirección exacta y demás datos privados en la lista.

**Never:** Cancelar una changa asignada o en otro estado; eliminar la publicación, liberar una asignación, reasignar automáticamente, ni agregar una decisión administrativa o notificación fuera del alcance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| OWNER_CANCELS | Autora autenticada; changa disponible sin asignación; confirma | Estado `cancelled`; deja de aparecer en disponibles y la lista propia muestra el estado | Éxito explícito |
| OTHER_USER | Otra cuenta solicita cancelar | Estado sin cambios | `403`, `changa_cancel_forbidden` |
| ASSIGNED | Changa asignada antes de la solicitud | Estado y asignación sin cambios | `409`, `changa_already_assigned` |
| OTHER_STATE | No disponible, pendiente de revisión, rechazada o ya cancelada | Estado sin cambios | `409`, `changa_not_available` |
| NOT_FOUND | Identificador inexistente | Sin cambios | `404`, `changa_not_found` |
| TAKE_CANCEL_RACE | Toma y cancelación simultáneas | Persiste exactamente una transición: asignada con asignación, o cancelada sin asignación | Operación perdedora recibe conflicto; nunca éxito engañoso |
| CANCEL_CONFIRMATION | La autora inicia cancelación y elige volver | No se envía petición ni cambia el estado | No aplica |
| CANCEL_FAILURE | Error HTTP o de red al confirmar | No se anuncia éxito; se conserva o recarga el estado real y se informa en español | Mensaje accesible y opción de reintento |

</frozen-after-approval>

## Code Map

- `specs/api/openapi-v1.yaml` -- actualizar primero el contrato; definir `GET /api/v1/changas/mine`, `POST /api/v1/changas/{id}/cancel`, respuestas y códigos estables. Reutilizar `ChangaPublicView`; no exponer `ExactAddress`.
- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- `ChangaStatus.Cancelled` ya existe. No requiere entidad ni columna nuevas; `Status` se persiste como texto de hasta 16 caracteres.
- `src/layers/ChangaQuest.DataAccess/Changas/IChangaStore.cs` y `ChangaStore.cs` -- añadir consulta de publicaciones propias sin asignación y transición de cancelación que comparta la protección transaccional/locking de `TakeAsync`. DataAccess persiste; no define permisos.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- añadir listado y cancelación; verificar cuenta elegible, autoría, estado y asignación. Traducir resultados de persistencia a errores de dominio. Mantener BusinessLogic como dueña de reglas de ciclo de vida.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` -- exponer el listado autenticado y cancelación, usando Problem Details y la identidad de la cuenta autenticada.
- `src/client/ChangaQuest.Client/Pages/Coordinaciones.razor` -- extender «Mis changas» con «Publicadas por mí», estados textuales y acción disponible solo para publicaciones cancelables. Usar confirmación explícita accesible, teclado y foco visible; no tocar la dirección privada.
- `tests/ChangaQuest.UnitTests/Changas/ChangaManagerTests.cs` -- probar autoría, elegibilidad, estados y errores.
- `tests/ChangaQuest.IntegrationTests/Changas/ChangaPersistenceTests.cs` -- comprobar persistencia de cancelación y carrera real contra `TakeAsync`.
- `tests/ChangaQuest.AcceptanceTests/Changas/ChangaApiTests.cs`, `Client/ChangaPageTests.cs` y `Contracts/OpenApiContractTests.cs` -- cubrir endpoints, confirmación/abandono, actualización de estado y contrato.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- definir listado propio, cancelación, respuestas y errores antes de implementar API.
- [x] `tests/` -- añadir primero pruebas de matriz y criterios; comprobar que fallan por el comportamiento faltante.
- [x] `DataAccess` y `BusinessLogic` -- consultar publicaciones propias y cancelar solo disponible sin asignación; competir atómicamente con la toma.
- [x] `Presentation` -- exponer ambos endpoints autenticados y traducir los resultados a Problem Details.
- [x] `Coordinaciones.razor` -- mostrar las publicaciones propias y confirmación accesible; reflejar resultado o error sin falsos éxitos.
- [x] Ejecutar build y suite completa; actualizar estado de spec y sprint.

**Acceptance Criteria:**
- Given una publicación propia disponible y sin asignación, when la autora confirma la cancelación, then queda cancelada y deja de estar disponible.
- Given una publicación propia sin asignación o ya asignada, when alguien distinto de la autora intenta cancelarla, then recibe denegación y no cambia el estado.
- Given que la publicación ya está asignada, when la autora intenta cancelarla, then la operación se rechaza y no altera estado ni asignación.
- Given una toma y una cancelación simultáneas, when ambas compiten, then solo una transición queda persistida y no existe una changa cancelada con asignación.
- Given que la autora inicia la acción desde «Mis changas», when revisa la consecuencia, then debe confirmar expresamente; puede volver sin cambios y todos los controles son comprensibles y operables con teclado.

## Implementation Notes

- El listado y la cancelación comparten las vistas públicas existentes; la cancelación serializable usa bloqueo de fila y una actualización condicional con `NOT EXISTS` para competir con la toma sin asignaciones canceladas.
- «Publicadas por mí» muestra estados textuales, omite publicaciones asignadas y no recibe direcciones privadas. La confirmación es explícita; los errores conservan la publicación y reintentan la lectura del estado real.

## Spec Change Log

## Review Triage Log

## Design Notes

La cancelación conserva el registro y cambia su estado; no equivale a borrar la publicación. El listado de «Publicadas por mí» comprende publicaciones propias sin asignación y muestra su estado, mientras que solo `available` ofrece la acción de cancelar. La ruta y el método de persistencia deben serializarse con la toma de la Story 1.4 para que no puedan confirmarse ambas operaciones.

## Verification

**Commands:**
- `dotnet build ChangaQuest.sln --nologo -v:q` -- correcto (0 errores).
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- correcto (138 pruebas, 0 errores).
