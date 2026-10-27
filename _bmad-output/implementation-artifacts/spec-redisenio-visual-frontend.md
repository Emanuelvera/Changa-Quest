---
title: 'Rediseño visual oscuro de todas las pantallas del frontend'
type: 'feature'
created: '2026-10-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '83dc4fdd3b37ad6696dd10fdb5b79dc96414f53d'
context:
  - '{project-root}/docs/METODOLOGIA.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/DESIGN.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/EXPERIENCE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La aplicación sirve para la demo, pero su interfaz no refleja la identidad visual de aventura moderna de la referencia.

**Approach:** Aplicar a todas las pantallas actuales una dirección visual oscura, adulta y coherente: azul pizarra nocturno, superficies elevadas, acentos ámbar/latón y ambientación sobria. Mantener el lenguaje, los datos, los flujos y la privacidad claros; la fantasía es solo visual.

## Boundaries & Constraints

**Always:** Diseño responsive mobile-first; preservar destinos y orden de navegación (Ver tareas, Publicar changa, Mis changas, Perfil), textos funcionales, datos y comportamiento. Mantener etiquetas y errores accesibles, foco visible, teclado, legibilidad con zoom, estados expresados con texto además de color y preferencia de movimiento reducido. Objetivo de contraste: al menos 4.5:1 para texto normal y 3:1 para texto grande y controles relevantes; objetivos de referencia, no declaración automática de conformidad integral WCAG.

**Never:** Cambiar API, reglas de negocio, persistencia o contenido funcional; ocultar datos esenciales bajo ilustraciones; usar lenguaje de juego para acciones cotidianas, urgencia ficticia o insignias que sugieran seguridad no comprobada; añadir dependencias de UI sin necesidad; copiar franquicias o calcar la referencia.

</frozen-after-approval>

## Code Map

- `src/client/ChangaQuest.Client/wwwroot/css/app.css` -- estilos globales existentes para formularios, tarjetas, estados, foco, navegación móvil y movimiento reducido; punto central para tokens y componentes.
- `src/client/ChangaQuest.Client/Layout/MainLayout.razor` y `Layout/MainLayout.razor.css` -- marca, shell y cuatro destinos persistentes; definir aquí la identidad compartida sin cambiar rutas.
- `src/client/ChangaQuest.Client/Pages/*.razor` -- acceso, tablón/detalle, publicación, coordinaciones, coordinación, finalización, calificación, perfil y no encontrado; conservar lógica y estructura funcional.
- `tests/ChangaQuest.AcceptanceTests/Client/ChangaPageTests.cs` -- pruebas bUnit existentes de navegación y pantallas; ampliar estructura/atributos del shell y conservar sus verificaciones de comportamiento.
- `tests/ChangaQuest.AcceptanceTests/Client/HomePageTests.cs` -- pruebas bUnit del acceso, registro y errores; preservar contratos de campos y anuncios.
- `_bmad-output/planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/DESIGN.md` -- propuesta de tokens oscuros y componentes hoy provisional; actualizar su estado/decisiones al implementar.
- `_bmad-output/planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/EXPERIENCE.md` -- reglas de navegación móvil, lenguaje, accesibilidad y estados que prevalecen sobre el mockup.
- `docs/METODOLOGIA.md` y `ARCHITECTURE-SPINE.md` -- obligan a especificar primero y a probar antes de producción.

## Tasks & Acceptance

**Execution:**
- [x] `tests/ChangaQuest.AcceptanceTests/Client/ChangaPageTests.cs` -- probar primero el shell visual compartido y navegación accesible, demostrando fallo antes de producción.
- [x] `src/client/ChangaQuest.Client/wwwroot/css/app.css`, `Layout/MainLayout.razor`, `Layout/MainLayout.razor.css` -- establecer tokens, shell, navegación, superficies, formularios, estados y adaptaciones responsive.
- [x] `src/client/ChangaQuest.Client/Pages/*.razor` -- aplicar clases compartidas o específicas solo donde el CSS actual no cubra las superficies, sin tocar lógica ni contratos de datos.
- [x] `_bmad-output/planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/DESIGN.md` -- registrar dirección oscura aprobada, tokens finales y contraste validado.

