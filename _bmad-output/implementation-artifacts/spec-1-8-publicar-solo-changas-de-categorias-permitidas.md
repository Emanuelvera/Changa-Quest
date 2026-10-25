---
title: "Story 1.8 — Publicar solo changas de categorías permitidas"
type: feature
created: 2026-10-24
status: review
route: dispatch
review_loop_iteration: 1
baseline_commit: 85c8a81eb310d10ebf823557e568858feb4653f8
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/docs/METODOLOGIA.md"
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La publicación actual convierte toda solicitud válida en una changa disponible sin exigir una categoría aprobada ni una revisión del contenido libre. Esto puede poner en el tablón trabajos profesionales o de alto riesgo fuera del propósito del producto real.

**Approach:** Incorporar un catálogo inicial de categorías aprobadas y hacer que toda publicación nueva pase por revisión antes de estar disponible. La categoría será obligatoria, la descripción libre se conservará para revisión independiente y una categoría aprobada no será considerada garantía de seguridad.

**Decisión de moderación:** La aprobación o rechazo de publicaciones pendientes se implementará inicialmente como una operación interna de BusinessLogic. No se agregará un endpoint administrativo ni una interfaz de moderación dentro de esta historia; una herramienta administrativa futura podrá consumir esa operación.

## Boundaries & Constraints

**Always:** El catálogo inicial contiene limpieza doméstica, cocina o preparación básica, compras y mandados, jardinería básica, organización del hogar y cuidado de mascotas. Una publicación requiere una categoría existente y aprobada. Toda publicación nueva queda `PendingReview` y no aparece en listados disponibles hasta ser aprobada. La descripción libre se valida y revisa independientemente de la categoría. Las reglas viven en BusinessLogic, la persistencia en DataAccess y los errores HTTP usan Problem Details. Los identificadores de categoría son estables y el catálogo expuesto solo incluye categorías aprobadas.

