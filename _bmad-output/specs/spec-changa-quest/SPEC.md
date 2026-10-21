---
id: SPEC-changa-quest
companions:
  - ../../planning-artifacts/prds/prd-Changa-Quest-2026-11-01/prd.md
  - ../../planning-artifacts/prds/prd-Changa-Quest-2026-11-01/addendum.md
  - ../../planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md
  - ../../planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/DESIGN.md
  - ../../planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/EXPERIENCE.md
sources:
  - ../../planning-artifacts/briefs/brief-Changa-Quest-2026-10-16/brief.md
  - ../../planning-artifacts/briefs/brief-Changa-Quest-2026-10-16/addendum.md
---

# Changa Quest

## Why

Changa Quest hace visibles solicitudes vecinales pequeñas y de bajo riesgo para conectar a personas adultas que necesitan ayuda con otras dispuestas a realizar la tarea a cambio de una recompensa. La persona solicitante es el usuario primario porque su necesidad inicia cada changa; los resultados sociales esperados —resolver necesidades cotidianas del barrio y facilitar ingresos locales— tienen igual importancia, pero siguen siendo hipótesis y no impactos medidos.

## Capabilities

- **CAP-1**
  - **intent:** Una persona adulta elegible puede publicar una changa segura con descripción, recompensa en ARS, ubicación del trabajo, responsabilidad por materiales y modalidad de asignación.
  - **success:** Solo puede publicar una cuenta con correo verificado y declaración de tener 18 años o más. La publicación muestra la información necesaria para evaluar la tarea; se rechazan datos requeridos faltantes y tareas fuera del alcance.
- **CAP-2**
  - **intent:** Una persona realizadora puede encontrar changas disponibles cerca de una ubicación de búsqueda, mientras el producto protege la ubicación precisa.
  - **success:** Puede ingresar la ubicación manualmente o usar GPS tras una acción explícita; la ubicación manual solo se guarda si así lo elige, el GPS puntual no se conserva, los resultados respetan el radio seleccionado y las personas no asignadas no ven la dirección exacta.
- **CAP-3**
  - **intent:** Una persona solicitante puede asignar por orden de llegada o elegir entre postulaciones, y ambas partes pueden coordinarse mediante consentimiento mutuo para compartir el teléfono.
  - **success:** Se asigna como máximo una persona realizadora distinta de quien publicó; las postulaciones no seleccionadas reciben el resultado; los números de teléfono se revelan solo después de la asignación y con el consentimiento explícito de ambas partes.
- **CAP-4**
  - **intent:** Las partes pueden completar o cancelar una changa según su ciclo de vida y declarar por separado un pago externo.
  - **success:** La persona solicitante puede cancelar antes de la asignación; después, la cancelación requiere acuerdo mutuo salvo que una decisión administrativa libere la asignación. La inactividad o el desacuerdo nunca la liberan automáticamente. Después de que la persona realizadora marque la tarea como terminada, la persona solicitante tiene hasta 20 minutos para confirmar o rechazar; el rechazo o la falta de respuesta inicia una revisión administrativa. El cierre normal requiere ambas declaraciones de pago, que nunca se presentan como transacciones verificadas.
- **CAP-5**
  - **intent:** Ambas partes pueden calificarse después de una finalización normal o administrativa.
  - **success:** Cada parte puede enviar una calificación de 1 a 5, una sola vez y sin comentarios escritos; las personas registradas ven el promedio y la cantidad en el perfil, no la identidad de quien calificó.
- **CAP-6**
  - **intent:** En el producto real, las personas pueden reportar o bloquear y la administración puede revisar reportes y discrepancias sobre la realización.
  - **success:** La revisión registra motivos y evidencias, ofrece a ambas partes la oportunidad de responder y apelar, y aplica los resultados definidos en el PRD; estas funciones no son obligatorias para la demo académica.

## Constraints

