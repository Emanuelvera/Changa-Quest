---
title: "Story 1.5 — Coordinar una changa asignada con privacidad"
type: feature
created: 2026-10-23
status: in-review
route: dispatch
review_loop_iteration: 0
baseline_commit: c129fb85d10ca26ba659c90e4c2f6403443712fe
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
  - "{project-root}/docs/METODOLOGIA.md"
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Una changa asignada necesita coordinación, pero hoy no hay forma de compartir teléfonos con consentimiento ni de que la persona realizadora consulte la dirección exacta, sin exponer datos a terceros.

**Approach:** Agregar una consulta y una acción de coordinación, decididas por `ChangaManager`. Cada parte responde por separado si comparte su teléfono (ficticio, cargado al aceptar y guardado en la asignación). Los teléfonos se revelan solo con ambos consentimientos; la dirección exacta solo a la persona realizadora asignada.

## Boundaries & Constraints

**Always:** Solo las dos partes de la asignación (autor y realizadora) acceden a la coordinación; cualquier otra cuenta recibe `403` con `coordination_forbidden`, aunque conozca el identificador. Las condiciones se comprueban en cada consulta, no solo en la interfaz (AD-5). Aceptar exige un teléfono con formato válido; rechazar lo descarta. Cada parte puede cambiar su respuesta y el cambio se refleja de inmediato. Una parte nunca ve el teléfono de la otra sin que ambas hayan aceptado, y el estado de la otra parte se informa solo como «aceptó» o «no compartió». La dirección exacta solo se devuelve a la realizadora, aun con el consentimiento incompleto. La interfaz usa texto en español claro, no depende solo del color y se opera con teclado con foco visible. Reglas en BusinessLogic, EF Core en DataAccess y errores con Problem Details.

**Never:** No simular chat ni canal alternativo de contacto. No cancelar ni liberar la asignación por falta de consentimiento. No devolver la dirección exacta en listados, detalles públicos ni a la persona solicitante. No guardar teléfonos reales, ni usarlos en registros ni mensajes de error. No implementar cancelación, finalización, pagos ni notificaciones. Resolver OQ-4 queda fuera: es un bloqueo para el producto real.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| BOTH_ACCEPT | Ambas partes aceptan con teléfono válido | Cada parte ve el teléfono de la otra | N/A |
| ONE_PENDING | Una parte aceptó y la otra no respondió | Ningún teléfono visible; la asignación sigue activa | N/A |
| DECLINED | Una parte rechaza | Ningún teléfono visible; se descarta su número; sin cancelación | N/A |
| REVOKE | Una parte que aceptó cambia a rechazar | El teléfono de ambas partes vuelve a ocultarse | N/A |
| WORKER_ADDRESS | Realizadora consulta con consentimiento incompleto | Ve la dirección exacta | N/A |
| REQUESTER_ADDRESS | Autor consulta la coordinación | No recibe dirección exacta | N/A |
| OUTSIDER | Cuenta ajena consulta o responde con el id | No recibe datos ni modifica nada | `403` `coordination_forbidden` |
| NOT_ASSIGNED | Changa disponible o sin asignación | No hay coordinación | `409` `changa_not_assigned` |
| INVALID_PHONE | Acepta sin teléfono o con formato inválido | No cambia el estado | `400` con error en `phone` |
| UNKNOWN_CHANGA | Id inexistente | Sin datos | `404` `changa_not_found` |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- `ChangaAssignment` agrega, por parte, decisión (pendiente/aceptó/rechazó) y teléfono; `ChangaPolicy` agrega límites del teléfono (8–15 dígitos; se permiten `+`, espacios, `-` y paréntesis).
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs` y `Migrations/` -- columnas nuevas y migración aditiva `AddCoordinationConsent`. No modificar `AddChangaAssignments`.
- `src/layers/ChangaQuest.DataAccess/Changas/IChangaStore.cs` y `ChangaStore.cs` -- guardar la decisión de una parte y listar asignaciones de una cuenta. `FindByIdAsync` ya trae la asignación.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- `GetCoordinationAsync`, `SetPhoneConsentAsync`, `ListCoordinationsAsync`; roles, proyección autorizada, validación del teléfono y nuevos `ChangaManagerStatus`. Reutilizar `CheckAccessAsync`.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` -- `GET /api/v1/changas/{id}/coordination`, `PUT /api/v1/changas/{id}/coordination/phone-consent`, `GET /api/v1/coordinations`; mapear estados a Problem Details.
- `specs/api/openapi-v1.yaml` -- contrato primero.
- `src/client/ChangaQuest.Client/Pages/` -- nueva `Coordinacion.razor` (`/tareas/{id}/coordinacion`) y listado `Coordinaciones.razor` (`/coordinaciones`); enlace desde `DetalleChanga.razor` tras tomar y desde `Tareas.razor`. No tocar la proyección pública `ChangaPublicView`.
- `tests/` -- unidad (`ChangaManagerTests`), aceptación (`ChangaApiTests`), integración de persistencia, contrato OpenAPI y UI.

