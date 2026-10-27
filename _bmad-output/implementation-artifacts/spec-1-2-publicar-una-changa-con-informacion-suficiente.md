---
title: "Story 1.2 — Publicar una changa con información suficiente"
type: feature
created: 2026-10-22
status: in-review
route: dispatch
review_loop_iteration: 0
baseline_commit: 064a587b5d5ccfc5997bfaa5d0c705f2bb85ba1b
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
  - "{project-root}/docs/METODOLOGIA.md"
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Sin publicaciones no hay nada que encontrar, tomar ni coordinar; quien realiza una changa necesita evaluar descripción, recompensa, zona y materiales antes de comprometerse, sin ver la dirección exacta.

**Approach:** Una cuenta verificada publica una changa (descripción libre, recompensa en ARS, zona aproximada, dirección exacta privada y respuesta explícita sobre materiales). Queda disponible, con asignación por orden de llegada, en un tablón que solo muestra datos públicos. La demo incluye datos de muestra ficticios y curados.

## Boundaries & Constraints

**Always:** N-Layers y SDD/TDD (pruebas en rojo antes del código). Reglas solo en `ChangaManager` (BusinessLogic); `DbContext` y consultas solo en DataAccess. Contrato en `specs/api/openapi-v1.yaml` antes de API y cliente. Errores como `application/problem+json`. Publicar y ver el tablón exigen sesión de cuenta verificada y adulta.

**Decisiones registradas (sin duda abierta):**
- Ubicación = `zone` (barrio/zona, texto público, 3–80 car.) + `exactAddress` (texto privado, 5–200 car.). Ningún endpoint de esta historia devuelve `exactAddress`; el acceso del realizador asignado es la Story 1.5. Coordenadas y distancia quedan para la Story 1.3 (migración aditiva).
- Descripción libre de 10–1000 caracteres. Recompensa en ARS, entera positiva, máximo 10.000.000. Moneda fija ARS.
- Materiales: `materialsNeeded` (obligatorio, sí/no). Si es `true`, `materialsProvider` obligatorio (`requester` o `worker`); si es `false`, se conserva "no hacen falta" y `materialsProvider` debe omitirse.
- Modalidad fija `first_come` (orden de llegada), visible en tarjeta y detalle. Estado inicial `available`.
- Datos de muestra: solo en Development, idempotentes, con `isSample=true`, sin autor real y rotulados "Ejemplo ficticio"; nunca ofrecen contacto ni pago.
- Cliente: páginas "Ver tareas" (tablón) y "Publicar changa" con navegación persistente; "Mis changas" y "Perfil" llegan con sus historias.

**Never:** Radio, distancia, GPS o mapas (1.3/1.7); tomar o postularse (1.4/1.6); catálogo de categorías (1.8); pagos, contactos o teléfonos; devolver la dirección exacta en lista o detalle; crear changas sin autor salvo muestras; implementar reglas en controladores o persistencia.

## I/O & Edge-Case Matrix

| Caso | Entrada/estado | Resultado | Error |
|---|---|---|---|
| Publicación válida | Sesión verificada; descripción, recompensa >0, zona, dirección, materiales | 201 con la changa `available`/`first_come`; visible en el tablón | N/A |
| Faltan campos | Falta descripción, recompensa, zona, dirección o `materialsNeeded` | 400 con error por campo; no se crea changa | `application/problem+json` con `errors` por campo |
| Recompensa inválida | 0, negativa o mayor al máximo | 400 en `reward` | Sin changa |
| Materiales requeridos | `materialsNeeded=true` sin `materialsProvider` | 400 en `materialsProvider` | Sin changa |
| Sin materiales | `materialsNeeded=false` | Se conserva y se muestra "No hacen falta materiales" | `materialsProvider` presente → 400 |
| Sin sesión / no verificada | Sin cookie o cuenta no habilitada | 401/403 | Problem Details |
| Tablón/detalle público | Changa `available` | Descripción, recompensa, zona, materiales y modalidad; sin `exactAddress` | Id inexistente → 404 |
| Muestras | Arranque en Development | Changas ficticias rotuladas, sin duplicarse al reiniciar | N/A |
| UI | Celular o laptop, teclado | Etiquetas persistentes, errores por campo, foco visible, estados no solo por color | Error de red no simula éxito y conserva los datos |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.Entities/` -- crear `Changas/Changa`, `ChangaStatus`, `AssignmentMode`, `MaterialsProvider`, `ChangaPolicy` (límites).
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs` -- agregar `DbSet<Changa>` y mapeo; se reutiliza el contexto único e Identity.
- `src/layers/ChangaQuest.DataAccess/Changas/` -- `IChangaStore`, `ChangaStore`, semilla Development y registro DI; nueva migración aditiva en `Accounts/Migrations` (no editar las existentes).
- `src/layers/ChangaQuest.BusinessLogic/Changas/` -- `ChangaManager`, validador y resultados; `ChangaQuest.BusinessLogic.csproj` ya referencia DataAccess y Entities.
- `src/layers/ChangaQuest.Presentation/Changas/` -- endpoints `/api/v1/changas` (POST, GET lista, GET `{id}`) y DI; seguir `AccountEndpointExtensions.cs`.
- `src/backend/ChangaQuest.Api/Program.cs` -- mapear endpoints y servicios.
- `src/client/ChangaQuest.Client/Pages/`, `Layout/MainLayout.razor` -- páginas del tablón y publicación, navegación; patrón de `Home.razor`.
- `specs/api/openapi-v1.yaml` -- contrato de changas.
- `tests/` -- extender `AccountApiFactory.cs` (LocalDB aislada), `AccountRegistrationFlowTests.cs` (patrón de sesión), `OpenApiContractTests.cs`, `ArchitectureDependencyTests.cs`, `HomePageTests.cs` (patrón bUnit).

