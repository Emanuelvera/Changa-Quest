---
name: Changa Quest
description: Contrato de navegación y comportamiento para descubrir, solicitar y completar changas locales.
status: draft
sources:
  - {planning_artifacts}/briefs/brief-Changa-Quest-2026-10-16/brief.md
  - {planning_artifacts}/briefs/brief-Changa-Quest-2026-10-16/addendum.md
  - {planning_artifacts}/prds/prd-Changa-Quest-2026-11-01/prd.md
  - {planning_artifacts}/prds/prd-Changa-Quest-2026-11-01/addendum.md
updated: 2026-10-20
---

## Foundation

- **Form factor:** responsive web application, designed mobile-first and usable on a laptop. The academic demo runs locally; a native mobile app is outside the initial scope.
- **UI system:** none selected. This experience contract describes behavior, not a component-library choice. Visual tokens and rationale live in [DESIGN.md](./DESIGN.md).
- **Product posture:** make it easy to ask for help or find a nearby changa without requiring familiarity with fantasy or marketplace conventions. Practical task information and safety come before atmosphere.
- **Sources:** the product brief and PRD, including their addenda, are inherited through `sources:` above. Their product requirements take precedence over illustrative examples here.
- **Launch boundary:** screens for trust, reports, blocking, and administrative review describe the real product target. The academic demo may use fictional data and omit those features; no real task, contact, payment, or location may be implied as operational in that demo.
- **Academic demo scope:** follow the PRD's local, email-verified end-to-end flow with manual location and first-come assignment only. Omit selection, GPS/maps, chat, payment processing, reports, blocking, and administrative resolution. A rejected or unanswered completion remains pending review without automatic closure or reassignment.

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Access and account | App entry | Register, verify email, and declare age as required by the PRD. Additional identity checks remain open for a real launch. |
| Tablón / Ver tareas | Main navigation | Discover available changas by search location and radius; expose a clear path to publish for people who need help. |
| Changa detail | Task card | Review description, reward, approximate zone, assignment mode, and tools/materials before taking or applying. Keep exact address hidden until assignment. |
| Publicar changa | Main action | Enter the task, reward, location, materials, and assignment mode with plain-language guidance. |
| Mis changas | Main navigation | Follow a person's own requests, applications, assignments, completion/payment declarations, and ratings. Distinguish each state with a label. |
| Profile | Main navigation | Show account details and the average/count of received ratings; do not reveal who gave each rating. |
| Administrative review | Restricted staff access | Review reports and completion disputes, capture reasons/evidence, hear both parties, record the outcome, and support appeal. This is not a general user destination. |

One account may act as requester in one task and worker in another. The app therefore treats “solicitar” and “realizar” as roles per task, not separate profiles. The primary navigation is: “Ver tareas”, “Publicar changa”, “Mis changas” and “Perfil”.

There is no in-app chat. Phone numbers are hidden until assignment and only exposed after both parties explicitly consent. A dedicated notification surface/channel is not defined; show status in the relevant task context until Product decides how required notifications are delivered.

## Voice and Tone

The interface speaks in plain, respectful Spanish for Argentina. Keep the fantasy influence visual; use ordinary words for navigation, tasks, money, safety, and decisions. **[ASSUMPTION]** Use voseo consistently, subject to UX copy review.

| Prefer | Avoid |
|---|---|
| “Publicar changa” | “Emitir una misión” |
| “Ver tareas” | “Aceptar aventura” |
| “La dirección se comparte solo con la persona asignada.” | “Tu secreto está protegido por el gremio.” |
| “Todavía no hay changas en esta zona.” | “¡No hay aventuras, héroe!” |
| “La persona solicitante tiene 20 minutos para responder.” | “¡El tiempo corre!” |
| “El pago se declara en la app; no lo procesamos ni verificamos.” | “Pago asegurado” or “Pago confirmado” when only one person has declared it |

Use short, specific explanations where a person must share sensitive information or make a commitment. Do not blame either party in status messages while a review is pending.

## Component Patterns

Behavioral rules below are paired with visual specifications in [DESIGN.md](./DESIGN.md). An unresolved item is named rather than silently assigned behavior.

