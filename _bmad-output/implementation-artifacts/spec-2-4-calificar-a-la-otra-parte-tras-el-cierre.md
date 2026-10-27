---
title: "Story 2.4 — Calificar a la otra parte tras el cierre"
type: feature
created: 2026-10-25
status: done
route: dispatch
review_loop_iteration: 0
baseline_commit: df364f6f2e4d9f499ceca91604d8050872c7def2
context:
  - "{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md"
  - "{project-root}/_bmad-output/planning-artifacts/epics.md"
  - "{project-root}/_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md"
  - "{project-root}/docs/METODOLOGIA.md"
---

<frozen-after-approval reason="intención confirmada por la persona usuaria">

## Intent

**Problema:** Las partes no pueden dejar una señal agregada de experiencia después de completar una changa, y los perfiles no muestran calificaciones recibidas.

**Enfoque:** Permitir que cada parte de una changa cerrada normalmente envíe una nota entera de 1 a 5, una sola vez y sin texto. Mostrar en el perfil de la persona calificada únicamente promedio y cantidad.

## Boundaries & Constraints

**Always:** Solo las dos partes de la changa pueden calificar; cada una califica a la otra, una vez por changa. BusinessLogic decide elegibilidad, autorización y validación; DataAccess persiste y protege la unicidad. No exponer quién calificó. El cierre normal es el estado producido por Story 2.3 cuando ambas partes registraron sus declaraciones; la confirmación de finalización de Story 2.1 por sí sola no habilita calificar.

**Never:** No habilitar por cancelación, revisión pendiente ni decisión administrativa de completado (fuera de la demo). No implementar declaraciones/pago ni simular el cierre de Story 2.3. No aceptar comentarios, texto libre ni notas fuera de 1–5. No publicar identidad del evaluador.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| CALIFICAR | Parte de changa cerrada normalmente envía entero 1–5 | Se registra una nota para la otra parte | N/A |
| NO_ELEGIBLE | Changa asignada, confirmada pero no cerrada, cancelada o en revisión | No se registra nota | `409`, código estable de calificación no disponible |
| NO_PARTE | Cuenta ajena intenta calificar | Sin cambios | `403`, código estable de acceso denegado |
| INVALIDA | Nota decimal o fuera de 1–5 | Sin cambios | `400`, error de validación |
| DUPLICADA | La misma parte vuelve a calificar la misma changa | Se conserva una sola nota | `409`, código estable de calificación duplicada |
| PERFIL_CON_NOTAS | Perfil registrado con calificaciones recibidas | Promedio y cantidad; sin identidades de evaluadores | N/A |
| PERFIL_SIN_NOTAS | Perfil sin calificaciones | Estado vacío, sin promedio inventado | N/A |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- dueño de reglas y ciclo de vida; reutilizar autorización por partes y resultados de negocio. La elegibilidad de rating debe depender del cierre normal de 2.3, no de `Confirmed` en 2.1.
- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- modelo de changa y asignación; agregar entidad de calificación sin extender datos públicos de la changa con el autor del rating.
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs`, `src/layers/ChangaQuest.DataAccess/Changas/` y `Migrations/` -- persistencia EF Core, configuración e índice único por changa y evaluador; seguir migraciones aditivas.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` y `src/layers/ChangaQuest.Presentation/Accounts/AccountEndpointExtensions.cs` -- validar contrato y delegar; extender perfiles con solo promedio/cantidad.
- `specs/api/openapi-v1.yaml` -- definir primero las operaciones, schemas y errores de rating/perfil.
- `tests/ChangaQuest.UnitTests/Changas/ChangaManagerTests.cs`, `tests/ChangaQuest.IntegrationTests/Changas/ChangaPersistenceTests.cs`, `tests/ChangaQuest.AcceptanceTests/Changas/ChangaApiTests.cs` y `tests/ChangaQuest.AcceptanceTests/Contracts/OpenApiContractTests.cs` -- reutilizar patrones de autorización/estado, unicidad persistida, aceptación y contrato.
- `src/client/ChangaQuest.Client/Layout/MainLayout.razor`, `src/client/ChangaQuest.Client/Pages/` -- el UX define la navegación «Perfil» pero aún no existe página; agregar la vista propia y la vista de otra cuenta con solo promedios, y enlazarla desde la coordinación privada.
- Story 2.1 `in-review` -- continuidad: transiciones atómicas, autorización de las partes, respuestas Problem Details y prueba con reloj controlado. No modificar su alcance ni su estado.

## Tasks & Acceptance

**Execution:**
- [x] `specs/api/openapi-v1.yaml` -- definir contrato de envío, lectura agregada y errores -- contrato primero.
- [x] `tests/` -- añadir pruebas de matriz y confirmar fallos antes de producción -- ciclo Red.
- [x] `Entities`, `DataAccess` -- agregar rating persistente, migración aditiva e índice único -- integridad por changa/evaluador.
- [x] `BusinessLogic`, `Presentation` -- implementar permisos, elegibilidad, validación y agregación -- reglas solo en BusinessLogic.
- [x] `src/client/ChangaQuest.Client/Pages/Calificacion.razor`, `Perfil.razor` y `Layout/MainLayout.razor` -- integrar control de nota y resumen de perfil -- UX-DR3, DR11, DR14–DR16.
- [x] Refactorizar con las pruebas de Story 2.4 en verde, documentar la única falla preexistente ajena a esta historia y actualizar `sprint-status.yaml` -- cierre de implementación.