**Acceptance Criteria:**
- Given cualquier ruta existente, when se representa dentro del layout, then comparte la identidad oscura, el foco es visible y los cuatro destinos de navegación conservan su orden y nombre.
- Given pantallas de acceso, tablón, detalle, publicación, coordinación, finalización, calificación y perfil, when se presentan en móvil o escritorio, then campos, tarjetas, estados, recompensas y acciones siguen siendo legibles, utilizables y no se recortan; la navegación móvil queda disponible en la parte inferior.
- Given texto normal, texto grande o controles sobre sus superficies, when se verifica su contraste, then alcanza los objetivos definidos en los límites y los estados siguen identificándose mediante texto, no solo color.
- Given una persona que usa teclado, zoom o movimiento reducido, when navega por cualquiera de las pantallas, then conserva foco visible, operación por teclado, contenido adaptable y ausencia de animación no esencial.
- Given el flujo funcional existente, when se ejecutan las pruebas de páginas, then formularios, errores, estados, privacidad y llamadas permanecen sin cambios observables.

## Implementation Notes

Implementado sobre `story/3.2` (base `83dc4fdd3b37ad6696dd10fdb5b79dc96414f53d`) en la rama aislada `feature/redisenio-visual-chat`. Los nuevos tests de bUnit se ejecutaron primero en rojo: faltaban el identificador de tema del layout y el contenedor responsive de Not Found. Luego se implementó el tema global, el shell compartido y los estilos de navegación, formularios, tarjetas y estados sin modificar API ni reglas de negocio. La navegación móvil ocupa la parte inferior y se respetan foco visible y movimiento reducido.

Validación visual automatizada de tokens: contraste de 15.46:1 para Warm Ink sobre Night Slate, 12.92:1 en Raised Slate, 9.52:1 para Muted Ink sobre Raised Slate y 8.23:1 para Brass Ink sobre Brass. Los tres colores semánticos superan 4.5:1 en sus superficies tintadas. Se comprobaron también los breakpoints, el tema, el foco por teclado y movimiento reducido. La app y el CSS devolvieron HTTP 200 en el smoke test. No se pudo completar una inspección visual con navegador/capturas en este entorno.

En la rama inicial basada en `story/3.2`, la suite pasó 211 de 212 pruebas; la única falla fue `AccountBlockApiTests.Blocking_is_confirmed_by_the_client_request_and_is_idempotent` (esperaba `Created`, recibió `InternalServerError`). Al preparar el PR aislado sobre `dev`, la suite completa pasó 205 de 205 pruebas; el caso de AccountBlocks no forma parte del estado de `dev`.

## Spec Change Log

## Review Triage Log

- **medium — patch (brecha de verificación):** no había una prueba ejecutable que protegiera `position: fixed` y `bottom: 0` de `.primary-nav` en el breakpoint móvil; el test bUnit solo verifica DOM y orden. Se agregó una prueba del bloque CSS móvil que falla si se pierde el anclaje inferior.
- **medium — patch (brecha de verificación):** no había una prueba repetible del contraste para los estados semánticos usados por la interfaz. Se agregó validación de las declaraciones CSS y del contraste calculado de éxito, advertencia y error contra 4.5:1.

## Verification

**Commands:**
- `dotnet build` -- éxito, 0 errores; reportó advertencias de obsolescencia `Bunit.TestContext` en pruebas preexistentes.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` -- 211 correctas y 1 falla preexistente de AccountBlocks.
- Contratos CSS añadidos: navegación inferior en el breakpoint móvil y contraste calculado de estados semánticos -- ambos pasan.
- `git diff --check` -- éxito.
- Comprobación automatizada de tokens, breakpoints, foco, movimiento reducido y ratios de contraste -- éxito.
- Smoke test HTTP de la app y del CSS -- ambos respondieron 200.
- Inspección visual en navegador y validación de desbordamiento en todas las rutas -- pendiente; no hubo una captura utilizable en este entorno.