| Component | Use | Behavioral rules |
|---|---|---|
| Changa card | Board results | Make description/title, ARS reward, approximate zone, materials, and assignment mode available before commitment. Tap opens detail; never place the exact address on a public card. |
| Search location and radius | Board | For the academic demo, use manual location only, without GPS or maps, with a 3 km default and 1, 3, 5, and 10 km options. The broader product may offer GPS only after an explicit action and must not retain or track a precise GPS location. Distances are approximate and shown only for board context. |
| Publish form | New request | Collect description, reward, task location, assignment mode, and an explicit materials-needed/supplied answer. Explain required fields in context; prevent publishing until required materials information is complete. |
| Assignment action | Detail / applications | For the academic demo, first-come tasks cannot be taken by their requester; submit one assignment attempt and clearly report if another person already took the task. For selection tasks in the broader product, show applicant status and the selected/non-selected result; do not imply that an application is an assignment. |
| Contact consent | Assigned task | Collect each party's explicit choice independently. The app asks for consent only after assignment, and it reveals the phone number only when both parties agree. One party's refusal remains private until the other also decides; no automatic re-share occurs. |
| Completion response | Assigned task | Let the worker mark work complete. Show the requester the 20-minute response window and choices to confirm or reject. Rejection or expiry starts administrative review; do not auto-close or auto-reassign. |
| Payment declarations | Closed/assigned task | After completion confirmation, record a separate yes/no statement that the published reward was paid or received; do not collect another numeric amount. Present each declaration as that person's statement, not verified payment. Normal closure requires both statements; admin completion is the documented exception. |
| Rating form | Completed task | Allow one 1–5 rating per party after normal closure or administrative completion. No written comment; profile shows average and count, not reviewer identity. |
| Report/block controls | Profile, content, interaction | Explain what will be submitted before sending a report. Confirm a block action and do not suggest that it cancels or safely resolves an already-assigned task. Reports are submitted with a reason and optional evidence; a block only prevents future contact. |
| Review record | Restricted admin surface | Capture reasons and evidence, both parties' responses, decision, and appeal. Make the status/outcome visible to the affected parties without exposing private evidence to other users. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Loading board | Tablón | Preserve the page structure while results load; do not show fabricated nearby tasks as real. |
| No results | Tablón | Say that no changas match this area/radius and offer to change the search area. Do not silently broaden the radius. |
| Location unavailable | Search | Explain that GPS was not used and offer manual entry. Do not repeatedly prompt for location permission. |
| Task already taken | Detail / assignment | Explain that availability changed and return the person to the refreshed board. Do not present the attempted assignment as successful. |
| Application not selected | Mis changas | Mark the application as not selected when the requester chooses another applicant. The delivery channel for the required notification is still undecided. |
| Consent incomplete | Assigned task | For the academic demo, the assignment remains active, both numbers stay hidden, and there is no automatic cancellation or alternate contact path. The product-real continuation rule remains open under PRD OQ-4. |
| Worker marked complete | Requester's task view | Show when the 20-minute response period ends and offer confirm/reject. **[ASSUMPTION]** Display a precise deadline and remaining time; validate what happens when the user is offline or the deadline passes across time zones/devices. |
| Rejected / no response | Task view | State that administrative review is required and that the task has not been automatically closed or reassigned. Avoid assigning blame while evidence and responses are considered. |
| Admin review | Affected parties' task view | Show that a review is in progress and whether a response or appeal is due. Do not imply a response deadline until the operating policy defines it. |
| Admin finds work incomplete | Task view / board | After the recorded decision, release the assignment and return the changa to available/unassigned status. A possible sanction is reviewed separately; do not display a sanction as imposed unless the process decides it. |
| Admin finds work complete | Task view | Close as completed even if payment is undeclared, clearly label payment as unverified, and allow mutual ratings. |
| Payment declaration pending | Task view | Show each declaration separately and explain which one is missing; never infer that one party's statement proves receipt by the other. |
| Empty personal activity | Mis changas | Explain whether there are no published, applied-to, or assigned changas and give a direct route to the relevant next action. |
| Service/error state | Any | Preserve entered form data where safe; identify the action that failed and offer a retry without claiming it succeeded. |

## Interaction Primitives

