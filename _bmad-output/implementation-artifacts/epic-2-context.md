# Epic 2 Context: Completar y cerrar una changa

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 2 permite que las partes de una changa asignada registren su resultado sin que la plataforma procese ni verifique pagos. El ciclo debe distinguir realización, confirmación, cancelación, declaraciones de pago y cierre, preservando la asignación cuando existe desacuerdo o falta de respuesta y dejando las decisiones administrativas fuera de los flujos normales.

## Stories

- Story 2.1: Confirmar la realización dentro del plazo
- Story 2.2: Cancelar una changa antes de su asignación
- Story 2.3: Declarar por separado el pago externo
- Story 2.4: Calificar a la otra parte tras el cierre
- Story 2.5: Cancelar una changa asignada por acuerdo mutuo

## Requirements & Constraints

- Una changa asignada no puede liberarse por abandono unilateral, inactividad o desacuerdo.
- La cancelación antes de la asignación es distinta de la cancelación posterior, que requiere acuerdo mutuo.
- Una solicitud de cancelación debe conservar la asignación activa hasta una respuesta explícita de la otra parte.
- El rechazo de una solicitud no cambia el estado de la changa; no se define vencimiento ni resolución automática.
- La aceptación mutua debe cancelar la changa y liberar la asignación como una sola transición válida.
- Las personas ajenas a la asignación no pueden operar ni consultar información privada del caso.
- La aplicación no procesa, custodia, verifica ni garantiza pagos.
- Los estados pendientes y los desacuerdos que requieren intervención administrativa permanecen fuera del flujo normal y dependen de OQ-7.
- La experiencia debe funcionar en móvil y laptop, con confirmaciones deliberadas, foco visible, etiquetas persistentes y estados explicados con texto.

## Technical Decisions

- Las reglas y transiciones del ciclo de vida pertenecen a `BusinessLogic`; `ChangaManager` mantiene la autoridad sobre la changa y su asignación.
- `Presentation` traduce casos de uso a endpoints REST y Problem Details; `DataAccess` ejecuta consultas y transacciones sin decidir reglas.
- La API usa `/api/v1` y el contrato OpenAPI se actualiza antes de API y cliente.
- Las transiciones incompatibles deben comprobar el estado persistido y confirmarse atómicamente bajo concurrencia.
- La asignación es única por changa. Una aceptación de cancelación no puede dejar la changa cancelada con una asignación activa ni liberar una asignación cuya solicitud no fue aceptada por ambas partes.
- Las pruebas deben cubrir unidad, persistencia, aceptación, contrato y cliente cuando corresponda, usando el runner Microsoft.Testing.Platform.

## UX & Interaction Patterns

- Separar visualmente la acción de proponer cancelación de las acciones de aceptar o rechazar.
- Requerir confirmación explícita antes de proponer, aceptar o rechazar.
- Mostrar mientras espera: “solicitud pendiente”; no presentar la changa como cancelada.
- Mostrar rechazo o ausencia de respuesta sin culpar ni insinuar una liberación automática.
- Tras el acuerdo, mostrar cancelación confirmada y que la asignación fue liberada.
- Las solicitudes fallidas deben conservar datos seguros y ofrecer reintento sin afirmar éxito.

## Cross-Story Dependencies

- Story 2.5 depende del modelo de asignación y coordinación establecido por Stories 1.4 y 1.5.
- Comparte el ciclo de vida con Stories 2.1–2.4; no debe habilitar calificaciones, pagos o cierre normal por una cancelación.
- Las decisiones administrativas de Epic 3 pueden liberar o cerrar changas en el futuro, pero no forman parte de esta historia.
