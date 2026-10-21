# Epic 1 Context: Publicar, encontrar y coordinar changas

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Habilitar a personas adultas elegibles a publicar changas locales, encontrarlas dentro de un radio elegido, asignarlas sin duplicados y coordinarse protegiendo ubicaciones y teléfonos. La demo académica cubre el recorrido local con datos ficticios; no habilita trabajos, ubicaciones, contactos ni pagos reales.

## Stories

- Story 1.1: Crear y verificar una cuenta adulta
- Story 1.2: Publicar una changa con información suficiente
- Story 1.3: Encontrar changas dentro de un radio elegido
- Story 1.4: Tomar una changa disponible sin duplicar asignaciones
- Story 1.5: Coordinar una changa asignada con privacidad
- Story 1.6: Elegir entre postulaciones
- Story 1.7: Buscar changas mediante GPS optativo
- Story 1.8: Publicar solo changas de categorías permitidas

## Requirements & Constraints

- Solo cuentas con correo verificado y declaración de mayoría de edad pueden publicar o tomar changas. Una cuenta puede actuar como solicitante y realizadora.
- Una publicación requiere descripción, recompensa positiva en ARS, ubicación del trabajo y respuesta explícita sobre materiales; si hacen falta, se indica quién los aporta. La modalidad queda visible antes del compromiso. En la demo se usa asignación por orden de llegada y descripción libre; los datos de muestra son ficticios y curados.
- En la demo, la búsqueda es manual, sin GPS ni mapas, con radio inicial de 3 km y opciones de 1, 3, 5 y 10 km. Los resultados respetan estrictamente el radio y muestran distancia aproximada en línea recta. La ubicación manual solo se guarda como preferencia si la persona lo elige.
- Antes de asignar, mostrar solo zona aproximada y distancia, nunca dirección exacta. La dirección exacta corresponde únicamente a la persona realizadora asignada. No conservar ubicación GPS puntual ni permitir acceso a datos sensibles por conocer el identificador de la changa.
- Una changa disponible solo puede asignarse a una persona realizadora distinta de quien la publicó. Las solicitudes simultáneas no producen asignaciones duplicadas; repetir una toma confirmada por la misma persona devuelve la asignación existente y otra persona recibe HTTP 409 con `changa_already_assigned`.
- Solicitar el consentimiento telefónico de cada parte por separado y solo tras asignar; revelar ambos teléfonos únicamente con consentimiento explícito mutuo. No hay chat. En la demo, rechazo o falta de respuesta mantiene activa la asignación y los teléfonos ocultos, sin cancelación automática ni canal alternativo.
- La demo excluye selección entre postulaciones (Story 1.6), GPS (Story 1.7) y catálogo de categorías aprobadas (Story 1.8). No habilitar categorías de riesgo ni tareas que requieran licencia para un lanzamiento real sin definiciones y revisión correspondientes.
- La interfaz debe ser adaptable, mobile-first, usable con teclado y ampliación; mantener etiquetas persistentes, foco visible, errores asociados a campos y estados comprensibles sin depender solo del color.

## Technical Decisions

- Arquitectura monolítica modular N-Layers: Backend API → Presentation → BusinessLogic → DataAccess → Entities, con BusinessLogic → Entities. Core provee soporte técnico compartido, no es capa de negocio. Presentation adapta HTTP y delega; BusinessLogic posee las reglas; DataAccess es responsable de EF Core, DbContext, consultas y persistencia. El host API configura servicios y middleware, sin reglas de negocio.
- `ChangaManager` es la autoridad para mutaciones del ciclo de vida y asignaciones. Las operaciones incompatibles se validan contra el estado persistido y se confirman atómicamente; DataAccess no decide resultados de negocio.
- Base tecnológica académica: .NET 10, ASP.NET Core 10, EF Core 10 con SQL Server LocalDB/Express, cliente Blazor WebAssembly y xUnit.net v3. La demo se ejecuta localmente.
- Publicar REST bajo `/api/v1`; un documento OpenAPI versionado es el contrato canónico para API y cliente. Errores HTTP usan `application/problem+json`/Problem Details y no exponen información interna. Revalidar permisos y consentimiento en cada consulta sensible.
- Aplicar SDD/TDD: criterios verificables vinculados a requisitos, pruebas antes de implementar y ciclo Red–Green–Refactor; cubrir asignación concurrente, reintentos y límites de acceso.

## UX & Interaction Patterns

Usar español claro de Argentina y una ambientación sobria y adulta de tablón de gremio, sin convertir la experiencia en juego ni prometer seguridad no verificada. Navegación persistente, en el mismo orden: “Ver tareas”, “Publicar changa”, “Mis changas” y “Perfil”; inferior y compacta en móvil. El tablón permite comparar descripción, recompensa, zona, materiales y modalidad antes de comprometerse; abrir el detalle no equivale a tomar la changa. Informar con claridad carga, vacío, error, ubicación no disponible y cambio de disponibilidad; nunca presentar resultados ficticios como reales ni ampliar el radio silenciosamente.

## Cross-Story Dependencies

La verificación de la Story 1.1 habilita publicación y toma. Las publicaciones de la Story 1.2 alimentan búsqueda y toma; la Story 1.5 requiere una asignación previa de la Story 1.4. La Story 1.6 describe una modalidad alternativa de asignación fuera de la demo. Epic 2 depende de las changas y asignaciones creadas en esta épica.
