# Epic 2 Context: Completar y cerrar una changa

<!-- Compilado desde los artefactos de planificación. Se puede editar libremente; regenerar si cambian los documentos fuente. -->

## Goal

Permitir que las dos personas asignadas registren el resultado de una changa: la realizadora la marca terminada, la solicitante confirma o rechaza, ambas declaran por separado el pago externo y, tras el cierre normal, se califican. La demo muestra el recorrido normal y deja constancia sin procesar ni verificar pagos; los desacuerdos y la falta de respuesta quedan pendientes de revisión, sin resolverse automáticamente.

## Stories

- Story 2.1: Confirmar la realización dentro del plazo
- Story 2.2: Cancelar una changa antes de su asignación
- Story 2.3: Declarar por separado el pago externo
- Story 2.4: Calificar a la otra parte tras el cierre
- Story 2.5: Cancelar una changa asignada por acuerdo mutuo (fuera de la demo)

## Requirements & Constraints

- La persona realizadora asignada marca el trabajo como terminado. Eso inicia un plazo de 20 minutos para que la solicitante confirme o rechace y no cierra la changa.
- El rechazo o el vencimiento llevan a **pendiente de revisión**: sin cierre ni reasignación automática. Después del vencimiento, o mientras la revisión siga pendiente, el flujo normal no permite confirmar ni rechazar.
- En la demo solo se cancela antes de la asignación. Una changa asignada no se cancela ni se libera por inactividad o desacuerdo.
- Tras confirmar, cada parte declara por separado, fuera de la aplicación, si pagó o recibió la recompensa publicada. No se pide monto ni datos de transacción. El cierre normal exige ambas declaraciones y recién entonces se habilitan las calificaciones.
- Cada parte envía una calificación entera de 1 a 5, sin texto, solo tras el cierre normal. No se habilita para changas canceladas ni pendientes de revisión. Los perfiles muestran promedio y cantidad, no quién calificó.
- La plataforma no procesa, custodia, verifica ni garantiza pagos. Los datos son ficticios.
- La revisión administrativa pertenece al producto real y está fuera de la demo. No inventar evidencia, autoridad, plazos ni resoluciones; solo mostrar el estado pendiente.

## Technical Decisions

- Solo BusinessLogic decide reglas y transiciones. `ChangaManager` es dueño del ciclo de vida de la changa y su asignación. Presentation valida el contrato HTTP y delega. DataAccess persiste lo que se le pide.
- Transiciones: asignada → pendiente de respuesta → confirmada, o → pendiente de revisión por rechazo o vencimiento. La segunda declaración de pago habilita el cierre normal.
- Las transiciones incompatibles deben protegerse de forma atómica: como máximo una queda persistida (patrón de la toma de la Story 1.4).
- El contrato `specs/api/openapi-v1.yaml` se actualiza antes que API y cliente.

## UX & Interaction Patterns

- En «Mis changas» los estados llevan etiqueta clara y texto neutral. A la solicitante se le muestran confirmar y rechazar con el plazo restante. El vencimiento y la revisión se explican sin culpar a nadie.
- Al rechazar o vencer, se explica que hace falta revisión y que la changa sigue sin cierre ni reasignación. No se simula una decisión administrativa.
- Cada declaración externa se presenta como afirmación de quien la registra, nunca como «pago confirmado» por la plataforma.
- Los estados no dependen solo del color y los controles se operan con teclado y foco visible.

## Cross-Story Dependencies

- Requiere una changa publicada y asignada por el épico 1 (Stories 1.4 y 1.5).
- La confirmación de la 2.1 habilita las declaraciones de la 2.3; ambas habilitan el cierre y este las calificaciones de la 2.4. El rechazo o el vencimiento detienen esa ruta.
- La cancelación de la 2.2 compite con la toma de la 1.4 y debe respetar su integridad.