## Tasks & Acceptance

**Execution:**
- [x] `tests/ChangaQuest.UnitTests/Changas/` -- Red: validador y `ChangaManager` (campos faltantes, recompensa, materiales, modalidad/estado inicial, privacidad del modelo público).
- [x] `tests/ChangaQuest.IntegrationTests/Changas/` -- Red: persistencia en SQL Server y semilla idempotente.
- [x] `tests/ChangaQuest.AcceptanceTests/Changas/` y `Contracts/` -- Red: flujo HTTP (401/403/400/201, tablón, detalle sin dirección exacta, 404) y contrato OpenAPI.
- [x] `tests/ChangaQuest.AcceptanceTests/Client/` -- Red: formulario (etiquetas, errores asociados, conserva datos ante fallo) y tablón (carga, vacío, error, rótulo de ejemplo).
- [x] `specs/api/openapi-v1.yaml` -- contrato antes de API y cliente.
- [x] Entities, DataAccess (migración), BusinessLogic, Presentation, API -- Green mínimo.
- [x] `src/client/ChangaQuest.Client/` -- páginas y navegación accesible; Refactor con suite en verde.

**Acceptance Criteria:**
- Given cuenta verificada, when publica con todos los datos, then la changa queda `available`, `first_come` y visible en el tablón.
- Given datos faltantes o inválidos, when publica, then recibe errores por campo y no se crea changa.
- Given la changa no está asignada, when se ve tarjeta o detalle, then no aparece la dirección exacta en ninguna respuesta.
- Given datos de muestra, then son ficticios, rotulados y no generan contactos ni pagos.
- Given uso con teclado o ampliación, then etiquetas, foco y errores siguen accesibles y el estado no depende solo del color.

## Implementation Notes

- Migracion aditiva `20261022111646_AddChangas`; aplicada por `AccountDatabaseMigrationService` al iniciar la API.
- Las muestras Development usan identificadores estables, autor nulo y `isSample=true`; no se cargan en Production.
- Red previo al codigo: `dotnet build ChangaQuest.sln --nologo` correcto; la suite fallo por tipos/funciones aun ausentes: validador y proyeccion publica, ruta de publicacion, navegacion y tabla `Changas`.
- Green final: build correcto con 0 advertencias/errores y 61/61 pruebas aprobadas usando los comandos de Verification, incluida la prueba HTTP de 403 al perder elegibilidad adulta.

## Spec Change Log

## Review Triage Log

- **medium → patch (resuelto):** el contrato OpenAPI no aclaraba que las longitudes se miden tras `Trim()`; se documentó en `description`, `zone` y `exactAddress`.
- **medium → patch (resuelto):** el formulario duplicaba reglas de negocio con anotaciones; se eliminaron y el cliente muestra los errores por campo que devuelve `ChangaManager`.

## Verification

**Commands:**
- `dotnet build ChangaQuest.sln --nologo` -- compila toda la solucion; resultado final: correcto, 0 advertencias y 0 errores.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- resultado final: 61 correctas, 0 fallidas, 0 omitidas.
- `dotnet ef migrations has-pending-model-changes --project src\layers\ChangaQuest.DataAccess\ChangaQuest.DataAccess.csproj --startup-project src\backend\ChangaQuest.Api\ChangaQuest.Api.csproj --context ApplicationDbContext` -- sin cambios pendientes.

**Evidencia Red:** antes del codigo de produccion, el build de pruebas paso y la suite fallo por el comportamiento faltante: el validador/proyeccion no existian, `/publicar` no tenia ruta ni navegacion, y la migracion aun no creaba la tabla `Changas`. Tras implementar, el mismo comando quedo completamente en verde.