**Acceptance Criteria:**
- Given una changa cerrada normalmente por el flujo de 2.3, when cada parte califica, then cada una registra como máximo una nota entera 1–5 para la otra parte, sin texto.
- Given una changa cancelada, pendiente de revisión o aún no cerrada normalmente, when una parte intenta calificar, then no se registra una nota.
- Given una persona registrada con notas recibidas, when consulta su perfil, then ve promedio y cantidad sin identidad de quienes calificaron.
- Given una persona sin notas recibidas, when consulta su perfil, then ve un estado vacío y no un promedio ficticio.
- Given que se envió una calificación, when la misma parte intenta repetirla para esa changa, then el sistema conserva una sola nota.
- Given una persona usa la calificación o perfil con teclado o ampliación, when navega por la interfaz, then encuentra etiquetas comprensibles, foco visible y estados no dependientes solo del color.

## Implementation Notes

- La rama de trabajo es `story/2-4-calificar-a-la-otra-parte-tras-el-cierre`, creada desde `dev`.
- El sprint dry-run confirmó `in_sync: true`; la historia y el épico quedaron `in-progress` al iniciar el trabajo.
- Dependencia aceptada: desarrollar contra el contrato esperado de Story 2.3 y dejar explícita la integración pendiente. En `dev` no existe todavía cierre normal end-to-end; no usar el estado `Confirmed` como sustituto.
- Decisiones de alcance confirmadas: solo cierre normal habilita rating; no se incluye cierre administrativo ni el trabajo de Story 2.3.

## Spec Change Log

## Review Triage Log

- `medium / defer` — La ruta real que lleva a `Closed` pertenece a Story 2.3 y está pendiente; la prueba de rating prepara ese estado directamente. Mantener la elegibilidad atada a `Closed` y ejecutar una prueba end-to-end al integrar 2.3; se registra en `deferred-work.md`.
- `medium / defer` — La aceptación cambia `Changas.Status` por SQL en vez de ejecutar el cierre normal. Es la misma dependencia externa: no implementar ni simular 2.3 dentro de esta historia; validar la transición real después de integrarla.
- `medium / patch` — El spec estaba en `in-review` mientras el sprint seguía `in-progress`; ambos estados se sincronizaron a `review`.
- `medium / patch` — El endpoint ignoraba propiedades JSON adicionales, permitiendo enviar comentarios pese al contrato. Ahora exige el objeto con únicamente `score`; la prueba API confirma `400` para `comment` y que luego se puede registrar la nota.
- `false` — Se señaló que el cambio de parámetro de ruta podía conservar la elegibilidad vieja; la navegación real entre IDs en `App` pasa la prueba de aceptación sin mostrar el estado anterior.
- `low / patch` — El enlace prometía calificar antes de saber si estaba habilitado; se reescribió como «Consultar disponibilidad para calificar».
- `false` — Se afirmó que no se cubrían cancelación y revisión; la teoría `Rating_is_unavailable_without_normal_close` ejecuta `Cancelled`, `CompletionReview` y `PendingReview`, además de otros estados no elegibles.
- `medium / patch` — Faltaba verificar autenticación en los endpoints de perfiles; se añadieron aserciones `401` para ambos perfiles y pasan en aceptación.
- `medium / patch` — Faltaba comprobar el perfil inexistente; se añadió la comprobación `404` y pasa en aceptación.
- `false` — La cobertura de estados de la pantalla ya incluía no elegible y envío correcto; se añadieron además casos de nota ya enviada y errores de carga/envío con reintento, que pasan.
- `medium / patch` — La revisión de casos límite confirmó que aceptar campos JSON extra permitía texto ajeno al contrato; resuelto junto con el hallazgo equivalente anterior.
- `medium / patch` — La elegibilidad podía indicar `CanRate` con `AuthorId` vacío aunque no hubiera destinatario; ahora comparte la resolución del destinatario con el envío y la prueba unitaria confirma que queda no elegible.
- `false` — La hipótesis de elegibilidad obsoleta al navegar entre changas no se reproduce al cambiar el ID desde el router de la aplicación; la prueba de navegación pasó sin modificar la página.
- `false` — La hipótesis de que una respuesta tardía reemplaza otro perfil no se reproduce al navegar entre cuentas desde el router; la prueba de navegación pasó sin modificar la página.
- `medium / defer` — La revisión de verificación observó que una regresión en el cierre normal no sería detectada por los tests de esta historia, que fuerzan `Closed`. Se conserva como pendiente de integración con Story 2.3, fuera del alcance actual.
## Design Notes

El rating pertenece al par (changa, evaluador), y el receptor es la otra parte asignada. La restricción única persistida es necesaria además de la comprobación de BusinessLogic para evitar duplicados concurrentes. La respuesta de perfil agrega ratings recibidos y omite filas individuales y cualquier identificador de evaluador. La integración de elegibilidad se acopla al estado de cierre normal que establezca la Story 2.3; confirmar la realización en 2.1 no es suficiente.

## Verification

**Commands:**
- `dotnet build --nologo` -- correcto.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- 149/150 correctas; queda una falla preexistente y no relacionada en `ChangaApiTests.Board_filters_by_radius_and_returns_approximate_distance_without_exact_address`.
- Las suites de unidad e integración terminaron correctamente, incluidas las pruebas de elegibilidad, privacidad, agregación y unicidad concurrente de rating. Las pruebas enfocadas de API y cliente pasan; incluyen propiedades JSON adicionales, perfiles anónimos/inexistentes, estado ya calificado y reintentos.
- La migración `AddChangaRatings` se generó y actualizó el snapshot de EF Core.
- La implementación de Story 2.4 está completa y el sprint queda en `review`; la prueba end-to-end con el cierre normal de Story 2.3 sigue diferida hasta integrar esa historia.