**Never:** No publicar automáticamente una changa solo porque su categoría esté aprobada. No interpretar la categoría como certificación de seguridad. No agregar todavía una lista automática de tareas prohibidas ni clasificar texto por heurísticas no aprobadas. No implementar profesiones, tareas de alto riesgo, pagos, reportes completos ni un endpoint o interfaz administrativa de moderación en esta historia. No romper la privacidad de `exactAddress`, la elegibilidad de cuenta ni los estados existentes `available`, `assigned` y `cancelled`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| VALID_CATEGORY | Cuenta elegible, categoría aprobada y formulario válido | Se crea la changa como `pending_review`; no aparece disponible | HTTP 201 con estado pendiente |
| MISSING_CATEGORY | Request sin categoría | No persiste la changa | HTTP 400, error en `categoryId` |
| UNKNOWN_CATEGORY | Identificador inexistente | No persiste la changa | HTTP 400, código estable de categoría inválida |
| UNAPPROVED_CATEGORY | Categoría existente pero no aprobada | No queda disponible ni se lista | HTTP 400, código estable de categoría no aprobada |
| REVIEW_REJECTED | Publicación pendiente rechazada por moderación | No aparece disponible y conserva el motivo | Resultado de negocio explícito |
| REVIEW_APPROVED | Publicación pendiente aprobada | Pasa a `available` y puede listarse | Resultado de negocio explícito |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- entidad, estados actuales y políticas de publicación; extender sin duplicar el ciclo de vida.
- `src/layers/ChangaQuest.DataAccess/Changas/ChangaStore.cs` y `IChangaStore.cs` -- persistencia, consultas disponibles y contrato de acceso; agregar catálogo/estado sin decidir reglas.
- `src/layers/ChangaQuest.DataAccess/ApplicationDbContext.cs` -- conversiones, índices y relaciones EF Core.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- `CreateAsync`, `ChangaRegistrationValidator`, comandos, resultados y autoridad de transiciones.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` -- `POST /api/v1/changas`, validación HTTP y Problem Details.
- `src/client/ChangaQuest.Client/Pages/Publicar.razor` -- formulario, carga del catálogo, errores asociados y envío.
- `specs/api/openapi-v1.yaml` -- contrato canónico de request, catálogo, estados y errores.
- `tests/ChangaQuest.UnitTests/Changas/ChangaManagerTests.cs` y `ChangaRegistrationValidatorTests.cs` -- pruebas unitarias existentes para ampliar en rojo.
- `tests/ChangaQuest.IntegrationTests/Changas/ChangaPersistenceTests.cs` -- persistencia y migración.
- `tests/ChangaQuest.AcceptanceTests/Changas/ChangaApiTests.cs`, `Client/ChangaPageTests.cs` y `Contracts/OpenApiContractTests.cs` -- API, UI y contrato.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- definir `categoryId`, catálogo, estado pendiente, transición de revisión y errores -- contrato antes de API y cliente.
- [x] `tests/ChangaQuest.UnitTests/Changas/*` -- escribir pruebas rojas de categoría obligatoria, aprobación y transición -- cubrir la matriz.
- [x] `tests/ChangaQuest.IntegrationTests/Changas/*` -- dejar preparada la persistencia, consultas y exclusión del tablón -- asegurar que pendiente/rechazada no sea disponible.
- [x] `tests/ChangaQuest.AcceptanceTests/{Changas,Client,Contracts}/*` -- cubrir API, formulario y contrato -- verificar el recorrido completo.
- [x] `src/layers/ChangaQuest.Entities/*` -- agregar categoría y estado necesarios -- representar el dominio sin reglas HTTP.
- [x] `src/layers/ChangaQuest.DataAccess/*` -- persistir catálogo/estado y consultar aprobadas -- ejecutar solicitudes del manager.
- [x] `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- validar categoría, crear pendiente y coordinar revisión -- única autoridad de negocio.
- [x] `src/layers/ChangaQuest.Presentation/Changas/*` y `src/client/ChangaQuest.Client/Pages/Publicar.razor` -- adaptar contrato y UX -- mostrar categorías, estados y motivos.

**Acceptance Criteria:**
- Given una persona solicitante elegible, when completa una publicación, then debe elegir una categoría aprobada y la publicación queda pendiente antes de estar disponible.
- Given una categoría inexistente o no aprobada, when intenta publicar, then no se crea como disponible y recibe el motivo correspondiente.
- Given una descripción libre junto a una categoría aprobada, when se revisa la publicación, then la categoría no sustituye la validación/revisión del contenido.
- Given una publicación pendiente o rechazada, when se consulta el tablón, then no aparece como disponible.
- Given una publicación pendiente aprobada, when se consulta el tablón, then aparece con la categoría aprobada y conserva las reglas de privacidad existentes.

## Implementation Notes

- El catálogo inicial se persiste mediante `ChangaCategories` y la migración `20261024051338_AddChangaCategoriesAndReview`; las categorías aprobadas se consultan desde DataAccess.
- Las publicaciones nuevas se crean como `PendingReview`; `ChangaManager` ofrece aprobación y rechazo internos, sin endpoint administrativo.
- La instancia `MSSQLLocalDB` está instalada pero no inicia en este entorno. Por eso las pruebas que necesitan una base SQL real quedan pendientes de ejecución local.

## Verification

**Commands:**
- `dotnet build` -- passed: solución compilada sin errores con SDK 10.0.401.
- `dotnet test --test-modules "tests\ChangaQuest.UnitTests\bin\Debug\net10.0\ChangaQuest.UnitTests.dll"` -- passed: 54 pruebas.
- `dotnet test --test-modules "tests\ChangaQuest.AcceptanceTests\bin\Debug\net10.0\ChangaQuest.AcceptanceTests.dll" --filter-class "ChangaQuest.AcceptanceTests.Client.ChangaPageTests"` -- passed: 19 pruebas.
- `dotnet test --test-modules "tests\ChangaQuest.AcceptanceTests\bin\Debug\net10.0\ChangaQuest.AcceptanceTests.dll" --filter-class "ChangaQuest.AcceptanceTests.Contracts.OpenApiContractTests"` -- passed: 2 pruebas.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- blocked: SQL LocalDB no inicia; las pruebas de API/integración no pueden crear la base.
