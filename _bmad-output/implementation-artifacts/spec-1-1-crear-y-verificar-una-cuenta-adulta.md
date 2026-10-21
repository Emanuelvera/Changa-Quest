---
title: "Story 1.1 — Crear y verificar una cuenta adulta"
type: feature
created: 2026-10-20
status: in-review
route: dispatch
review_loop_iteration: 0
baseline_commit: UNBORN_HEAD
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Sin alta, confirmación de correo e inicio de sesión, las historias posteriores no pueden identificar a las partes ni impedir acciones de cuentas no elegibles.

**Approach:** Registrar cuentas con correo, contraseña y declaración de 18+; habilitar el acceso solo tras verificar el correo. Una cuenta puede solicitar y realizar changas. La demo es local y usa datos ficticios.

## Boundaries & Constraints

**Always:** Seguir N-Layers y SDD/TDD: pruebas antes del código. Usar ASP.NET Core Identity, EF Core/SQL Server y el contrato OpenAPI `/api/v1`. En Development, mostrar el enlace de verificación en la consola del backend, sin enviar correo externo. Exigir contraseña de al menos 12 caracteres, sin reglas obligatorias de composición. Guardar solo la declaración 18+, no fecha ni documentos de identidad.

**Never:** Enviar correos reales ni habilitar tareas, contactos o pagos reales. No autenticar cuentas no verificadas o sin declaración 18+, crear roles excluyentes, ni guardar credenciales/tokens en almacenamiento accesible a JavaScript.

## I/O & Edge-Case Matrix

| Caso | Entrada/estado | Resultado | Error |
|---|---|---|---|
| Registro elegible | Email, contraseña, declaración 18+ | Cuenta pendiente; mensaje local; sin sesión | No mostrar éxito si falla |
| No elegible | Menor o declaración omitida | Sin cuenta habilitada | Error asociado al campo |
| Email duplicado/inválido | Email existente/inválido o contraseña menor a 12 caracteres | Sin cuenta duplicada ni alta | Validación clara sin datos sensibles |
| Login pendiente | Email no verificado | Acceso denegado; indicar verificación | Sin sesión |
| Token válido | Confirmación del email | Email confirmado; puede iniciar sesión | Token inutilizable después |
| Token inválido | Incorrecto, vencido o usado | Cuenta sin cambios | Informar que el enlace no es válido |

</frozen-after-approval>

## Code Map

- No hay solución, proyectos, autenticación ni pruebas de producto; se crea la estructura de arquitectura.
- `epics.md` — AC de Story 1.1.
- `epic-1-context.md` — límites de Epic 1.
- `ARCHITECTURE-SPINE.md` — N-Layers, contrato `/api/v1`, stack y TDD.

## Tasks & Acceptance

**Execution:**
- [x] `ChangaQuest.sln`, `src/`, `tests/` — crear proyectos N-Layers, cliente Blazor y suites xUnit v3.
- [x] `tests/ChangaQuest.UnitTests/Accounts/`, `tests/ChangaQuest.IntegrationTests/Accounts/`, `tests/ChangaQuest.AcceptanceTests/Accounts/` — escribir Red primero para elegibilidad, persistencia/confirmación SQL y flujos HTTP, incluyendo errores y acceso previo a verificación.
- [x] `specs/api/openapi-v1.yaml`, `tests/ChangaQuest.AcceptanceTests/Contracts/` — definir y comprobar contrato de registro, confirmación y login antes de implementar API y cliente.
- [x] `src/layers/ChangaQuest.Entities/`, `DataAccess/`, `BusinessLogic/`, `Presentation/`, `src/backend/ChangaQuest.Api/` — implementar usuario, persistencia Identity y endpoints; DbContext solo en DataAccess.
- [x] `src/client/ChangaQuest.Client/` — implementar registro, confirmación, login y navegación accesible tras autenticarse.

**Acceptance Criteria:**
- Given una cuenta cumple 18+ y verifica su email, when inicia sesión, then puede operar como solicitante o realizadora.
- Given la cuenta no verificó email o no declaró 18+, when intenta autenticarse, then el acceso se deniega.
- Given error, email duplicado o datos inválidos, when envía el formulario, then recibe un error y no un falso éxito.
- Given uso con teclado o ampliación, when recorre alta y login, then etiquetas, foco y errores siguen accesibles.

## Implementation Notes

SDK instalado: 10.0.401; runtimes .NET/ASP.NET Core: 10.0.12. LocalDB `MSSQLLocalDB` está disponible. La arquitectura fija 10.0.402/10.0.13: verificar compatibilidad al restaurar/probar. No existen proyectos. Para la demo, servir Blazor desde el host local de la API y autenticar con cookie Identity `HttpOnly` de mismo origen; no usar almacenamiento persistente del navegador para tokens. Migraciones aditivas; no borrar datos.

## Spec Change Log

## Review Triage Log

- **medium → patch (resuelto):** Presentation y el host API dependían directamente de DataAccess/DbContext, apartándose de N-Layers. El registro de persistencia y las migraciones quedaron en DataAccess, y una prueba verifica las dependencias permitidas.
- **medium → patch (resuelto):** La confirmación con token inválido respondía HTML con HTTP 400 en lugar de Problem Details. Ahora devuelve `application/problem+json` con un código estable y la prueba de aceptación lo comprueba.
- **medium → patch (resuelto):** Los desafíos de cookie redirigían rutas API y podían aparentar un 200 tras seguir la redirección. Ahora devuelven 401/403 con Problem Details; la aceptación cubre `/me` y `logout` sin sesión.
- **low → patch (resuelto):** El error de registro por desconexión afirmaba que los datos no se enviaron aunque el servidor pudiera haberlos recibido. El mensaje ahora evita esa afirmación.
- **medium → patch (resuelto):** Los errores locales de validación no estaban asociados por `aria-describedby` a todos los campos de registro y la declaración adulta podía apuntar a un ID inexistente. Se corrigieron las referencias y el ID del texto de ayuda.
- **medium → patch (resuelto):** Un `.slnx` vacío junto con proyectos de capa duplicados hacía fallar `dotnet test` sin argumento por ambigüedad. Se conservó la solución canónica y se quitaron los scaffolds duplicados.

## Verification

**Commands:**
- `dotnet build ChangaQuest.sln --nologo` — compila todos los proyectos de la solución.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` — esperado: todas las pruebas unitarias, de integración y de aceptación pasan contra SQL Server LocalDB.
