---
title: 'Revisar un reporte con participación de las partes'
type: 'feature'
created: '2026-10-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'cb22b168d10095a65c0079f817e964c2271eaa54'
context:
  - '{project-root}/docs/METODOLOGIA.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problema:** La aplicación aún no ofrece una revisión trazable de reportes con acceso restringido a evidencia, participación de las partes, registro de una decisión y posibilidad de apelación.

**Enfoque:** Extender el flujo de reportes existente para que personal autorizado gestione la revisión y las partes accedan únicamente a las acciones e información que les corresponden.

## Boundaries & Constraints

**Always:** Aplicar autorización en el servidor a cada consulta o mutación que exponga casos o evidencia. Registrar decisiones, motivos y evidencias consideradas. Dar oportunidad de respuesta a las partes antes de decidir. No mostrar plazos hasta que se aprueben.

**Never:** Exponer reportes o evidencia a personas ajenas al caso, confiar en restricciones solo de interfaz, inventar categorías de sanción o políticas de evidencia, o incluir esta capacidad en la navegación operativa de la demo académica.

**Decisiones acordadas:** La dependencia de Story 3.1 ya está integrada desde `origin/dev`. Las dos partes son la persona solicitante y la realizadora de la changa asociada; en esta historia, reportante/reportada refiere a esas partes. Se puede decidir después de ofrecer respuesta a ambas, sin exigir respuesta ni renuncia, temporizador o cierre automático. La decisión registra una conclusión y motivo en texto libre, sin sanciones ni cambios de estado de la changa. Solo se usa la evidencia textual ya asociada al reporte; la decisión registra si esa evidencia se consideró, sin nuevos adjuntos ni política adicional de retención. Cualquier cuenta con rol `Administrator`, incluida quien revisó, puede resolver una apelación. El estado, la decisión y las acciones se consultan en la vista del caso, sin notificación por canales no definidos.

## I/O & Edge-Case Matrix

| Escenario | Entrada / estado | Salida / comportamiento esperado | Manejo de error |
|---|---|---|---|
| Lectura autorizada | Persona administradora consulta un reporte pendiente | Devuelve motivo y evidencia restringida al caso | 404 si no existe; no revelar datos del caso |
| Lectura no autorizada | Persona sin rol administrativo consulta reporte o evidencia | Acceso denegado sin filtrar contenido privado | 401/403 según autenticación |
| Respuesta de una parte | Solicitante o realizadora responde mientras el caso sigue abierto | Respuesta asociada al caso y disponible para la revisión | Rechazar respuesta de persona no vinculada o caso cerrado |
| Decisión | Persona administradora registra conclusión libre, motivo y si consideró la evidencia textual | Guardar un registro trazable sin sancionar ni cambiar la changa | Rechazar decisión no autorizada o duplicada |
| Apelación | Una de las dos partes apela luego de que la decisión queda visible en el caso | Registrar apelación asociada al caso y permitir resolverla a un `Administrator` | Rechazar apelación de persona no vinculada o antes de la decisión |

</frozen-after-approval>

## Code Map

- La rama `story/3-3-revisar-un-reporte-con-participacion` ya integra `origin/dev`, incluida Story 3.1 por PR #9.
- `src/layers/ChangaQuest.Entities/Reports/Report.cs` define `Report`, `ReportStatus`, `ReporterId`, `ChangaId`, evidencia opcional de texto y el rol `Administrator`.
- `src/layers/ChangaQuest.BusinessLogic/Reports/ReportManager.cs` concentra autorización y consultas de reportes; extender reglas aquí, no en endpoints.
- `src/layers/ChangaQuest.DataAccess/Reports/IReportStore.cs` y `ReportStore.cs` contienen persistencia del reporte; `ApplicationDbContext.cs` y su snapshot gobiernan EF Core.
- `src/layers/ChangaQuest.Presentation/Reports/ReportEndpointExtensions.cs` publica `/api/v1/reports`; proteger nuevas operaciones y conservar Problem Details.
- `specs/api/openapi-v1.yaml` contiene el contrato de Story 3.1; actualizarlo antes de endpoints y cliente.
- `src/client/ChangaQuest.Client/Pages/` alberga las superficies Blazor; la revisión requiere una vista de caso accesible solo a personal autorizado y acciones para sus dos partes.
- `tests/ChangaQuest.UnitTests/Reports/`, `tests/ChangaQuest.IntegrationTests/Reports/` y `tests/ChangaQuest.AcceptanceTests/Reports/` contienen pruebas existentes para ampliar.
- `ChangaManager` es dueño de las transiciones de changas. No cambiar estados de asignación/cierre ni aplicar sanciones desde esta historia sin la definición y coordinación de Story 3.4.

## Tasks & Acceptance

**Ejecución:**
- [x] Integrar la dependencia de Story 3.1 desde `origin/dev`.
- [x] Actualizar OpenAPI y especificar endpoints para lectura, respuestas, decisión y apelación con acceso por rol y pertenencia al caso.
- [x] Extender Entities, DataAccess, migración y BusinessLogic para persistir respuestas, conclusión, motivo, evidencia considerada y apelaciones, sin sanciones ni efectos de estado.
- [x] Añadir la superficie de revisión y las acciones para las partes, sin exponer evidencia privada ni plazos.
- [x] Añadir pruebas unitarias, de integración, contrato y aceptación; ejecutar el runner Microsoft.Testing.Platform documentado.

**Criterios de aceptación:**
- **Given** personal autorizado y un reporte en revisión, **when** consulta el caso, **then** ve motivo y evidencia privada; personas ajenas no acceden.
- **Given** un caso abierto con una persona solicitante y una realizadora, **when** se ofrece respuesta a ambas antes de decidir, **then** cualquier respuesta enviada queda asociada al caso y disponible para la revisión.
- **Given** una decisión registrada por personal autorizado, **when** se completa la revisión, **then** quedan guardados conclusión, motivo e indicador de si se consideró la evidencia textual existente, sin sanción ni transición de la changa.
- **Given** una decisión visible en la vista del caso, **when** una de las partes quiere apelar, **then** puede presentar una apelación asociada y cualquier `Administrator` puede resolverla.
- **Given** una persona sin autorización administrativa, **when** accede a revisión o evidencia, **then** el servidor deniega el acceso sin revelar datos privados.
- **Given** que los plazos siguen sin aprobación, **when** se muestra cualquier superficie de revisión, **then** no se presentan vencimientos ni cierres automáticos, y el caso permanece abierto hasta una acción explícita.

## Implementation Notes

Las decisiones de alcance constan en el bloque de intención. La historia registra solo la revisión y apelación; no implementa sanciones ni transiciones del ciclo de vida de una changa.

## Spec Change Log

## Review Triage Log

## Design Notes

Story 3.3 depende del contrato y modelo de reporte introducidos por Story 3.1. La historia no debe convertir la captura de una decisión en una transición de la changa ni en una sanción; las transiciones de realización pertenecen a Story 3.4 y a `ChangaManager`.

## Verification

**Commands:**
- `dotnet build` -- esperado: compilación correcta tras implementar.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- esperado: suite Microsoft.Testing.Platform completa en verde.
