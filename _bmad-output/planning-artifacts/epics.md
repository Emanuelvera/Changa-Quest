---
status: final
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Changa-Quest-2026-11-01/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Changa-Quest-2026-11-01/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-Changa-Quest-2026-10-19/EXPERIENCE.md
  - _bmad-output/specs/spec-changa-quest/SPEC.md
---

# Changa Quest — Descomposición de alcance

## Overview

Este documento organiza los requisitos de Changa Quest para su posterior descomposición en épicas e historias implementables. La SPEC consolida la intención; PRD, arquitectura y UX permanecen como contratos fuente complementarios. Las preguntas abiertas y los supuestos se preservan y no se convierten en decisiones por esta extracción.

## Academic Demo Scope

El alcance de la demo académica está acotado al flujo normal de punta a punta descrito en el PRD: registro/autenticación local con verificación de correo, publicación, búsqueda manual por barrio/zona y radio, asignación por orden de llegada, coordinación con consentimiento mutuo de teléfono, finalización, confirmación, declaraciones de pago externo y calificaciones. Una cuenta puede actuar como solicitante y realizadora.

La demo no incluye asignación por selección, GPS/mapas, chat, procesamiento de pagos ni Epic 3 (reportes, bloqueos y resolución administrativa). Si una parte rechaza o no responde al consentimiento telefónico, la asignación permanece activa, los teléfonos siguen ocultos y no hay cancelación automática ni canal alternativo. Rechazo o falta de respuesta tras la finalización deja la changa pendiente de revisión, sin cierre ni reasignación automática; la resolución administrativa queda fuera de la demo. Solo se incluye cancelación antes de la asignación. Los datos de contacto y ubicaciones exactas son ficticios y no se realizan trabajos ni pagos reales.

La interfaz es mobile-first y también usable desde laptop, con español claro de Argentina, etiquetas persistentes, errores asociados a sus campos, foco visible y navegación por teclado; los estados no dependen solo del color. La dirección visual de gremio adulto es orientativa: los valores exactos de la paleta y el objetivo WCAG 2.2 AA siguen siendo provisionales.

El PRD sigue siendo la fuente canónica de las reglas; esta sección fija el límite de alcance para descomponer e implementar la demo, sin quitar las capacidades correspondientes del producto objetivo.

## Requirements Inventory

### Functional Requirements

FR-1: Una persona puede crear una cuenta verificando su correo electrónico y declarando tener al menos 18 años. Solo las cuentas verificadas y que confirmen esa edad pueden publicar o tomar changas; controles adicionales de identidad para un lanzamiento real siguen sujetos a revisión.

FR-2: Una persona solicitante puede publicar una changa segura con descripción, recompensa en ARS, ubicación del trabajo y modalidad de asignación. Solo categorías permitidas pueden aparecer como disponibles; las categorías y el tratamiento de publicaciones fuera de alcance requieren definición antes del lanzamiento real.

FR-3: Al publicar, la persona solicitante debe indicar si hacen falta herramientas o materiales y quién los aporta cuando corresponda, incluyendo explícitamente que no se necesitan materiales. Esta información debe estar disponible antes de tomar o postularse.

FR-4: Al publicar, la persona solicitante puede elegir asignación por orden de llegada o selección entre postulaciones. En modo selección, las postulaciones no elegidas se cierran como no seleccionadas y sus autores reciben un aviso; el canal de aviso está pendiente.

FR-5: Una persona realizadora autenticada puede definir ubicación de búsqueda y radio; los resultados deben quedar dentro del radio elegido. La ubicación puede ingresarse manualmente o determinarse por GPS bajo acción explícita; el GPS puntual no se conserva ni se usa para seguimiento y una ubicación manual solo se guarda si se elige.

FR-6: Antes de asignar, la aplicación muestra barrio/zona y distancia aproximada, sin dirección exacta ni pin preciso. Solo la persona realizadora asignada puede consultar la ubicación exacta necesaria para el trabajo.

FR-7: En modalidad por orden de llegada, una changa disponible se asigna a una sola persona realizadora distinta de quien la publicó. Intentos concurrentes o reintentos no pueden crear asignaciones duplicadas y una changa asignada deja de estar disponible.

FR-8: En modalidad de selección, la persona solicitante puede revisar postulaciones y asignar una. Solo la elegida se convierte en asignación; las restantes se cierran como no seleccionadas y reciben notificación.

FR-9: Después de la asignación, cada parte puede aceptar compartir su número de teléfono. La aplicación revela ambos teléfonos únicamente si ambas partes dieron consentimiento explícito; no incluye chat. La consecuencia de una negativa sigue abierta.

FR-10: La persona realizadora asignada puede marcar la changa como terminada. La persona solicitante tiene hasta 20 minutos para confirmar o rechazar; rechazo o ausencia de respuesta inicia revisión administrativa. La changa no se cierra ni reasigna automáticamente.

FR-11: Después de confirmar la realización, ambas partes pueden declarar por separado el pago o la recepción del importe acordado fuera de la aplicación. Una sola declaración no permite cierre normal y la plataforma no procesa, custodia, verifica ni garantiza el pago. Un cierre administrativo como completado es una excepción.

FR-12: Después del cierre normal o de una decisión administrativa de finalización, cada parte puede enviar una calificación de 1 a 5 por changa, una sola vez y sin texto. Los perfiles muestran promedio y cantidad a personas registradas, sin revelar quién otorgó cada nota.

FR-13: La persona solicitante puede cancelar antes de la asignación. Después, requiere acuerdo mutuo, salvo liberación de asignación por revisión administrativa que determine que el trabajo no se completó. No se permite abandono unilateral ni liberación automática por inactividad o desacuerdo; otros casos pendientes siguen abiertos.

FR-14: Una persona usuaria puede reportar contenido/interacción y bloquear a otra. Los reportes quedan disponibles para revisión administrativa; categorías, alcance del bloqueo y efecto sobre changas asignadas siguen abiertos.