- Use direct verbs and predictable tap/click behavior. A card opens details; a separately labeled action commits to taking or applying.
- Require deliberate confirmation before publishing, taking a task, sharing a phone, cancelling, submitting a report, or blocking someone.
- Explain consequences near the action, especially for public location, contact sharing, task commitment, and external payment declarations.
- Format rewards in ARS and distinguish the offered reward from any amount a person later declares paid.
- GPS is opt-in at the point of use. Manual location entry remains available; precise GPS is not retained or used for tracking.
- Prevent duplicate first-come assignments under simultaneous attempts. A stale screen must not make a taken task appear successfully assigned.
- Do not use an animation, badge, sound, streak, or celebratory reward as a substitute for a clear task status.
- **Not in scope:** in-app chat, payment processing, payment verification, or automatic dispute resolution.

## Accessibility Floor

**[ASSUMPTION — target for validation]** Design toward WCAG 2.2 AA for the responsive web experience; the PRD leaves final accessibility criteria to UX and the team.

- All actions work with keyboard as well as touch/mouse; focus order follows the page's reading order and focus remains visible.
- Use persistent labels for form controls, programmatic names for icon-only controls, and specific error messages associated with their fields.
- Provide sufficient contrast for text, controls, focus, and status indicators; verify the proposed palette rather than relying on token names.
- Do not convey assignment, safety, deadline, or payment state by color alone.
- Keep task titles, reward, location, and materials readable at browser zoom and text resizing without hiding actions.
- Use meaningful alternative text for task imagery; mark atmospheric decoration as decorative. Respect reduced-motion preferences.
- **[ASSUMPTION]** Target at least 44 × 44 CSS pixels for touch controls; validate against the selected accessibility standard and platform behavior.

## Key Flows

Estos recorridos describen el producto objetivo; para la demo académica se aplican las exclusiones de Foundation: ubicación manual y asignación por orden de llegada, sin GPS ni selección entre postulaciones.

### UJ-1 — Doña Marta publica una changa y confirma su realización

1. Doña Marta llega al tablón con una necesidad concreta: quiere que barran su vereda.
2. Elige “Publicar changa” y describe la tarea, ofrece ARS 10.000, señala la zona y confirma que aporta escoba, bolsa y pala.
3. Elige asignación por orden de llegada porque no le importa quién realice el trabajo y revisa los datos antes de publicar.
4. Una persona toma la changa. La tarjeta de “Mis changas” muestra que hay una asignación; la dirección exacta sigue oculta a quienes no fueron asignados.
5. Cada parte acepta compartir su teléfono. Solo tras ambas aceptaciones se revelan los números a la otra parte; la ubicación exacta se comparte solo con la persona asignada.
6. La persona realizadora completa el trabajo y marca la changa como terminada.
7. **Clímax:** Doña Marta ve el plazo de 20 minutos y confirma que la tarea está completa. Si rechaza o no responde a tiempo, ve que se inicia una revisión administrativa; la app no cierra ni reasigna automáticamente.
8. Doña Marta declara que pagó fuera de la app y la persona realizadora declara por separado que recibió el pago.
9. Tras ambas declaraciones, la changa se cierra y cada parte puede enviar una calificación de 1 a 5. La aplicación no afirma haber verificado la transferencia.

### UJ-2 — Don Ramón encuentra y completa una changa cercana

1. Don Ramón abre “Ver tareas” y define una ubicación de búsqueda y un radio. Si usa GPS, lo hace mediante una acción explícita; también puede ingresar una ubicación manualmente.
2. Revisa el tablón y compara tareas por descripción, recompensa, zona aproximada y materiales antes de comprometerse.
3. Abre la changa de Doña Marta, comprueba que ella aporta escoba, bolsa y pala y la toma por orden de llegada.
4. Si la tarea ya fue asignada mientras Don Ramón la veía, la app explica que dejó de estar disponible y actualiza el resultado; no crea una segunda asignación.
5. Tras la asignación, ambas partes aceptan compartir sus teléfonos. Cuando existe consentimiento mutuo, Don Ramón puede coordinarse y consultar la ubicación exacta necesaria para realizar el trabajo.
6. Don Ramón realiza la tarea y la marca como terminada; el estado queda a la espera de la respuesta de Doña Marta dentro de los 20 minutos.
7. **Clímax:** Doña Marta confirma la realización; Don Ramón declara que recibió el pago externo y la changa se cierra cuando también se registra la declaración de pago de ella.
8. Ambos pueden calificarse después del cierre normal.

## Inspiration & Anti-patterns

