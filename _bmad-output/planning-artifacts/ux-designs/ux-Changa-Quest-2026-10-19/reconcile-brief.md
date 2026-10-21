# Reconciliación breve: brief vs. UX

## Fuente de entrada
- `brief-Changa-Quest-2026-10-16` (brief.md + addendum.md)

## Hallazgos clave

1. **El usuario primario y la intención social quedan diluidos en la IA UX.**
   - **Evidencia del brief:** `brief.md` establece que “la persona que publica la solicitud es el cliente primario”, y que el impacto esperado es “ayudar a resolver necesidades cotidianas de vecinos y generar ingresos locales”, con el solicitante como eje del flujo. `addendum.md` reitera que “el solicitante sigue siendo el usuario primario porque su necesidad inicia el flujo.”
   - **Evidencia de UX:** `EXPERIENCE.md` presenta “requester” y “worker” como actividades de una misma cuenta y define el tablero y “Mis changas” como destinos principales; no vuelve a priorizar al solicitante ni a la necesidad cotidiana que inicia el flujo.
   - **Riesgo de distorsión:** el producto comunica una experiencia más simétrica de mercado y menos una herramienta de resolución de necesidades vecinales impulsada por la solicitud.

2. **Los límites de seguridad y elegibilidad de tareas no pasan a reglas operativas de UX.**
   - **Evidencia del brief:** `brief.md` excluye tareas que requieren licencia, altura, maquinaria peligrosa o materiales peligrosos, y deja abierta la moderación de publicaciones no válidas (“¿Cómo se moderan las publicaciones que no cumplen los límites del MVP?”).
   - **Evidencia de UX:** `DESIGN.md` habla de “safety before atmosphere” y `EXPERIENCE.md` incluye decisiones de seguridad para datos sensibles, pero no convierte esa exclusión en una regla visible para publicar, filtrar o rechazar tareas.
   - **Riesgo de distorsión:** la UX sugiere un tablón abierto con tono “seguro”, pero no refleja claramente que el contenido debe estar acotado a tareas simples y de bajo riesgo.

3. **La restricción académica .NET/EF/N-Layers queda ausente del diseño UX.**
   - **Evidencia del brief y addendum:** `brief.md` y `addendum.md` describen explícitamente la obligación académica de “.NET y Entity Framework con arquitectura N-Layers”, con capas de Presentación, Lógica de Negocio, Acceso a Datos, Entidades y Core.
   - **Evidencia de UX:** `DESIGN.md` y `EXPERIENCE.md` no mencionan esta restricción ni la convierten en una guía de diseño o un marco de decisiones de producto; incluso `EXPERIENCE.md` dice que la UI system “none selected”.
   - **Riesgo de distorsión:** la solución UX se presenta como un producto abstracto, no como un MVP académico con requisitos de arquitectura y demostración local.

4. **La revisión administrativa se incorpora como superficie real de UX, aunque el brief la descarta para el MVP.**
   - **Evidencia del brief y addendum:** `brief.md` y `addendum.md` indican que “el MVP no incluye mediación ni reportes administrativos” y que “reportes y revisión administrativa ... quedan como posibilidad futura.”
   - **Evidencia de UX:** `EXPERIENCE.md` incluye un “Administrative review | Restricted staff access” con “Review record” y trae un flujo de evidencia/decisión/appeal como si fuera un destino funcional real.
   - **Riesgo de distorsión:** la UX altera el alcance del MVP y encarna una visión de producto más completa que la que el brief y el addendum permiten.

## Conclusión
La mayor diferencia no es estética, sino de intención y alcance: el brief define un MVP local, orientado a resolver tareas vecinales con un usuario primario concreto y límites claros; los spines UX preservan la estética y algunos flujos de seguridad, pero diluyen el foco del solicitante, omiten la restricción académica y elevan funciones de administración que el MVP aún no contempla.

## Citas de referencia
- `brief.md`: “la persona que publica la solicitud es el cliente primario” y “la solución deberá poder ejecutarse en un servidor local y demostrarse en clase.”
- `brief.md`: “Se excluyen trabajos que requieran licencia o impliquen altura, maquinaria peligrosa o materiales peligrosos.”
- `addendum.md`: “.NET y Entity Framework con arquitectura N-Layers” y “el MVP no incluye mediación ni reportes administrativos.”
- `EXPERIENCE.md`: “Administrative review | Restricted staff access” y “Review record.”
- `DESIGN.md`: “safety before atmosphere” y la referencia a la “guild” como marco visual, no como sistema de rol-play.