FR-15: Una persona administradora puede revisar reportes y discrepancias sobre la realización, registrar motivos y evidencias, dar oportunidad de respuesta a ambas partes y permitir apelación. Si determina que el trabajo no se completó, libera la asignación y reabre la changa sin asignar; si determina que se completó, puede cerrarla aunque el pago no esté confirmado y habilitar calificaciones. Evidencias, plazos, autoridad de apelación, sanciones y pagos pendientes siguen abiertos.

### NonFunctional Requirements

NFR-1: No conservar ubicación GPS puntual ni divulgar dirección exacta antes de asignar; la ubicación manual solo puede guardarse como preferencia cuando la persona lo elige.

NFR-2: Proteger números telefónicos y revelarlos solo después de asignar y obtener consentimiento explícito de ambas partes.

NFR-3: Proteger cuentas, datos de contacto, ubicación exacta e información de reportes de acceso no autorizado. Controles concretos, retención y auditoría requieren definición.

NFR-4: Mantener integridad de asignación bajo solicitudes simultáneas; como máximo una persona realizadora puede quedar asignada por changa.

NFR-5: Excluir tareas de alto riesgo o que requieran licencia profesional. Definir categorías, moderación y procedimiento antes de operar con usuarios reales.

NFR-6: No habilitar lanzamiento real hasta que asesoría legal argentina revise clasificación y obligaciones laborales, impositivas, de consumo, privacidad y seguridad aplicables.

NFR-7: Establecer objetivos medibles de disponibilidad, tiempos de respuesta, accesibilidad, soporte y retención antes del lanzamiento real; no hay umbrales definidos.

NFR-8: La experiencia es web adaptable, mobile-first, utilizable también desde laptop; aplicación móvil nativa fuera del alcance inicial.

NFR-9: Español de Argentina es el idioma propuesto para Córdoba, sujeto a confirmación de UX.

### Additional Requirements

- Implementar en .NET y Entity Framework Core, siguiendo la guía académica de arquitectura N-Layers.
- Respetar dependencias: Backend API → Presentation → BusinessLogic → DataAccess → Entities, además de BusinessLogic → Entities; Core es soporte técnico compartido, no una quinta capa.
- Presentation adapta HTTP y delega; BusinessLogic es dueña de reglas y transiciones; un ChangaManager es autoridad de mutación del ciclo de vida; DataAccess mantiene Stores, EF Core, DbContext, consultas y persistencia.
- El host API configura servicios, middleware y dependencias mediante Presentation; no aloja reglas de negocio.
- Aplicar SDD orientado a TDD: cada cambio parte de especificación versionada y criterios vinculados a FR; pruebas antes del código, ciclo Red–Green–Refactor y suite completa aprobada.
- Validar automáticamente las referencias entre capas; el mecanismo se decide al inicializar la solución.
- Persistir asignaciones y transiciones incompatibles como operaciones atómicas; probar concurrencia, reintentos y transiciones que compiten contra el proveedor relacional seleccionado.
- Publicar la API bajo `/api/v1` con un documento OpenAPI versionado como contrato canónico; pruebas de contrato validan API y cliente Blazor contra el mismo esquema.
- Una repetición de toma por la misma persona devuelve la asignación existente; una persona distinta recibe HTTP 409 con el código estable `changa_already_assigned`. La selección confirma asignación y resultados de todas las postulaciones en una única transacción; los avisos se emiten solo después del commit.
- La proyección pública/autorizada de datos se decide en BusinessLogic; DataAccess consulta lo solicitado y Presentation serializa solo datos permitidos. Volver a comprobar autorización/consentimiento en cada consulta sensible.
- Errores HTTP uniformes con `application/problem+json`/Problem Details, diferenciando errores de validación/regla y técnicos sin filtrar información interna.
- La demo académica debe ejecutarse en servidor local con datos de ejemplo ficticios; no habilita tareas, ubicaciones exactas, contactos ni pagos reales. Reportes, bloqueo y revisión administrativa pueden omitirse de la demo.
- La cátedra confirmó .NET 10 / ASP.NET Core 10 / EF Core 10; la demo usa EF Core SQL Server con LocalDB/Express, Blazor WebAssembly y xUnit.net v3 según el architecture spine.
- CI, alojamiento/entrega de assets, secretos, backups, monitoreo y operación siguen diferidos; el proveedor de base de datos para el producto real también queda abierto.

### UX Design Requirements

UX-DR1: Aplicar la dirección visual de tablón de gremio adulto y acogedor como ambientación, no como juego de rol; priorizar datos reales, legibilidad y acciones cotidianas, sin copiar franquicias ni usar insignias que impliquen seguridad no verificada.

UX-DR2: Mantener la paleta semántica, tipografía, espaciado, radios y tokens de componentes de DESIGN.md como propuesta provisional; validar contraste antes de implementación y permitir revisión del tema/tokens.

UX-DR3: Diseñar mobile-first; en móvil usar navegación inferior compacta con Ver tareas, Publicar, Mis changas y Perfil; en escritorio mantener los mismos destinos y orden en barra persistente.

UX-DR4: En el tablón, destacar título/descripción, recompensa ARS, zona aproximada, materiales y modalidad de asignación antes del compromiso; nunca incluir dirección exacta en tarjetas públicas.

UX-DR5: Para la demo académica, usar ubicación manual únicamente, con radio predeterminado de 3 km y opciones de 1, 3, 5 y 10 km; no integrar GPS ni mapas. Para el producto real, ofrecer GPS solo después de una acción explícita, no conservar GPS puntual ni solicitar permiso repetidamente, y mantener abiertos el radio y la precisión de distancia según PRD OQ-3.