- La implementación académica usa .NET y Entity Framework Core bajo la arquitectura N-Layers y las reglas de dependencias del companion de arquitectura.
- Los cambios funcionales siguen SDD orientado a TDD: los requisitos y criterios de aceptación documentados preceden a las pruebas; se aplica Red–Green–Refactor y la suite completa debe pasar.
- El producto es una experiencia web adaptable, mobile-first. La demo académica se ejecuta localmente y usa datos de ejemplo ficticios.
- La primera zona objetivo del producto real es la ciudad de Córdoba, Argentina; los barrios del piloto siguen sin definir.
- No cobrar a las personas usuarias en el lanzamiento inicial; la monetización futura no está decidida.
- No habilitar changas reales hasta resolver las condiciones legales, de seguridad, confianza, moderación y operación del PRD.
- Excluir tareas profesionales, que requieran licencia, de alto riesgo, en altura, con maquinaria peligrosa o materiales peligrosos.
- No ofrecer chat en la aplicación ni procesar, custodiar, verificar o garantizar pagos. No conservar GPS puntual para seguimiento.
- Mantener oculta la dirección exacta hasta la asignación. Compartir teléfonos solo después de asignar y con consentimiento explícito mutuo.

## Academic Demo Scope

La demo académica incluye registro y autenticación local con verificación de correo y declaración de tener al menos 18 años. Una cuenta verificada puede actuar como solicitante y realizadora.

El recorrido normal incluye publicar una changa, encontrarla mediante ubicación manual por barrio/zona y radio configurable (3 km por defecto; opciones 1, 3, 5 y 10 km), tomarla por orden de llegada con asignación atómica, compartir teléfonos solo con consentimiento mutuo, marcar la finalización, confirmarla, registrar por separado si se pagó/recibió la recompensa publicada fuera de la aplicación y permitir calificaciones mutuas. Las declaraciones no incluyen un monto numérico. Si una parte no acepta compartir su teléfono o todavía no responde, la asignación permanece activa y no se revelan teléfonos ni se ofrece un canal alternativo. Las distancias se estiman en línea recta con coordenadas de referencia ficticias; no se integra GPS ni mapas.

Solo se incluye cancelación antes de la asignación. El rechazo o la falta de respuesta a la finalización deja la changa pendiente de revisión, sin resolución administrativa, cierre ni reasignación automática. No se incluyen selección entre postulaciones, chat, procesamiento de pagos, reportes, bloqueos ni revisión administrativa. Los teléfonos y ubicaciones exactas son ficticios; no se realizan trabajos, contactos ni pagos reales. El producto objetivo conserva las capacidades completas de PRD, incluidas las que la demo omite.

## Non-goals

- Una aplicación móvil nativa, un mercado general de empleo o una plataforma de servicios profesionales.
- Procesamiento, custodia, verificación o garantía de pagos; chat dentro de la aplicación.
- Tratar la demo académica como autorización para transacciones reales ni como evidencia de demanda, seguridad o impacto social validado.
- Operar un lanzamiento real antes de cumplir las condiciones legales y operativas de go/no-go del PRD.

## Success signal

La demo académica local permite mostrar el flujo normal descrito en Academic Demo Scope, desde el registro verificado hasta las calificaciones, usando asignación por orden de llegada. Para el producto se registran personas activas por rol, changas completadas y recompensas con declaraciones de pago mutuas, junto con señales de seguridad/moderación y casos sin resolver; períodos y objetivos siguen abiertos. La demo presenta el posible impacto vecinal y de ingresos como hipótesis no validada, no como resultado medido.

## Assumptions

- .NET 10, ASP.NET Core 10 y EF Core 10 son la base aprobada por la cátedra y definida en el architecture spine.
- Los tokens visuales y los objetivos de accesibilidad de UX son provisionales porque sus documentos siguen en borrador.

## Open Questions

- Para el producto real, OQ-3 del PRD mantiene abiertos los radios y la precisión de distancia. Para la demo académica se acuerdan 3 km por defecto y opciones de 1, 3, 5 y 10 km, sin GPS ni mapas.
- Para la demo, una asignación permanece activa si falta el consentimiento telefónico mutuo y los teléfonos siguen ocultos, sin cancelación automática ni canal alternativo; la consecuencia para el producto real sigue abierta.
- ¿Qué controles de confianza/identidad, categorías de tareas seguras, comportamiento de reportes/bloqueos, evidencias, autoridad de revisión, plazos de respuesta/apelación y sanciones se necesitan antes del lanzamiento real?
- ¿Cómo se resuelven casos pendientes del ciclo de vida, como ausencia de postulantes, inasistencia, declaraciones de pago faltantes y desacuerdo de cancelación?
- ¿Qué canal y momento se usarán para avisar a las personas no seleccionadas?
- Confirmar en UX el español de Argentina y los criterios de accesibilidad propuestos antes de aprobar la experiencia.
- ¿Qué niveles de servicio, proceso de soporte, retención de datos y controles de privacidad/seguridad se necesitan antes de un lanzamiento real?
