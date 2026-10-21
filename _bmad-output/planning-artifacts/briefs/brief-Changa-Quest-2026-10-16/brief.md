---
title: Brief de producto — Changa Quest
status: draft
created: 2026-10-16
updated: 2026-10-20
---

# Brief de producto: Changa Quest

## Resumen ejecutivo

Changa Quest conecta a vecinos que necesitan resolver tareas cotidianas pequeñas con personas dispuestas a realizarlas a cambio de una recompensa monetaria. Inspirada en el tablón de solicitudes de un gremio de aventureros de la fantasía, lleva esa dinámica a barrios o ciudades pequeñas: alguien publica una changa y otra persona puede tomarla.

El proyecto se desarrolla como trabajo final de Programación Aplicada 2. Su impacto social esperado tiene dos resultados de igual importancia: ayudar a resolver necesidades cotidianas de vecinos y generar ingresos locales por changas. El solicitante sigue siendo el usuario primario porque su necesidad inicia el flujo. Estos resultados son hipótesis a argumentar, no impactos ya medidos. La solución deberá poder ejecutarse en un servidor local y demostrarse en clase.

## Problema y oportunidad

Un vecino puede necesitar ayuda con tareas puntuales —por ejemplo, barrer la vereda, cortar el pasto o recoger escombros no peligrosos— y ofrecer una recompensa por resolverlas. Otras personas pueden estar interesadas en realizar pequeños trabajos remunerados. Changa Quest propone un tablón local para hacer visible la solicitud y conectar ambas partes.

**Decisión de enfoque:** la persona que publica la solicitud es el cliente primario: su necesidad origina la changa. La persona que la realiza aporta la solución; el impacto social esperado surge al completar solicitudes. La propuesta no apunta a empleos profesionales ni a crear un mercado general de empleo.

**[POR VALIDAR]** No se documentaron todavía evidencias sobre cómo los vecinos resuelven hoy estas necesidades, cuán frecuente es el problema ni si usarían la aplicación.

## Usuarios

- **Primario — vecino solicitante adulto (18+):** necesita resolver una tarea cotidiana pequeña y publica su descripción, recompensa y zona.
- **Secundario — persona adulta que busca changas (18+):** explora las solicitudes disponibles y puede postularse para realizarlas.

Al registrarse, cada persona declara tener 18 años o más y verifica su correo electrónico. No se recopilan documentos oficiales; esto no constituye verificación de identidad legal.

## Propuesta de solución

Un tablón digital de solicitudes de changas remuneradas, enfocado en barrios o ciudades pequeñas. El solicitante publica la tarea y la recompensa en pesos argentinos (ARS); otras personas pueden encontrarla y ofrecerse para realizarla. La aplicación incluye registro de usuarios y calificaciones.

Al publicar, el solicitante elige entre asignar la changa a la primera persona que la tome o recibir postulaciones y elegir a quién asignarla. Después de la asignación, ambas modalidades siguen el mismo flujo. Al asignar una postulación, las demás se marcan como no seleccionadas y se notifica a quienes las enviaron.

**Diseño de ubicación:** la ubicación de la solicitud es el lugar del trabajo, no necesariamente el domicilio del solicitante. Quien busca changas puede ingresar una dirección o solicitar que el móvil detecte su ubicación actual y configurar un radio. El GPS se usa solo para esa búsqueda y no se guarda; una dirección ingresada manualmente puede guardarse como preferencia. Antes de la asignación se muestran el barrio o zona y una distancia aproximada, sin pin preciso ni dirección. La dirección exacta se revela únicamente a la persona asignada.

Al terminar el trabajo, la persona asignada lo marca como terminado y el solicitante confirma que se completó. Luego ambas partes declaran que se realizó el pago externo. La aplicación registra esas declaraciones, pero no verifica ni garantiza la transferencia ni procesa o custodia fondos. Una integración con Mercado Pago queda como posibilidad futura.

Una vez que ambas partes confirman el pago, pueden calificarse mutuamente una vez, con una puntuación de 1 a 5 sin comentarios escritos. Los usuarios registrados ven en cada perfil el promedio y la cantidad de calificaciones, no quién dio cada nota.