UX-DR6: El formulario de publicación recoge descripción, recompensa, ubicación del trabajo, materiales/herramientas y modalidad de asignación; materiales deben tener respuesta explícita y campos persistentes con validación asociada.

UX-DR7: Separar apertura de detalle de la acción de tomar/postularse; explicar cuándo la changa ya fue asignada y no mostrar intento obsoleto como éxito.

UX-DR8: Solicitar consentimiento de teléfono después de asignar y por separado para cada parte; conservar privados ambos números hasta consentimiento mutuo y no inferir un canal de chat alternativo.

UX-DR9: Mostrar estados de “Mis changas” de forma inequívoca; al marcar finalización, mostrar al solicitante las opciones de confirmar/rechazar y el límite de 20 minutos; rechazo/expiración inicia revisión, sin cierre ni reasignación automática.

UX-DR10: Representar declaraciones de pago como afirmaciones individuales de que la recompensa publicada fue pagada/recibida, indicar cuál falta y diferenciar la recompensa publicada de una declaración; no solicitar un monto numérico ni comunicar pago procesado o verificado.

UX-DR11: Habilitar calificación 1–5 sin texto tras cierre permitido; perfil presenta promedio y cantidad, nunca identidad del evaluador.

UX-DR12: Reporte requiere motivo y evidencia opcional; bloqueo exige confirmación y no debe prometer resolver o cancelar una changa ya asignada.

UX-DR13: La superficie administrativa es restringida; muestra revisión, evidencia privada, respuestas de ambas partes, decisión y apelación sin exponer evidencias a otras personas usuarias. No asumir plazos no definidos.

UX-DR14: Implementar estados de carga, vacío, ubicación no disponible, changa tomada, postulación no seleccionada, consentimiento incompleto, revisión, error y actividad vacía; conservar datos de formulario de forma segura y ofrecer reintento sin simular éxito.

UX-DR15: Aplicar lenguaje claro y respetuoso en español de Argentina; mantener fantasía solo en la identidad visual. No usar urgencia ficticia ni promesas de pago/seguridad.

UX-DR16: Acciones utilizables con teclado y foco visible; campos con etiquetas persistentes, nombres accesibles y errores asociados; estados no dependen solo del color; contraste suficiente y validado; contenido usable con zoom/redimensión; texto alternativo significativo, decoración marcada como tal y respeto por movimiento reducido. WCAG 2.2 AA y objetivo táctil de 44×44 CSS px son supuestos UX por confirmar.

## FR Coverage Map

FR-1: Epic 1 — elegibilidad y cuentas para solicitar o realizar changas.
FR-2: Epic 1 — publicar una solicitud segura y remunerada.
FR-3: Epic 1 — informar herramientas y materiales disponibles.
FR-4: Epic 1 — elegir asignación por orden de llegada o selección.
FR-5: Epic 1 — descubrir changas por ubicación y radio.
FR-6: Epic 1 — proteger la dirección exacta antes de asignar.
FR-7: Epic 1 — asignar por orden de llegada sin duplicados.
FR-8: Epic 1 — elegir entre postulaciones y cerrar las restantes.
FR-9: Epic 1 — compartir teléfono solo con consentimiento mutuo.
FR-10: Epic 2 — marcar y confirmar la realización dentro del plazo.
FR-11: Epic 2 — registrar declaraciones independientes de pago externo.
FR-12: Epic 2 — cerrar changas elegibles y habilitar calificaciones.
FR-13: Epic 2 — cancelar según estado sin liberación automática.
FR-14: Epic 3 — reportar contenido/interacciones y bloquear.
FR-15: Epic 3 — revisar reportes y discrepancias de realización.

## Agrupación de trabajo

### Epic 1: Publicar, encontrar y coordinar changas

Como persona adulta solicitante, quiero publicar una changa y asignarla, y como persona realizadora quiero encontrarla y coordinarme con la persona solicitante, para resolver una necesidad vecinal con información clara y protegida.
**FRs covered:** FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-9.
**Valor independiente:** permite registrar personas elegibles, publicar y descubrir solicitudes, y completar la asignación/contacto conforme a privacidad. Sienta el flujo que la épica de cierre extiende.

### Epic 2: Completar y cerrar una changa

Como persona solicitante y realizadora asignadas, queremos registrar la finalización, resolver cancelaciones permitidas, declarar por separado el pago externo y calificarnos, para dejar constancia del resultado sin que la aplicación procese ni verifique el pago.
**FRs covered:** FR-10, FR-11, FR-12, FR-13.
**Dependencia natural:** utiliza una changa y asignación creadas en Epic 1. El resultado agrega el cierre de extremo a extremo.

### Epic 3: Reportar y revisar situaciones problemáticas

Como persona usuaria, quiero reportar una situación y, como persona administradora, quiero revisar reportes o discrepancias con participación de ambas partes, para gestionar incidentes del producto real de manera trazable.
**FRs covered:** FR-14, FR-15.
**Frontera de alcance:** pertenece al producto real y puede excluirse de la demo académica. Depende de las cuentas, tareas y asignaciones de las épicas anteriores; no habilita por sí sola una operación real sin resolver las condiciones de lanzamiento.

## Epic 1: Publicar, encontrar y coordinar changas

Como persona adulta solicitante, quiero publicar una changa y asignarla, y como persona realizadora quiero encontrarla y coordinarme con la persona solicitante, para resolver una necesidad vecinal con información clara y protegida.

### Story 1.1: Crear y verificar una cuenta adulta

As a persona adulta,
I want registrarme con email y contraseña, verificar mi correo e iniciar sesión,
So that pueda publicar o tomar changas con una cuenta elegible.

**References:** FR-1, NFR-3, UX-DR1, UX-DR2, UX-DR3, UX-DR15, UX-DR16, Academic Demo Scope.

**Acceptance Criteria:**