## Tasks & Acceptance

**Execution:**
- [x] `tests/` -- escribir y ejecutar primero las pruebas de la matriz y de los criterios (Red) -- cumplir AD-2.
- [x] `specs/api/openapi-v1.yaml` -- documentar los tres endpoints, esquemas y errores -- contrato antes que API y cliente.
- [x] `src/layers/ChangaQuest.Entities/`, `DataAccess/` -- campos de consentimiento, migración y acceso a datos -- persistir la decisión por parte.
- [x] `src/layers/ChangaQuest.BusinessLogic/Changas/` -- coordinación, roles, validación y proyección autorizada -- única capa con reglas.
- [x] `src/layers/ChangaQuest.Presentation/` -- endpoints y Problem Details estables -- adaptar HTTP.
- [x] `src/client/ChangaQuest.Client/` -- pantalla de coordinación, listado y estados (consentimiento incompleto, error, carga) -- sin chat ni éxito falso.

**Acceptance Criteria:**
- Given una changa asignada, when cada parte responde por separado, then sus decisiones son independientes y ningún teléfono es visible hasta que ambas acepten.
- Given ambas partes aceptaron, when cualquiera consulta, then ve el teléfono ficticio de la otra y ninguna cuenta ajena puede consultarlo.
- Given una parte rechazó o no respondió, when se consulta el estado, then la asignación sigue activa, los teléfonos siguen ocultos y no hay cancelación ni canal alternativo.
- Given la realizadora asignada, when consulta la coordinación, then ve la dirección exacta aunque el consentimiento esté incompleto; el autor nunca la recibe.
- Given una cuenta no asignada, when pide dirección o teléfonos con el id, then el sistema los deniega.
- Given el consentimiento mutuo no está habilitado, when se muestra la pantalla, then no hay chat ni contactos reales y se advierte que el número es ficticio.
- Given cualquier estado, when se muestra el resultado, then el texto es claro, no depende solo del color y los controles funcionan con teclado y foco visible.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| # | Hallazgo | Veredicto y evidencia | Ruta |
|---|---|---|---|
| 1 | Posible sobrescritura de consentimientos concurrentes | `false` - EF Core solo actualiza las propiedades modificadas por esta parte; las decisiones y telefonos de autor y realizadora ocupan columnas distintas. | Rechazado |
| 2 | Longitud del telefono comprobada antes de recortar espacios | `low` - el limite rechazaba un telefono valido por espacios exteriores; se normaliza antes de validar y la prueba de regresion con espacios excedentes pasa. | `patch` |
| 3 | Direccion exacta en el listado para la persona realizadora | `medium` - la proyeccion inicial reutilizaba la direccion del detalle y la lista la mostraba; ahora la proyeccion de lista fuerza `null`, cubierto en API y UI. | `patch` |
| 4 | Repeticion de la filtracion de direccion en listados | `medium` - misma causa raiz que el hallazgo 3: el listado usaba la proyeccion individual. Corregido en la proyeccion de lista. | `patch` (agrupado con 3) |
| 5 | Diff de revision sin encabezados de archivo nuevo | `low` - el diff incluia los contenidos pero no describia los nuevos archivos como adiciones; se regenero como diff unificado Git con `/dev/null` y modo de archivo nuevo. | `patch` |
| 6 | Identificador de migracion posterior al reloj local | `low` - el ID `20261023145438` excedia la hora local observada (`12:25`, 23-10-2026); se renombro a `20261023122000`, que sigue despues de la migracion predecesora. | `patch` |
| 7 | Navegacion `assignment.Changa` posiblemente nula | `false` - la consulta de listado incluye `Changa`; la relacion es requerida y la FK impide una asignacion valida sin changa asociada. | Rechazado |
| 8 | El manager ignora una actualizacion que no se guardo | `false` - el mismo contexto ya cargo la asignacion; la operacion ordinaria la actualiza o propaga un error de concurrencia, no confirma una escritura omitida como exito. No hay flujo de producto que elimine la asignacion concurrentemente. | Rechazado |
| 9 | Validacion acepta separadores telefonicos mal ubicados | `false` - la spec solo limita el conjunto de caracteres y la cantidad de digitos; no define una gramatica de formato canonico. La implementacion limita caracteres y exige parentesis balanceados. | Rechazado |
| 10 | Telefono valido con espacios exteriores rechazado por longitud | `low` - duplicado del hallazgo 2; ambas observaciones proceden de validar la cadena sin normalizar. Corregido y probado. | `patch` (agrupado con 2) |
| 11 | La pagina no vuelve a comprobar el rol antes de mostrar la direccion | `false` - el API proyecta la direccion solo para la persona realizadora y entrega `null` a quien solicita; la interfaz consume esa proyeccion autorizada. El listado ademas ya no representa la direccion. | Rechazado |
| 12 | El telefono propio no se precarga al volver a aceptar | `false` - la spec no requiere devolver el telefono propio y la persona debe pulsar explicitamente aceptar para cambiar su respuesta; no hay reemplazo automatico o silencioso. | Rechazado |
| 13 | Texto de error con acentos corruptos | `low` - el diff revisado contenia signos `?` en mensajes visibles; se corrigieron y se agregaron comprobaciones UTF-8 de error del API y estado no autenticado de la lista. | `patch` |
| 14 | Falta de cobertura de privacidad para la persona solicitante | `false` - la prueba HTTP matricial ya verifica direccion nula, telefono oculto y consentimiento mutuo para quien solicita; se amplio ademas la cobertura de ambos listados. | Rechazado |
| 15 | Pruebas OpenAPI solo buscaban fragmentos de texto | `low` - la prueba previa no comprobaba las variantes del request ni los esquemas/status relevantes; se agregaron aserciones para ambas variantes, referencias, errores y direccion nula en listas. | `patch` |
| 16 | Sin prueba de migracion sobre asignaciones ya existentes | `medium` - la prueba anterior migraba una base vacia; se agrego una prueba que crea una asignacion con el esquema previo, migra y confirma ambos estados `Pending` y telefonos nulos. | `patch` |
| 17 | La respuesta del listado puede contener direccion exacta | `medium` - confirmado en la revision original: la lista reutilizaba la proyeccion de detalle. Se corrigio junto con los hallazgos 3 y 4 y se cubrio para ambos roles. | `patch` (agrupado con 3) |
| 18 | Timeouts sin capturar en Coordinacion y Coordinaciones | `medium` - OperationCanceledException escapaba y mostraba el banner de error no controlado; se capturan y se muestra un mensaje recuperable. | `patch` |
| 19 | `+` mal ubicado aceptado (`-+12345678`) | `low` - la validacion solo exigia que `+` precediera a los digitos; ahora solo se admite en la primera posicion. | `patch` |
| 20 | Respuesta 200 con cuerpo null y arreglo `errors.phone` vacio | `low` - el formulario desaparecia sin mensaje o la lectura del error fallaba; se tratan como error. | `patch` |
| 21 | Huecos de prueba: aceptar desde la UI, telefono visible con consentimiento mutuo, caracteres prohibidos, endpoints sin sesion | `medium` - sin esas pruebas una regresion no fallaba; se agregaron 15 pruebas. | `patch` |
| 22 | Numeros reales aceptados aunque se describan como ficticios | `low` - no es verificable sin un catalogo de numeros de demo; la UI advierte que debe ser ficticio y OQ-4 bloquea el uso real. | Rechazado (alcance de demo) |
| 23 | `NotAssigned` antes de comprobar la participacion | `low` - solo revela si una changa ya publicada esta asignada; el estado ya es publico en el listado. La matriz de la spec fija `409`. | Rechazado |
| 24 | Escritura sin asignacion entre lectura y guardado | `false` - no existe un flujo de producto que elimine una asignacion. | Rechazado |

## Design Notes

- Decisión de diseño: el teléfono se carga al aceptar y vive en la asignación; `AppUser.PhoneNumber` no se usa. Un número descartado al rechazar no puede filtrarse después.
- El estado de la otra parte se limita a «aceptó» o «no compartió» para no distinguir entre rechazo y silencio.
- Las pruebas de aceptación deben cubrir acceso permitido y denegado (AD-5): autor, realizadora y una tercera cuenta.

## Verification

**Commands:**
- `dotnet build ChangaQuest.sln --nologo` -- esperado: compilación sin errores.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- esperado: suite completa en verde.