El MVP no incluye chat; permite compartir datos de contacto solo después de asignar la changa. El dato o canal específico está por definir.

Si falta una confirmación de realización o de pago, la changa permanece pendiente. Si el solicitante rechaza el trabajo terminado, también queda pendiente; no se reasigna ni se cierra automáticamente. En esos casos, las partes resuelven el desacuerdo por fuera de la aplicación.

El solicitante puede cancelar antes de la asignación. Después, solo se cancela por acuerdo mutuo; si no hay acuerdo, sigue asignada y no se permite el abandono unilateral. Si luego no se completa, queda pendiente, sin cierre automático ni resolución interna.

## Alcance y restricciones

**Incluido en el MVP**

- Registro de usuarios adultos (18+), con declaración de edad y verificación del correo electrónico; sin verificación documental de identidad.
- Publicación y consulta de changas de tareas cotidianas simples y de bajo riesgo, con recompensa en ARS y zona. Se excluyen trabajos que requieran licencia o impliquen altura, maquinaria peligrosa o materiales peligrosos.
- Para el producto objetivo, asignación por orden de llegada o por elección del solicitante entre postulantes. La demostración académica incluye solo la modalidad por orden de llegada; la selección queda fuera de su alcance.
- Confirmación de realización, registro de las declaraciones de pago y calificaciones mutuas (1–5) después del cierre.
- Contacto entre las partes solo después de la asignación; sin chat.
- Ejecución en servidor local para una demostración funcional en clase.

**Fuera del MVP**

- Procesamiento de pagos dentro de la aplicación.
- Reportes y revisión administrativa de desacuerdos; quedan como posibilidad futura.
- Piloto con usuarios reales como requisito de la materia.
- Aplicación móvil nativa; el MVP será una aplicación web adaptable a distintos tamaños de pantalla, diseñada primero para dispositivos móviles y demostrable en una laptop.

**Restricción técnica académica:** .NET y Entity Framework con arquitectura N-Layers, según la guía provista. La estructura separa Presentación, Lógica de Negocio, Acceso a Datos y Entidades; el detalle técnico se desarrolla en la arquitectura.

## Impacto y criterios de éxito

La hipótesis es que hacer visibles las solicitudes y conectar a las partes puede resolver necesidades cotidianas y generar ingresos locales. Ambos resultados tienen igual importancia; el solicitante sigue siendo el usuario primario porque su necesidad inicia el flujo. No hay validación de usuarios ni mediciones de impacto.

Para el trabajo final, el equipo debe argumentar ese impacto potencial y levantar la aplicación en un servidor local. La demostración funcional cubrirá:

1. **Flujo completo de la demo:** publicar una changa segura con recompensa y zona; otra persona la encuentra y la toma por orden de llegada; marcarla terminada; confirmar su realización; registrar las dos declaraciones de pago externo; y permitir que ambas partes se califiquen.
2. La modalidad de postulaciones y selección pertenece al producto objetivo, pero no se implementa ni se demuestra en esta etapa académica.

No se requiere implementar un producto completo ni realizar un piloto real. Los datos concretos de muestra pueden definirse al preparar la presentación.

## Inspiración y posicionamiento

La referencia creativa es el sistema de gremios y tablones de solicitudes de obras de fantasía, como *Goblin Slayer*, *Fairy Tail* y *Overlord*. La inspiración representa el flujo de solicitud, publicación y cumplimiento; la propuesta lo abstrae como una plataforma donde una persona solicita una tarea y otra la realiza a cambio de una recompensa. No se afirma que el concepto sea único frente a productos existentes.

## Preguntas abiertas

- ¿Qué barrio o ciudad se usará como comunidad inicial y contexto de la demostración?
- ¿Cómo se moderan las publicaciones que no cumplen los límites de tareas del MVP?
- ¿Qué límites tendrá el radio de búsqueda y qué precisión tendrá la zona aproximada?
- ¿Qué dato de contacto se comparte tras la asignación?
- ¿Qué argumentos o evidencia respaldarán el impacto social potencial sin presentarlo como validado?