**Given** que una persona registra un email válido, contraseña de al menos 12 caracteres y declara tener 18 años o más  
**When** envía el formulario de registro  
**Then** se crea una cuenta no verificada y se genera un mensaje de verificación en el entorno local de prueba.

**Given** que una cuenta todavía no verificó su email  
**When** intenta publicar o tomar una changa  
**Then** la operación se rechaza y no cambia el estado de ninguna changa.

**Given** que la persona confirma el mecanismo de verificación recibido  
**When** la verificación es válida  
**Then** la cuenta queda verificada y puede iniciar sesión.

**Given** que el email no está verificado  
**When** la persona intenta iniciar sesión  
**Then** no obtiene acceso a las funciones autenticadas y recibe una indicación clara de que debe verificarlo.

**Given** que la persona declara tener menos de 18 años o no confirma la mayoría de edad  
**When** intenta registrarse  
**Then** no se crea una cuenta habilitada para publicar ni tomar changas.

**Given** que una cuenta verificada inicia sesión  
**When** publica o toma una changa  
**Then** puede actuar en el rol correspondiente y la cuenta no queda atada a un único rol.

**Given** que el email o la contraseña son inválidos, o el registro no puede completarse  
**When** se envía el formulario  
**Then** se informa el error y no se presenta la operación como exitosa.

**Given** que la contraseña tiene menos de 12 caracteres  
**When** se envía el formulario de registro  
**Then** se rechaza el registro con un error claro y no se crea una cuenta habilitada.

**Given** que la persona usa el registro o inicio de sesión desde un celular o laptop  
**When** navega con teclado o amplía el contenido  
**Then** los controles conservan etiquetas accesibles, el foco permanece visible, los errores se asocian a sus campos y el contenido sigue siendo utilizable sin depender solo del color.

**Given** que se presenta la interfaz de acceso  
**When** se muestran instrucciones y elementos visuales  
**Then** usa español claro y una ambientación adulta y sobria de tablón de gremio, sin copiar franquicias ni prometer seguridad no verificada; los valores exactos de la paleta permanecen provisionales.

**Given** que una persona verificada inicia sesión correctamente  
**When** entra a la aplicación  
**Then** encuentra “Ver tareas”, “Publicar changa”, “Mis changas” y “Perfil”; la navegación es inferior y compacta en celular y conserva ese orden en una barra persistente en laptop.

### Story 1.2: Publicar una changa con información suficiente

As a persona solicitante verificada,
I want publicar una descripción, recompensa, ubicación y datos de materiales,
So that una persona realizadora pueda evaluar la changa antes de tomarla.

**References:** FR-2, FR-3, FR-6, UX-DR1, UX-DR2, UX-DR6, UX-DR14, UX-DR15, UX-DR16, Academic Demo Scope.

**Acceptance Criteria:**

**Given** que una persona adulta inició sesión con email verificado  
**When** publica una descripción, recompensa positiva en ARS, ubicación del trabajo y respuesta explícita sobre materiales  
**Then** la changa se guarda como disponible, con asignación por orden de llegada, y queda visible en el tablón.

**Given** que faltan la descripción, recompensa, ubicación o la respuesta sobre materiales  
**When** intenta publicar  
**Then** el sistema señala los campos faltantes y no crea una changa disponible.

**Given** que la persona indica que hacen falta materiales  
**When** completa la publicación  
**Then** debe indicar quién los aporta y esa información queda visible antes de que alguien tome la changa.

**Given** que la persona indica que no hacen falta materiales  
**When** completa la publicación  
**Then** esa respuesta explícita se conserva y se muestra a quien consulta la changa.

**Given** que la changa todavía no está asignada  
**When** alguien ve la tarjeta o el detalle público  
**Then** ve descripción, recompensa, zona aproximada y materiales, pero no la dirección exacta.

**Given** que se publican ejemplos para la demostración  
**When** se cargan los datos de muestra  
**Then** son ficticios y curados, y no representan trabajos reales ni generan contactos o pagos reales.

**Nota de alcance:** la demo permite descripción libre y solo usa datos de ejemplo curados; el catálogo y las validaciones del producto real se cubren en la Story 1.8 y quedan bloqueados por PRD OQ-5.

**Given** que una persona usa el formulario de publicación desde celular o laptop  
**When** completa o corrige sus datos  
**Then** los campos tienen etiquetas persistentes, los errores se asocian al campo correspondiente y el formulario es utilizable con teclado y foco visible.

**Given** que se presenta el formulario o una changa en el tablón  
**When** se comunica una acción o estado  
**Then** se usa lenguaje cotidiano y claro, el contenido esencial sigue legible al ampliar o redimensionar y la interfaz no transmite estados solo mediante color.

### Story 1.3: Encontrar changas dentro de un radio elegido

As a persona realizadora verificada,
I want buscar changas por una ubicación manual y un radio,
So that pueda encontrar tareas cercanas y conocer su información esencial antes de tomarlas.

**References:** FR-5, FR-6, NFR-1, NFR-8, UX-DR3, UX-DR4, UX-DR5, UX-DR14, UX-DR15, UX-DR16.

**Acceptance Criteria:**

**Given** que una persona verificada define una ubicación de búsqueda manual  
**When** elige un radio de 1, 3, 5 o 10 km  
**Then** el tablón muestra solo changas disponibles dentro de ese radio y no amplía el radio silenciosamente.

**Given** que una persona abre el filtro de radio sin haber elegido otro valor  
**When** se muestran los controles de búsqueda  
**Then** el valor inicial es 3 km.

**Given** que una ubicación de prueba tiene coordenadas ficticias de referencia  
**When** el sistema calcula su distancia a la ubicación de búsqueda  
**Then** muestra una distancia aproximada en línea recta y no consulta servicios de mapas ni GPS.

**Given** que una persona elige guardar su ubicación manual como preferencia  
**When** confirma esa elección  
**Then** la preferencia se conserva para búsquedas posteriores; si no lo elige, la ubicación no se guarda.