- **Referencia aportada por Ema:** [Idea_Changa_quest.png](./imports/Idea_Changa_quest.png), una propuesta de “aventura moderna” con tablón de tareas, tarjetas y ambientación de gremio. Se conserva como inspiración, no como diseño aprobado al píxel.
- **Inspiración conceptual:** la fantasía de un gremio sirve para dar identidad al tablón; el modelo de interacción sigue siendo vecinal y práctico.
- **Evitar:** estética infantil, rangos/insignias que impliquen calidad o seguridad no verificada, lenguaje que obligue a aprender un juego, pantallas recargadas, urgencia ficticia, direcciones exactas públicas, chat no acordado y promesas de pago garantizado.
- Las pantallas ilustrativas nunca prevalecen sobre las reglas de privacidad, ciclo de vida, lenguaje claro y accesibilidad de estas dos spines.

## Responsive & Platform

Use a compact bottom navigation on mobile with four destinations: Ver tareas, Publicar, Mis changas, Perfil. On desktop the same destinations remain visible in a persistent side or top bar without changing the order. Keep the board and task surfaces as the main content. The experience must still be demoable on a laptop and usable with one hand on mobile. No native app, map integration, push notifications, or offline mode are assumed.

## Decisiones UX cerradas en esta pasada

Estas decisiones se toman como base del draft actual y quedan como línea guía hasta que el producto o la validación legal las reabra.

| Tema | Decisión adoptada | Nota |
|---|---|---|
| Modelo de cuenta | Una cuenta puede actuar como solicitante y realizadora; los roles dependen de cada changa. | Aplicable a la demo y al producto objetivo. |
| Búsqueda | Para la demo: ubicación manual, sin GPS ni mapas. Para el producto objetivo, GPS solo si se activa explícitamente. | No divulgar ubicación precisa antes de asignar. |
| Radio de búsqueda | Demo: 3 km por defecto y opciones 1, 3, 5 y 10 km. | La propuesta queda adoptada para la demo; los valores para el producto real siguen pendientes. |
| Consentimiento de contacto | Se solicita después de la asignación, con decisión independiente por parte de cada persona. | Se comparte solo si ambas partes aceptan; la negativa de una sigue siendo privada para la otra. |
| Moderación | El reporte va acompañado de motivo y evidencia opcional; el bloqueo es un control futuro y no reemplaza la revisión. | El bloqueo no borra ni resuelve una changa ya asignada. |
| Navegación principal | La home del tablón y “Mis changas” son los centros de navegación; la publicación es acción primaria, no un flujo separado que rompa el contexto. | Se mantiene la prioridad del solicitante sin perder la búsqueda del trabajador. |
| Lenguaje | La interfaz conserva un tono claro y cotidiano de barrio; la fantasía queda en la imagen y en la identidad visual. | No se usa lenguaje lúdico ni jerga de juego para decisiones de seguridad. |
| Búsqueda del producto real | Los radios y la precisión de distancia siguen abiertos en PRD OQ-3. | Esta apertura no modifica los radios ya acordados para la demo; proteger dirección exacta según FR-6. |
| Falta de consentimiento telefónico | Demo: la asignación permanece activa, no se comparte ningún teléfono y no hay cancelación automática ni canal alternativo. | Decisión de demo registrada; la regla para el producto real sigue abierta en PRD OQ-4. |
| Reportes y bloqueo | El alcance, las categorías y el efecto sobre trabajos ya asignados dependen de PRD OQ-5. | Producto y Operaciones; antes del lanzamiento real. |
| Revisión y apelación | Evidencias, plazos, autoridad de apelación y pagos pendientes dependen de PRD OQ-6. | Producto, Operaciones y asesoría legal; antes de habilitar casos reales. |
| Avisos de selección | El producto objetivo exige avisar a quienes no fueron seleccionados; esta modalidad queda fuera de la demo. | El canal y el momento siguen abiertos; no asumir email, push ni inbox. |
| Accesibilidad | WCAG 2.2 AA y el objetivo de tamaño táctil aquí indicados son metas provisionales, pendientes de aprobación. | UX y desarrollo; antes de aceptar el sistema visual. |
| Demo segura | El alcance acordado exige teléfonos y ubicaciones exactas ficticios y no operativos; no habrá trabajos ni pagos reales. | Decisión registrada en PRD y SPEC; verificarlo antes de presentar. |
