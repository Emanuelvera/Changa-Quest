---
title: 'Integrar el frontend del especialista con la aplicación Changa Quest'
type: 'feature'
created: '2026-10-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c672fbe625bc4244246d126ba8f6794a01370d01'
context:
  - '{project-root}/docs/METODOLOGIA.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-redisenio-visual-frontend.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El proyecto del especialista es una demo aislada: autentica y modifica datos en memoria, sin operar sobre cuentas ni changas reales.

**Approach:** Adoptar su presentación en el cliente Blazor WebAssembly existente y conectar cada acción a los endpoints reales, conservando reglas, sesión por cookie, privacidad, errores y estados persistidos. Crear una rama de trabajo nueva desde `origin/master`, sin modificar la rama visual existente hacia `dev`.

## Boundaries & Constraints

**Always:** Mantener el cliente .NET 10 WebAssembly, llamadas HTTP de mismo origen y cookie. Usar `Guid` reales, no `DemoStore` ni datos ficticios como estado. Preservar rutas, reglas, privacidad, errores y accesibilidad responsive. Incluir un formulario para enviar reportes a la API existente, sin panel administrativo. Seguir SDD/TDD: probar en rojo antes de producción.

**Never:** Copiar la app .NET 8 como servidor paralelo; cambiar API/reglas, añadir persistencia, simular éxitos, exponer datos privados o construir un panel administrativo. No alterar el proyecto externo.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| SESIÓN | Usuario con/sin cookie | Login, registro y rutas usan la sesión real | Mostrar 401/403; sin sesión ficticia |
| CHANGAS | Búsqueda, detalle o acción | API real con `Guid`; conservar formulario ante error | Validación/conflicto visibles; sin falso éxito |
| COORDINACIÓN | Consentimiento, cancelación, cierre o pago | Datos autorizados y estados de API | Mantener estado del servidor ante conflicto |
| PERFIL / REPORTE | Perfil, bloqueo o envío de reporte | Datos agregados; confirmar bloqueo; enviar motivo y evidencia opcional | Errores explícitos; no afirmar éxito falso |

</frozen-after-approval>

## Code Map

- `C:\Users\emanu\OneDrive\Desktop\ChangaQuest-Blazor\ChangaQuest\Components\Pages\` -- referencia visual; no trasladar `DemoStore` ni acciones simuladas.
- `src/client/ChangaQuest.Client/` -- cliente WASM real; conservar `Program.cs`, `App.razor`, HTTP same-origin y hosting de API.
- `src/client/ChangaQuest.Client/Pages/*.razor` -- rutas y operaciones existentes; alinear presentación sin perder sus llamadas.
- `src/client/ChangaQuest.Client/Layout/`, `wwwroot/css/app.css` -- shell/tema vigente; incorporar componentes responsive y estilos del especialista.
- `src/layers/ChangaQuest.Presentation/{Changas,Accounts,Reports}/` -- contratos REST existentes para changas, perfil, bloqueo (Story 3.2) y reportes.
- `tests/ChangaQuest.AcceptanceTests/Client/` -- pruebas bUnit actuales de páginas, errores, privacidad y bloqueo.
- `specs/api/openapi-v1.yaml` -- contrato fuente; editar solo si se requiere una operación nueva.

## Tasks & Acceptance

**Execution:**
- [x] `tests/ChangaQuest.AcceptanceTests/Client/` -- añadir y ejecutar primero pruebas rojas de pantallas, API, errores y privacidad.
- [x] `src/client/ChangaQuest.Client/Pages/*.razor` -- adoptar pantallas conservando DTOs, endpoints, rutas `Guid` y validación.
- [x] `Layout/` y `wwwroot/css/app.css` -- adaptar shell, componentes y estilos responsive/accesibles.
- [x] Cliente y pruebas -- añadir formulario de reporte conectado a la API; validar build y suite Microsoft.Testing.Platform.

**Acceptance Criteria:**
- Given una ruta del prototipo, when se abre en el cliente integrado, then presenta el diseño nuevo y opera sobre datos API.
- Given una acción protegida, when falta sesión o falla la solicitud, then se informa el error sin falsear éxito ni perder los datos del formulario.
- Given coordinación y cierre, when se consulta o cambia el estado, then se preservan privacidad, consentimiento, plazos y reglas del backend.
- Given perfil/bloqueo y reporte, when se usan, then muestran agregados, requieren confirmación explícita para bloquear y nunca ocultan errores.
- Given una persona en móvil, teclado o zoom, when navega, then mantiene rutas, foco, legibilidad y acciones accesibles.

## Implementation Notes

- Se conservó el cliente WASM y su `HttpClient` de mismo origen; las acciones de las pantallas continúan usando los endpoints existentes.
- Se adoptó la identidad visual oscura azul/dorada del especialista para el shell y los componentes existentes, manteniendo navegación móvil, foco visible y estados de carga/error.
- Se agregó el envío privado de reportes asociado a una changa, con motivo obligatorio, evidencia textual/enlace opcional y sin mostrar datos del reporte a otras personas.
- Se separaron visualmente los estados informativos de las confirmaciones de éxito para evitar mostrar estados pendientes como exitosos.
- La referencia `spec-redisenio-visual-frontend.md` indicada en `context:` no existe en el worktree actual; no se modificó ningún archivo del proyecto externo.

## Spec Change Log

## Review Triage Log

| Verdict | Route | Evidence |
|---|---|---|
| medium | patch | `.status` estaba agrupado con `.success-message` y teñía de verde mensajes de carga, espera y revisión. Se separó el estilo neutral y se añadió una prueba de contrato CSS que falló antes del cambio y pasó después. |

## Verification

**Commands:**
- `dotnet build` -- esperado: compilación correcta de la solución.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- esperado: todas las pruebas existentes y nuevas pasan.

**Resultado:** `dotnet build` terminó correctamente y la suite Microsoft.Testing.Platform informó 217 pruebas correctas, 0 errores y 0 omitidas. Las pruebas nuevas de reporte se ejecutaron primero en rojo y luego en verde. La prueba de contrato visual del estado también se ejecutó en rojo antes del ajuste y en verde después.