**Given** que hay changas dentro del radio  
**When** se muestran los resultados  
**Then** cada tarjeta presenta descripción, recompensa en ARS, zona aproximada, distancia aproximada y materiales, pero no la dirección exacta.

**Given** que no hay resultados o falla la búsqueda  
**When** se presenta el estado del tablón  
**Then** se explica que no hay coincidencias o que ocurrió un error, se permite ajustar la búsqueda o reintentar, y no se inventan resultados; las changas de muestra solo aparecen si existen como datos locales curados e identificados para la demo.

**Nota de alcance:** los radios y coordenadas son para la demo; radios y precisión de distancia del producto real siguen abiertos en PRD OQ-3.

**Given** que el tablón está cargando resultados  
**When** se presenta el estado de carga  
**Then** se mantiene la estructura de la página sin mostrar changas ficticias como si fueran resultados reales.

**Given** que una persona usa el filtro y el tablón desde celular o laptop  
**When** navega con teclado, cambia el radio o recibe un estado vacío/error  
**Then** los controles mantienen foco visible y nombres accesibles, el estado se expresa con texto además de color y el contenido se adapta sin ocultar acciones.

### Story 1.4: Tomar una changa disponible sin duplicar asignaciones

As a persona realizadora verificada,
I want tomar una changa disponible publicada por otra persona,
So that pueda reservarla sin que dos cuentas queden asignadas al mismo trabajo.

**References:** FR-7, NFR-4, AD-3, AD-4, AD-6, AD-7, UX-DR7, UX-DR14, UX-DR16, Academic Demo Scope.

**Acceptance Criteria:**

**Given** que una persona verificada distinta de quien publicó consulta una changa disponible  
**When** confirma que quiere tomarla  
**Then** el sistema crea una única asignación y la changa deja de estar disponible.

**Given** que la persona solicitante intenta tomar su propia changa  
**When** confirma la acción  
**Then** la operación se rechaza y la changa permanece disponible sin asignación.

**Given** que varias personas intentan tomar simultáneamente la misma changa disponible  
**When** se procesan las solicitudes concurrentes  
**Then** como máximo una obtiene la asignación y todas las demás reciben un resultado claro de que la changa ya no está disponible.

**Given** que la misma persona ya tomó la changa y repite la solicitud tras una respuesta perdida o incierta  
**When** el sistema procesa el mismo comando de toma  
**Then** devuelve correctamente la asignación existente sin crear una segunda asignación.

**Given** que la changa ya está asignada a otra persona  
**When** alguien distinto intenta tomarla  
**Then** recibe HTTP 409 con el código estable `changa_already_assigned`, sin alterar la asignación ni el estado actual.

**Given** que la changa está cancelada  
**When** una persona intenta tomarla  
**Then** se rechaza la operación y no se altera el estado actual.

**Given** que una persona revisa una changa disponible  
**When** abre su detalle antes de tomarla  
**Then** la acción de tomar es separada, está identificada claramente y requiere confirmación deliberada.

**Given** que la changa cambia de estado mientras una persona revisa su detalle  
**When** esa persona intenta tomarla  
**Then** la interfaz informa que ya no está disponible, actualiza el estado y no muestra la operación como exitosa.

### Story 1.5: Coordinar una changa asignada con privacidad

As a persona solicitante o realizadora asignada,
I want decidir por separado si comparto mi teléfono y consultar la dirección solo si soy la persona realizadora asignada,
So that pueda coordinar el trabajo sin exponer datos personales a otras personas.

**References:** FR-6, FR-9, NFR-1, NFR-2, AD-5, UX-DR8, UX-DR14, UX-DR15, UX-DR16, Academic Demo Scope.

**Acceptance Criteria:**

**Given** que una changa está asignada  
**When** cada parte consulta la información de coordinación  
**Then** cada una puede expresar su consentimiento telefónico de forma explícita e independiente, y ninguna ve el teléfono de la otra antes de que ambas acepten.

**Given** que ambas partes aceptan compartir sus teléfonos  
**When** el sistema registra ambos consentimientos  
**Then** cada parte puede ver el teléfono ficticio de la otra y ninguna persona ajena a la asignación puede consultarlo.

**Given** que una parte rechaza o todavía no responde al consentimiento  
**When** una parte consulta el estado de coordinación  
**Then** la asignación permanece activa, ambos teléfonos siguen ocultos y no se ofrece cancelación automática ni un canal alternativo de contacto.

**Given** que una persona realizadora está asignada a la changa  
**When** consulta la ubicación necesaria para realizarla  
**Then** puede ver la dirección exacta ficticia, incluso si el consentimiento telefónico está incompleto.

**Given** que una persona no está asignada a la changa  
**When** intenta consultar la dirección exacta o los teléfonos  
**Then** el sistema deniega esos datos, aunque conozca el identificador de la changa.

**Given** que ambas partes necesitan coordinarse dentro de la demo  
**When** no se ha habilitado el consentimiento mutuo  
**Then** la interfaz no simula un chat ni revela contactos reales.

**Given** que una parte consulta el estado del consentimiento o la ubicación  
**When** la interfaz comunica el resultado  
**Then** lo expresa con texto claro en español, no depende solo del color y permite operar los controles con teclado y foco visible.

**Bloqueo para el producto real:** resolver la consecuencia de la falta de consentimiento telefónico según PRD OQ-4 antes de habilitar coordinación con personas usuarias reales.

### Story 1.6: Elegir entre postulaciones

As a persona solicitante,
I want recibir postulaciones y elegir a una persona realizadora,
So that pueda asignar la changa según quién se adapte mejor a la tarea.

**References:** FR-2, FR-4, FR-8, NFR-4, AD-3, AD-4, AD-6, AD-7, UX-DR6, UX-DR7, UX-DR14; producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una persona solicitante publica una changa en modalidad de selección  
**When** la publicación queda disponible  
**Then** una persona realizadora puede postularse sin quedar asignada automáticamente.

**Given** que existen postulaciones para una changa todavía disponible  
**When** la persona solicitante revisa la changa  
**Then** puede consultar la lista y elegir una postulación para asignar.

**Given** que la persona solicitante elige una postulación  
**When** se confirma la asignación  
**Then** solo la persona elegida queda asignada, las otras postulaciones quedan registradas como no seleccionadas y la changa deja de estar disponible.

**Given** que falla una escritura durante la confirmación de una selección  
**When** la transacción no puede completar el commit  
**Then** no queda persistida ninguna parte de la selección: la asignación y los estados de todas las postulaciones conservan sus valores previos.

**Given** que hay postulaciones no seleccionadas tras confirmar una asignación  
**When** se actualiza el resultado de la selección  
**Then** cada persona no seleccionada recibe un aviso por el canal configurado para el producto real.

**Given** que la changa ya no está disponible o una postulación ya no puede asignarse  
**When** la persona solicitante intenta elegirla  
**Then** no se crea una asignación duplicada y se informa que el estado cambió.

**Bloqueos para la implementación en el producto real:** definir canal y momento del aviso a las personas no seleccionadas, y qué ocurre si no hay postulantes o nadie es elegido (preguntas abiertas del PRD sobre FR-4 y ciclo de vida). Esta historia no forma parte de la demo.

### Story 1.7: Buscar changas mediante GPS optativo

As a persona realizadora,
I want usar mi ubicación actual solo cuando lo solicito explícitamente,
So that pueda encontrar changas cercanas sin que la aplicación guarde mi ubicación puntual ni me rastree.

**References:** FR-5, NFR-1, UX-DR5, UX-DR14; producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una persona elige explícitamente buscar mediante GPS  
**When** activa esa opción  
**Then** la aplicación solicita permiso en ese momento y no antes.

**Given** que la persona autorizó el uso de GPS para esa búsqueda  
**When** se consultan las changas dentro del radio seleccionado  
**Then** se usa la ubicación puntual solo para esa búsqueda, no se conserva ni se utiliza para seguimiento, y los resultados no muestran su punto exacto.

**Given** que la persona deniega el permiso o el GPS no está disponible  
**When** intenta buscar por ubicación  
**Then** la aplicación ofrece ingreso manual y no vuelve a solicitar permiso repetidamente.

**Given** que la persona no elige guardar una ubicación manual  
**When** termina la búsqueda  
**Then** no se conserva una ubicación de búsqueda precisa como preferencia.

**Bloqueos para la implementación en el producto real:** definir radios y precisión de distancia para el lanzamiento (PRD OQ-3), y completar la revisión legal/de privacidad previa a tareas reales. Esta historia no forma parte de la demo.

### Story 1.8: Publicar solo changas de categorías permitidas

As a persona solicitante,
I want elegir una categoría aprobada al publicar una changa,
So that el tablón no ofrezca trabajos profesionales o de alto riesgo fuera del propósito del producto.

**References:** FR-2, NFR-5; producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una persona solicitante publica una changa para el producto real  
**When** completa el formulario  
**Then** debe elegir una categoría del catálogo aprobado antes de que la changa pueda quedar disponible.

**Given** que una categoría no está aprobada o una tarea queda fuera de las reglas definidas  
**When** se intenta publicarla  
**Then** no se publica como disponible y se informa el motivo correspondiente.

**Given** que el formulario permite una descripción libre además de la categoría  
**When** se revisa la publicación  
**Then** la categoría aprobada no se interpreta como garantía de que el contenido sea seguro y se aplican las validaciones aprobadas para el lanzamiento.

**Bloqueos para la implementación en el producto real:** definir el catálogo de categorías, tareas prohibidas, validaciones, tratamiento de publicaciones fuera de alcance y proceso de moderación (PRD OQ-5). Esta historia no forma parte de la demo.

## Epic 2: Completar y cerrar una changa

Como persona solicitante y realizadora asignadas, queremos registrar la finalización, resolver cancelaciones permitidas, declarar por separado el pago externo y calificarnos, para dejar constancia del resultado sin que la aplicación procese ni verifique el pago.

### Story 2.1: Confirmar la realización dentro del plazo

As a persona realizadora asignada,
I want marcar la changa como terminada y que la persona solicitante pueda confirmarla o rechazarla,
So that quede registrado el resultado del trabajo sin cerrarlo automáticamente.

**References:** FR-10, UX-DR9, UX-DR14, UX-DR15, UX-DR16, AD-3, Academic Demo Scope.

**Acceptance Criteria:**

**Given** que una changa está asignada a una persona realizadora  
**When** esa persona la marca como terminada  
**Then** la changa pasa a estado pendiente de respuesta y empieza un plazo de 20 minutos para la persona solicitante.

**Given** que la changa está pendiente de respuesta y no venció el plazo  
**When** la persona solicitante confirma la realización  
**Then** queda registrada la confirmación y la changa no se cierra hasta completar las declaraciones de pago requeridas.

**Given** que la changa está pendiente de respuesta y no venció el plazo  
**When** la persona solicitante rechaza la realización  
**Then** la changa pasa a “pendiente de revisión” sin cierre ni reasignación automática.

**Given** que vencen los 20 minutos sin respuesta  
**When** el sistema evalúa el estado de la changa  
**Then** pasa a “pendiente de revisión” sin cierre ni reasignación automática.

**Given** que la changa está pendiente de revisión o el plazo ya venció  
**When** una persona intenta confirmar o rechazar desde el flujo normal  
**Then** la acción no altera el estado y no elude la revisión pendiente.

**Given** que una persona consulta una changa con finalización pendiente o revisión iniciada  
**When** ve su estado  
**Then** se muestra el plazo o el estado de revisión con texto claro, sin asignar culpas ni simular una decisión administrativa.

### Story 2.2: Cancelar una changa antes de su asignación

As a persona solicitante,
I want cancelar una changa que todavía no fue asignada,
So that pueda retirar una solicitud que ya no necesito.

**References:** FR-13, UX-DR14, UX-DR15, UX-DR16, AD-3, Academic Demo Scope.

**Acceptance Criteria:**

**Given** que una persona solicitante autenticada publicó una changa disponible y sin asignación  
**When** confirma que quiere cancelarla  
**Then** la changa queda cancelada y deja de aparecer entre las disponibles.

**Given** que la persona no publicó esa changa  
**When** intenta cancelarla  
**Then** la operación se rechaza y el estado no cambia.

**Given** que una changa ya está asignada  
**When** la persona solicitante intenta cancelarla desde el flujo de demo  
**Then** la operación no se permite; la cancelación posterior a la asignación queda fuera del alcance académico.

**Given** que una toma y una cancelación ocurren simultáneamente sobre la misma changa  
**When** se procesan ambas transiciones  
**Then** solo una transición válida queda persistida; la changa no puede quedar a la vez asignada y cancelada.

**Given** que la persona inicia la cancelación desde celular o laptop  
**When** revisa la acción y confirma  
**Then** la consecuencia se presenta en lenguaje claro, la confirmación es deliberada y los controles se pueden usar con teclado y foco visible.

### Story 2.3: Declarar por separado el pago externo

As a persona solicitante o realizadora de una changa confirmada,
I want confirmar si pagué o recibí la recompensa publicada fuera de la aplicación,
So that queden registradas ambas declaraciones antes del cierre normal.

**References:** FR-11, FR-12, UX-DR10, UX-DR14, UX-DR15, UX-DR16, AD-3.

**Acceptance Criteria:**

**Given** que la realización fue confirmada y la persona solicitante no declaró el pago  
**When** confirma que pagó fuera de la aplicación la recompensa publicada  
**Then** se registra su declaración individual, sin solicitar un monto numérico ni datos de transacción.

**Given** que la realización fue confirmada y la persona realizadora no declaró la recepción  
**When** confirma que recibió fuera de la aplicación la recompensa publicada  
**Then** se registra su declaración individual, sin solicitar un monto numérico ni datos de transacción.

**Given** que solo una parte hizo su declaración  
**When** cualquiera de las partes consulta el estado  
**Then** se identifica cuál declaración está pendiente y la changa no se cierra.

**Given** que ambas partes registraron sus declaraciones  
**When** se procesa la segunda declaración  
**Then** la changa se cierra por el flujo normal y ambas partes quedan habilitadas para calificar.

**Given** que una persona consulta una declaración registrada  
**When** la aplicación la muestra  
**Then** la identifica como afirmación de esa persona, no como pago procesado, custodiado, verificado o garantizado por la plataforma.

**Given** que la realización aún no fue confirmada  
**When** una parte intenta declarar el pago  
**Then** la acción no está disponible y no cambia el estado de la changa.

### Story 2.4: Calificar a la otra parte tras el cierre

As a parte de una changa cerrada normalmente o por decisión administrativa de completado,
I want calificar a la otra persona con una nota de 1 a 5,
So that pueda aportar una señal agregada de experiencia sin publicar comentarios personales.

**References:** FR-12, UX-DR11, UX-DR14, UX-DR15, UX-DR16.

**Acceptance Criteria:**

**Given** que una changa se cerró por el flujo normal o una decisión administrativa determinó que se completó  
**When** cada una de las partes accede a la calificación  
**Then** puede enviar una única nota entera entre 1 y 5, sin texto.

**Given** que la changa no se cerró por el flujo normal ni por una decisión administrativa de completado, fue cancelada o está pendiente de revisión  
**When** una parte intenta calificar  
**Then** la calificación no está disponible y no se registra una nota.

**Given** que una parte ya calificó a la otra en esa changa  
**When** intenta enviar otra calificación para la misma changa  
**Then** el sistema no registra una segunda nota.

**Given** que una persona registrada consulta un perfil  
**When** el perfil tiene calificaciones recibidas  
**Then** muestra el promedio y la cantidad de notas, sin identificar quién otorgó cada una.

**Given** que no hay calificaciones recibidas  
**When** una persona registrada consulta el perfil  
**Then** se informa que todavía no hay calificaciones, sin inventar un promedio.

**Given** que una persona usa la calificación o consulta el perfil desde celular o laptop  
**When** navega con teclado o amplía el contenido  
**Then** la interfaz presenta etiquetas claras, foco visible y estados comprensibles sin depender solo del color.

### Story 2.5: Cancelar una changa asignada por acuerdo mutuo

As a persona solicitante o realizadora asignada,
I want proponer cancelar una changa y obtener la aceptación explícita de la otra parte,
So that pueda terminar una asignación solo cuando ambas personas están de acuerdo.

**References:** FR-13, AD-3, AD-4; producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una changa está asignada y ninguna de las partes la canceló  
**When** una de las partes solicita cancelarla  
**Then** la asignación permanece activa hasta recibir la respuesta explícita de la otra parte.

**Given** que una parte solicitó cancelar la changa  
**When** la otra parte acepta explícitamente  
**Then** la changa queda cancelada y la asignación se libera por acuerdo mutuo.

**Given** que la otra parte rechaza la solicitud o no responde  
**When** se consulta la changa  
**Then** la asignación permanece activa y no se libera automáticamente por rechazo o inactividad.

**Given** que una persona ajena a la asignación intenta cancelar la changa  
**When** envía la solicitud  
**Then** la operación se deniega y el estado no cambia.

**Given** que una cancelación mutua compite con una transición incompatible del ciclo de vida  
**When** se procesan las operaciones  
**Then** solo se persiste una transición válida y la changa no queda en estados incompatibles.

**Bloqueos para la implementación en el producto real:** definir el tratamiento de desacuerdos y otros estados pendientes del ciclo de vida según PRD OQ-7. Esta historia no forma parte de la demo académica.

## Epic 3: Reportar y revisar situaciones problemáticas

Como persona usuaria, quiero reportar una situación y, como persona administradora, quiero revisar reportes o discrepancias con participación de ambas partes, para gestionar incidentes del producto real de manera trazable. Epic 3 pertenece al producto real y queda fuera de la demo académica.

### Story 3.1: Reportar contenido o una interacción

As a persona usuaria registrada,
I want reportar contenido o una interacción indicando un motivo y, si quiero, evidencia,
So that una persona administradora pueda revisarlo.

**References:** FR-14, NFR-3, UX-DR12; backlog del producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una persona usuaria autenticada identifica contenido o una interacción que desea reportar  
**When** completa el motivo y confirma el envío  
**Then** se registra un reporte disponible para revisión administrativa y no se publica para otras personas usuarias.

**Given** que la persona desea adjuntar evidencia  
**When** la incluye en el reporte  
**Then** queda asociada al reporte y protegida con el mismo acceso restringido.

**Given** que la persona omite evidencia  
**When** envía el reporte con un motivo  
**Then** el reporte puede registrarse sin evidencia adjunta.

**Given** que el motivo está vacío o el envío falla  
**When** la persona intenta enviar el reporte  
**Then** se informa el error y no se presenta el reporte como enviado.

**Given** que una persona no tiene autorización administrativa  
**When** intenta consultar un reporte o su evidencia  
**Then** el acceso se deniega.

**Bloqueos para la implementación en el producto real:** definir categorías/motivos de reporte, tipos y límites de evidencia, acceso/retención de evidencias y proceso de respuesta antes del lanzamiento (PRD OQ-5).

### Story 3.2: Solicitar el bloqueo de otra cuenta

As a persona usuaria,
I want solicitar el bloqueo de otra cuenta con una confirmación explícita,
So that tenga un control frente a interacciones que no deseo.

**References:** FR-14, UX-DR12; backlog del producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una persona usuaria elige bloquear otra cuenta  
**When** inicia la acción  
**Then** la aplicación explica que el bloqueo no cancela ni resuelve una changa ya asignada y solicita confirmación.

**Given** que la persona confirma el bloqueo  
**When** se registra la acción  
**Then** queda registrada la relación de bloqueo sin afirmar que una changa asignada se canceló o resolvió.

**Given** que la persona cancela la confirmación  
**When** cierra el diálogo  
**Then** no se registra el bloqueo.

**Bloqueos para la implementación en el producto real:** definir el alcance del bloqueo, qué interacciones impide y su efecto sobre changas asignadas antes de implementar la aplicación efectiva del bloqueo (PRD OQ-5).

### Story 3.3: Revisar un reporte con participación de las partes

As a persona administradora autorizada,
I want revisar un reporte, consultar su evidencia privada y recibir la respuesta de las partes involucradas,
So that pueda registrar una decisión trazable y permitir apelación.

**References:** FR-14, FR-15, NFR-3, UX-DR13; backlog del producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una persona tiene autorización administrativa  
**When** consulta un reporte en revisión  
**Then** puede ver su motivo y la evidencia asociada, sin exponerlos a personas usuarias ajenas al caso.

**Given** que el reporte involucra a dos partes  
**When** el caso sigue abierto  
**Then** cada parte tiene una oportunidad de aportar su respuesta antes de que se registre la decisión.

**Given** que la persona administradora registra una decisión  
**When** completa la revisión  
**Then** quedan registrados la decisión, su motivo y las evidencias consideradas.

**Given** que se comunicó la decisión a las partes involucradas  
**When** una de ellas desea apelar  
**Then** dispone de una acción para presentar una apelación asociada al caso.

**Given** que una persona no tiene autorización administrativa  
**When** intenta acceder a la superficie o evidencia de revisión  
**Then** se deniega el acceso.

**Bloqueos para la implementación en el producto real:** definir qué decisiones y sanciones están permitidas, qué evidencia se admite, quién revisa y resuelve apelaciones, y los plazos de respuesta/apelación (PRD OQ-5 y OQ-6). No fijar plazos en la interfaz hasta que se aprueben.

### Story 3.4: Resolver una discrepancia sobre la realización

As a persona administradora autorizada,
I want registrar si una changa disputada se completó o no,
So that se aplique la transición correspondiente sin presentar el pago externo como verificado.

**References:** FR-10, FR-11, FR-12, FR-13, FR-15, NFR-3, AD-3, AD-5, UX-DR13; backlog del producto real, fuera de demo.

**Acceptance Criteria:**

**Given** que una revisión autorizada determina que la changa no se completó  
**When** la persona administradora registra la decisión y su motivo  
**Then** la asignación se libera y la changa vuelve a estar disponible sin persona realizadora asignada.

**Given** que una revisión autorizada determina que la changa sí se completó  
**When** la persona administradora registra la decisión y su motivo  
**Then** la changa se cierra como completada aunque falten declaraciones de pago y las partes quedan habilitadas para calificar.

**Given** que la changa se cierra por una decisión administrativa  
**When** se muestra su estado o el pago declarado  
**Then** se comunica el cierre administrativo sin afirmar que el pago se procesó, verificó o garantizó.

**Given** que una persona no tiene autorización administrativa  
**When** intenta registrar una decisión sobre la realización  
**Then** la operación se deniega y el estado de la changa no cambia.

**Bloqueos para la implementación en el producto real:** definir evidencia admitida, autoridad y apelaciones, y si una apelación puede suspender o revertir una decisión; resolver además el tratamiento de pagos pendientes y sanciones antes de habilitar la revisión real (PRD OQ-6).
