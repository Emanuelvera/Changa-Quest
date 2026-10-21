---
title: PRD de producto — Changa Quest
status: draft
created: 2026-10-19
updated: 2026-10-20
---

# PRD: Changa Quest

## 0. Propósito del documento

Este documento define el producto objetivo de Changa Quest para un lanzamiento real inicial en la ciudad de Córdoba, Argentina, y distingue ese alcance de la primera demostración académica local. Está dirigido al equipo de producto y a quienes desarrollen después UX, arquitectura, épicas e historias. Los requisitos describen capacidades y resultados verificables; las decisiones de implementación quedan para arquitectura.

El PRD se basa en el [brief de producto](../../briefs/brief-Changa-Quest-2026-10-16/brief.md), su [anexo](../../briefs/brief-Changa-Quest-2026-10-16/addendum.md) y las decisiones registradas durante esta elaboración. Los detalles técnicos y las fuentes de investigación se conservan en el [anexo del PRD](./addendum.md). Las hipótesis no validadas y las preguntas abiertas se presentan como tales; no se consideran compromisos de lanzamiento resueltos.

## 1. Visión

Changa Quest conecta a personas adultas de una comunidad local: unas publican solicitudes para resolver tareas cotidianas, simples y de bajo riesgo; otras las realizan a cambio de una recompensa acordada. La persona solicitante origina el flujo porque tiene la necesidad. Los dos resultados sociales buscados tienen igual importancia: atender necesidades vecinales y facilitar ingresos locales por changas.

La primera zona objetivo es la ciudad de Córdoba. El producto no cobra a sus usuarios en el lanzamiento inicial; su sostenibilidad y cualquier monetización futura siguen por definirse. La experiencia debe ayudar a que ambas partes entiendan la tarea, la recompensa, la ubicación y quién aporta las herramientas antes de comprometerse.

Resolver necesidades e impulsar ingresos son hipótesis de impacto, no resultados demostrados. No se dispone de entrevistas, métricas de demanda ni evidencia de adopción. Hay plataformas de servicios para el hogar que declaran operar en Córdoba, como [Clickie](https://clickie.com.ar/), con servicios profesionales, presupuestos o precios cerrados y pagos gestionados por la plataforma. Esto confirma un contexto competitivo adyacente, pero no demuestra que la demanda de tareas vecinales pequeñas esté validada ni que no existan alternativas locales informales.

La inspiración creativa es el tablón de solicitudes de un gremio de aventureros de la fantasía, abstraído como un sistema donde una persona solicita ayuda y otra realiza la tarea por una recompensa. No se afirma que la idea sea única ni que las obras que la inspiran validen el mercado.

### Producto objetivo para lanzamiento real

El objetivo es operar inicialmente en Córdoba, para personas adultas, sin cobro a las partes al comienzo. El lanzamiento queda condicionado a la revisión legal, la definición de controles de confianza proporcionales, el procedimiento de reportes y la revisión administrativa de discrepancias de realización, y la validación de seguridad y demanda. La ciudad está definida; los barrios iniciales y la densidad de oferta/demanda no.

El producto debe cubrir publicación, búsqueda, ambas modalidades de asignación, ubicación protegida, consentimiento mutuo para compartir teléfonos, confirmación de realización, declaraciones de pago externo, calificaciones y herramientas básicas de reporte, bloqueo y revisión administrativa de discrepancias de realización. Los requisitos operativos, la autoridad y los plazos de revisión deben definirse antes de trabajar con usuarios reales.

### Primera demostración académica

- Aplicación web adaptable, mobile-first, ejecutable en un servidor local y demostrable desde una laptop.
- Incluir registro y autenticación con verificación de correo en un entorno local de prueba y declaración de tener al menos 18 años. Una misma cuenta puede actuar como solicitante y realizadora.
- Recorrido normal de extremo a extremo: publicar una changa, encontrarla por zona y radio, tomarla por orden de llegada con asignación atómica, coordinarse, completar, confirmar, declarar por separado el pago externo y calificarse.
- La búsqueda usa ubicación manual por barrio/zona y radio configurable, con 3 km por defecto y opciones de 1, 3, 5 y 10 km; no integra GPS ni mapas. Las distancias usan ubicaciones ficticias de prueba con coordenadas de referencia y cálculo aproximado en línea recta. La dirección exacta solo se muestra a la persona realizadora asignada.
- El intercambio de teléfonos requiere consentimiento explícito de ambas partes después de asignar. No incluye chat; teléfonos y ubicaciones exactas son ficticios.
- La persona solicitante puede cancelar antes de la asignación. La cancelación posterior a la asignación queda fuera de esta demo.
- La persona solicitante dispone de 20 minutos para confirmar o rechazar la finalización. El rechazo o la falta de respuesta deja la changa pendiente de revisión, sin resolución administrativa, cierre ni reasignación automática en la demo.
- Tras la confirmación, cada parte declara por separado si pagó/recibió fuera de la aplicación la recompensa publicada. La declaración es una confirmación, no un monto numérico; la app no procesa, custodia ni verifica pagos. Tras el cierre normal, cada parte puede calificar de 1 a 5, una vez y sin texto; los perfiles muestran promedio y cantidad, sin identificar a quien calificó.
- No incluye asignación por selección entre postulaciones, integración de GPS/mapas, chat, procesamiento de pagos, reportes, bloqueos ni resolución administrativa. Estas capacidades permanecen en el alcance del producto objetivo según corresponda.
- Usar datos de ejemplo ficticios; no se realizarán trabajos, contactos ni pagos reales.
- No requiere piloto con usuarios reales ni procesamiento de pagos.
- La presentación académica debe argumentar el impacto social potencial como hipótesis, sin presentarlo como medido o validado.

## 2. Usuarios objetivo

### 2.1 Trabajos que necesitan resolver

- **Persona solicitante:** quiere resolver una tarea pequeña de su entorno sin realizarla personalmente; necesita describirla, ofrecer una recompensa, indicar la zona y aclarar los materiales necesarios.
- **Persona realizadora:** quiere encontrar changas locales adecuadas a su ubicación y conocer la tarea, la recompensa y los recursos requeridos antes de aceptarla.

### 2.2 No usuarios del producto objetivo

- Personas menores de 18 años.
- Personas que buscan contratar o realizar servicios profesionales como propósito principal del producto.
- Publicaciones que requieran licencia profesional o impliquen altura, maquinaria peligrosa o materiales peligrosos.

El alcance de tareas permitidas y la política final de verificación de identidad deben revisarse antes de operar con usuarios reales.

### 2.3 Recorridos de usuario

- **UJ-1. Doña Marta resuelve una vereda sucia sin barrerla personalmente.**
  - **Persona y contexto:** Doña Marta es una vecina adulta; no le gusta ver su vereda sucia, pero tampoco quiere barrerla.
  - **Estado inicial:** ya identificó la necesidad. El recorrido comienza cuando decide solicitar ayuda; el acceso o registro no forma parte de su narración.
  - **Recorrido:** publica una changa para barrer la vereda, ofrece ARS 10.000 e indica que aporta escoba, bolsa y pala. Elige asignación por orden de llegada porque no le importa quién realice la tarea. Una persona toma la changa; después de que ambas aceptan compartir sus teléfonos, pueden coordinarse. La ubicación exacta se revela solo a la persona asignada.
  - **Clímax:** la persona realizadora marca la tarea como terminada y Doña Marta confirma que se completó.
  - **Resolución:** Doña Marta declara el pago externo y la persona realizadora declara haberlo recibido. Tras ambas declaraciones, se califican mutuamente y la changa queda cerrada. La aplicación registra declaraciones; no verifica ni procesa la transferencia.

- **UJ-2. Don Ramón encuentra y completa una changa cercana.**
  - **Persona y contexto:** Don Ramón es un adulto que busca una changa y usa una cuenta personal.
  - **Estado inicial:** inicia sesión y define una ubicación de búsqueda y un radio de alcance; los valores posibles del radio están pendientes.
  - **Recorrido:** la aplicación le muestra changas disponibles dentro del área elegida. Antes de tomar una, puede saber si necesita herramientas y quién las aporta. Ve la solicitud de Doña Marta para barrer la vereda, donde ella declara que aporta los materiales, y la toma por orden de llegada. Después de la asignación, ambas partes aceptan compartir sus teléfonos; Don Ramón se comunica con Doña Marta y recibe la ubicación exacta del trabajo. Realiza la tarea y la marca como terminada.
  - **Clímax:** Doña Marta confirma la tarea; Don Ramón recibe el pago externo y declara haberlo recibido.
  - **Resolución:** después de que ambas partes declaran el pago, se califican mutuamente y la changa queda cerrada.

La decisión entre asignación por orden de llegada y selección entre postulaciones es una capacidad del producto. El segundo modo está definido en §4, pero aún no tiene un recorrido narrado por una persona usuaria.

## 3. Glosario

- **Changa:** tarea cotidiana de bajo riesgo publicada con una recompensa, desde su publicación hasta su cierre.
- **Persona solicitante:** persona adulta que publica una changa y ofrece la recompensa.
- **Persona realizadora:** persona adulta que toma una changa y acuerda realizarla. El término describe un rol del producto y no determina una clasificación jurídica.
- **Postulación:** expresión de interés de una persona realizadora cuando la solicitud permite que la persona solicitante elija entre candidatas.
- **Asignación:** vínculo de una changa con una única persona realizadora elegida o que la tomó primero.
- **Publicación:** representación visible de una changa en la aplicación.
- **Ubicación de búsqueda:** ubicación que la persona realizadora usa para descubrir changas dentro de un radio elegido.
- **Ubicación del trabajo:** lugar físico donde debe realizarse la changa.
- **Declaración de pago:** constancia dentro de la aplicación de que una parte afirma haber pagado o recibido el pago externo acordado.
- **Cierre:** estado alcanzado cuando se confirma la realización y ambas partes declaran el pago; habilita las calificaciones mutuas.
- **Persona administradora:** persona autorizada a revisar los reportes recibidos por la aplicación.

## 4. Funcionalidades

### 4.1 Cuentas y confianza

**Descripción:** las dos partes necesitan cuentas para publicar, buscar, asignar y cerrar changas. El producto objetivo es para personas adultas. La suficiencia de la verificación de correo y de la declaración de edad está reabierta para el lanzamiento real; no debe asumirse que sea un control de confianza suficiente.

**Requisitos funcionales:**

#### FR-1: Registrar y autenticar personas usuarias

Una persona puede crear una cuenta verificando su correo electrónico y declarando que tiene al menos 18 años. La aplicación habilita la publicación o toma de changas únicamente para cuentas verificadas que hayan confirmado esa edad.

**Consecuencias comprobables:**
- Una cuenta sin correo verificado no puede publicar ni tomar una changa.
- La aplicación no permite completar el registro a una persona que declare tener menos de 18 años.
- El mecanismo de verificación de identidad adicional para el lanzamiento real queda sujeto a la revisión de confianza y legal de OQ-2.

### 4.2 Publicar y administrar solicitudes

**Descripción:** la persona solicitante describe una tarea cotidiana segura, su recompensa y dónde debe realizarse. La publicación permite evaluar el trabajo antes de que alguien lo acepte.

**Requisitos funcionales:**

#### FR-2: Publicar una changa

Una persona solicitante puede publicar una changa con descripción, recompensa en ARS, ubicación del trabajo y modalidad de asignación. La publicación debe cumplir los límites de tareas permitidas.

**Consecuencias comprobables:**
- Una changa publicada muestra su descripción, recompensa, zona y modalidad de asignación.
- Solo las categorías aprobadas para el producto pueden aparecer como changas disponibles.
- Las categorías permitidas, las validaciones y el tratamiento de publicaciones fuera de alcance deben definirse antes del lanzamiento (OQ-5).

#### FR-3: Informar herramientas y materiales

Al publicar, la persona solicitante debe indicar si hacen falta herramientas o materiales y, cuando sí, quién los aporta. La publicación debe permitir declarar explícitamente que no se necesita ningún material.

**Consecuencias comprobables:**
- Una persona realizadora puede conocer esta información antes de tomar o postularse a una changa.
- El formulario no permite publicar hasta que se indique si hacen falta materiales y, cuando sí, quién los aporta.
- La forma de representar casos compartidos o recursos alternativos debe definirse durante UX.

#### FR-4: Elegir modalidad de asignación

Al publicar, la persona solicitante puede escoger entre asignar al primer interesado que tome la changa o recibir postulaciones para elegir a una persona realizadora.

**Consecuencias comprobables:**
- La modalidad elegida queda visible antes de que una persona se comprometa.
- En modo de selección, la persona solicitante puede elegir entre las postulaciones recibidas.
- Al asignar una postulación, las demás se marcan como no seleccionadas y se notifica a quienes las enviaron. El canal de notificación está pendiente.

### 4.3 Descubrir changas y proteger la ubicación

**Descripción:** la persona realizadora descubre changas usando una ubicación de búsqueda y un radio que configura. La aplicación comparte solo la precisión de ubicación necesaria para cada estado del flujo.

**Requisitos funcionales:**

#### FR-5: Buscar changas por ubicación y radio

Una persona realizadora autenticada puede definir una ubicación de búsqueda y un radio. La aplicación muestra las changas disponibles cuya ubicación queda dentro de ese radio.

**Consecuencias comprobables:**
- Los resultados excluyen las changas fuera del radio elegido.
- La aplicación permite el ingreso manual de una ubicación o el uso de GPS mediante una acción explícita de la persona.
- El GPS puntual no se conserva ni se utiliza para seguimiento.
- Una ubicación ingresada manualmente solo puede guardarse como preferencia si la persona así lo elige.
- Los valores permitidos, predeterminados y la precisión de búsqueda quedan pendientes de OQ-3.

#### FR-6: Mostrar la ubicación en función de la asignación

Antes de asignar una changa, la aplicación muestra el barrio o zona y una distancia aproximada, pero no la dirección exacta ni un pin preciso. La ubicación exacta se revela solo a la persona realizadora asignada.

**Consecuencias comprobables:**
- Una persona no asignada no puede consultar la dirección exacta.
- La persona asignada puede acceder a la ubicación exacta necesaria para realizar el trabajo.
- La zona previa a la asignación no debe permitir inferir deliberadamente el domicilio exacto.

### 4.4 Asignar y coordinar

**Descripción:** una changa tiene como máximo una persona realizadora asignada. Las partes solo intercambian teléfonos después de asignar y con consentimiento explícito de ambas.

**Requisitos funcionales:**

#### FR-7: Asignar por orden de llegada sin duplicados

En la modalidad por orden de llegada, la changa se asigna a una sola persona realizadora distinta de quien la publicó, siempre que la tome mientras siga disponible.

**Consecuencias comprobables:**
- Si varias personas intentan tomar simultáneamente la misma changa, como máximo una obtiene la asignación.
- Una changa asignada deja de aparecer como disponible para otras personas.
- Una asignación no puede duplicarse por reintentos o solicitudes concurrentes.
- La persona solicitante no puede tomar su propia changa.

#### FR-8: Asignar entre postulaciones

En la modalidad de selección, la persona solicitante puede revisar las postulaciones y asignar la changa a una de ellas.

**Consecuencias comprobables:**
- Solo la postulación elegida se convierte en asignación.
- Las demás postulaciones se cierran como no seleccionadas.
- Las personas no seleccionadas reciben una notificación del resultado.

#### FR-9: Compartir teléfono con consentimiento mutuo

Después de la asignación, cada parte puede aceptar compartir su número de teléfono con la otra. La aplicación solo revela los números si ambas partes aceptan.

**Consecuencias comprobables:**
- Ninguna parte ve el teléfono de la otra antes de la asignación.
- Si una de las partes no acepta, su número no se revela.
- La experiencia y el estado de la changa cuando falta consentimiento de una parte deben definirse antes de cerrar los requisitos de UX y arquitectura (OQ-4).
- El producto no incluye chat en la aplicación.

### 4.5 Completar, declarar el pago y calificar

**Descripción:** la aplicación registra el avance y las declaraciones de las partes, pero no procesa ni garantiza pagos. La confirmación de una parte no sustituye a la de la otra.

**Requisitos funcionales:**

#### FR-10: Confirmar la realización

La persona realizadora asignada puede marcar la changa como terminada. Desde ese momento, la persona solicitante tiene un máximo de 20 minutos para confirmar o rechazar que se completó.

**Consecuencias comprobables:**
- Marcar una changa como terminada no la cierra automáticamente.
- Si la persona solicitante rechaza la realización, se inicia una revisión administrativa.
- Si no responde dentro de los 20 minutos, se inicia una revisión administrativa.
- La aplicación no cierra ni reasigna automáticamente la changa por falta de respuesta.

#### FR-11: Registrar declaraciones de pago externo

Después de la confirmación de realización, ambas partes pueden declarar que se pagó o recibió el importe acordado fuera de la aplicación.

**Consecuencias comprobables:**
- La aplicación registra por separado la declaración de pago de cada parte.
- Una sola declaración no basta para cerrar la changa.
- La aplicación no procesa, custodia, verifica ni garantiza una transferencia.
- En el flujo normal, la changa no se cierra hasta que ambas partes declaran el pago.
- Una decisión administrativa que cierre la changa como completada es una excepción a ese requisito.

#### FR-12: Cerrar y permitir calificaciones mutuas

En el flujo normal, cuando la persona solicitante confirma la realización y ambas partes declaran el pago, la changa queda cerrada y cada parte puede calificar a la otra. Una revisión administrativa también puede cerrar la changa como completada aunque el pago no esté confirmado; en ese caso, las calificaciones igualmente quedan habilitadas.

**Consecuencias comprobables:**
- Cada parte puede enviar una sola calificación por changa, con un valor de 1 a 5.
- Las calificaciones no incluyen comentarios escritos.
- Los perfiles muestran el promedio y la cantidad de calificaciones a personas usuarias registradas; no muestran quién otorgó cada calificación.
- No se habilitan calificaciones antes del cierre normal o de una decisión administrativa de finalización.

#### FR-13: Cancelar según el estado de la changa

La persona solicitante puede cancelar antes de la asignación. Después, la cancelación requiere el acuerdo mutuo de ambas partes.

**Consecuencias comprobables:**
- Una persona realizadora no puede abandonar unilateralmente una changa asignada.
- La asignación puede liberarse tras una revisión administrativa que concluya que el trabajo no se completó.
- La aplicación no libera una asignación automáticamente por inactividad o desacuerdo.
- El tratamiento de casos pendientes fuera de una revisión de realización queda pendiente de OQ-7.

### 4.6 Reportar, bloquear y revisar

**Descripción:** el producto para lanzamiento incluye herramientas básicas para que las personas usuarias reporten o bloqueen y para que una persona administradora revise reportes. Estas capacidades pueden quedar fuera de la primera demostración académica.

**Requisitos funcionales:**

#### FR-14: Reportar y bloquear

Una persona usuaria puede reportar contenido o una interacción que considere problemática y bloquear a otra persona usuaria. **[ASSUMPTION: un bloqueo impide nuevas interacciones entre ambas personas; confirmar el alcance y su efecto sobre changas ya asignadas.]**

**Consecuencias comprobables:**
- La aplicación registra la acción de reportar o bloquear.
- Un reporte queda disponible para revisión administrativa (FR-15).
- El alcance del bloqueo, las categorías de reporte y el tratamiento de una changa ya asignada están pendientes de OQ-5.

#### FR-15: Revisar reportes y discrepancias de realización

Una persona administradora puede revisar reportes y discrepancias sobre la realización, registrar motivos y evidencias, dar a ambas partes la oportunidad de responder y permitir una apelación.

**Consecuencias comprobables:**
- Si la revisión concluye que el trabajo no se completó, la asignación actual se libera y la changa vuelve a estar disponible sin asignación para que otra persona pueda tomarla; se revisa una posible sanción a la persona realizadora anterior.
- Si la revisión concluye que el trabajo sí se completó, la changa se cierra como completada aunque no se haya confirmado el pago; se revisa una posible sanción a la persona solicitante.
- La revisión no procesa pagos. Criterios de evidencia, plazos de respuesta y apelación, catálogo de sanciones, consecuencias y tratamiento de pagos pendientes deben definirse antes del lanzamiento (OQ-6).
- La intervención por falta de respuesta comienza al vencer los 20 minutos de FR-10; el tiempo de respuesta de administración aún no está definido.

## 5. Requisitos no funcionales transversales

- **Privacidad de ubicación:** aplicar FR-5 y FR-6; no conservar ubicación GPS puntual ni divulgar una dirección exacta antes de la asignación.
- **Privacidad de contacto:** revelar números de teléfono solo después de asignar y obtener aceptación explícita de ambas partes.
- **Seguridad de acceso:** proteger cuentas, datos de contacto, ubicación exacta e información de reportes contra acceso de personas no autorizadas. Los controles concretos, retención y auditoría quedan pendientes de arquitectura y revisión de privacidad.
- **Concurrencia e integridad:** la asignación por orden de llegada debe mantener la regla de una única persona asignada incluso ante solicitudes simultáneas (FR-7).
- **Seguridad de las tareas:** excluir tareas de alto riesgo y que requieran licencia profesional; definir categorías, moderación y procedimiento administrativo antes de operar (FR-2, FR-14, FR-15).
- **Cumplimiento:** no habilitar un lanzamiento con personas usuarias reales hasta que asesoría legal argentina revise la clasificación y las obligaciones laborales, impositivas, de consumo, privacidad y seguridad aplicables al modelo. La información pública sobre casas particulares menciona limpieza y jardinería, pero no determina por sí sola la clasificación de esta plataforma.
- **Disponibilidad, tiempos de respuesta, accesibilidad, soporte y retención:** establecer objetivos medibles antes del lanzamiento real; no se recibieron umbrales.
- **Plataforma:** aplicación web adaptable, diseñada primero para móviles y accesible también desde laptop. La aplicación móvil nativa no forma parte del alcance inicial.
- **Idioma:** **[ASSUMPTION: español de Argentina para la primera zona de lanzamiento, Córdoba]**; confirmar en definición UX.

## 6. No objetivos

- Crear un mercado general de empleo o priorizar trabajos profesionales.
- Publicar tareas que requieran licencia o impliquen altura, maquinaria peligrosa o materiales peligrosos.
- Procesar, custodiar, verificar o garantizar pagos dentro de Changa Quest.
- Proveer chat en la aplicación.
- Garantizar pagos, actuar como tribunal laboral o resolver automáticamente desacuerdos fuera del proceso administrativo de realización definido en FR-15.
- Lanzar una aplicación móvil nativa en la primera etapa.
- Cobrar a las partes al inicio del lanzamiento. La monetización posterior no está decidida.
- Tratar el éxito de la demo académica como evidencia de demanda, seguridad o impacto social.

## 7. Métricas de éxito

Las métricas no tienen umbrales validados. Deben medirse durante un piloto controlado y fijarse después de conocer la línea de base, sin convertir las metas en afirmaciones previas de impacto.

**Primarias**

- **SM-1 — Personas usuarias activas por rol:** cantidad de personas solicitantes y personas realizadoras activas en un período acordado. Valida FR-1, FR-2 y FR-5. Definición de “activa”, período y objetivo pendientes.
- **SM-2 — Changas completadas:** cantidad de changas cerradas como completadas, ya sea por el flujo normal o por una revisión administrativa, en un período acordado. Valida FR-10, FR-12 y FR-15. El objetivo y el período están pendientes.
- **SM-3 — Recompensas declaradas:** suma en ARS de las recompensas de changas con ambas declaraciones de pago. Valida FR-11 y FR-12. Es un importe declarado por las partes, no un movimiento verificado por la plataforma.

**Contramétricas**

- **SM-C1 — Seguridad y moderación:** reportes, bloqueos y casos pendientes o revisados, junto con tiempos de revisión. Valida FR-14 y FR-15. No se debe optimizar a la baja la cantidad de reportes como si menos reportes significaran automáticamente más seguridad.
- **SM-C2 — Resolución incompleta:** changas asignadas que quedan pendientes por rechazo, falta de confirmación, cancelación o desacuerdo. Valida FR-10, FR-11 y FR-13, y contrapesa el conteo de changas completadas; definiciones y umbrales pendientes.

## 8. Preguntas abiertas

1. **Bloqueo de lanzamiento — revisión legal:** ¿qué obligaciones laborales, impositivas, de consumo, privacidad, seguridad y contratación aplican al modelo en Argentina y Córdoba? Responsable: equipo de producto con asesoría legal argentina. La evidencia de aprobación y quién registra el go/no-go siguen abiertos; no se permiten trabajos reales hasta resolverlo. **Revisar antes de habilitar tareas reales.**
2. **Confianza e identidad:** ¿qué controles proporcionales se necesitan además de la declaración de edad y el correo verificado? La decisión de no solicitar documentos se reabrió y no debe considerarse vigente para un lanzamiento real sin revisión. **Responsables:** Producto y Operaciones, con asesoría legal. **Revisar antes de habilitar cuentas para tareas reales.**
3. **Búsqueda para lanzamiento real:** ¿qué radios mínimos, máximos y predeterminados se ofrecerán, con qué precisión y cómo se definirá la distancia aproximada? Para la demo académica se acuerdan 3 km por defecto y opciones de 1, 3, 5 y 10 km; no se integra GPS ni mapas. **Responsables:** Producto y UX. **Revisar los parámetros del producto real antes de cerrar el diseño de lanzamiento.**
4. **Consentimiento de contacto para lanzamiento real:** si una parte no acepta compartir el teléfono después de la asignación, ¿cómo continúa o termina el flujo sin revelar datos ni dejar a las partes en un estado ambiguo? Para la demo académica, la asignación permanece activa, los teléfonos siguen ocultos y no hay cancelación automática ni canal alternativo. **Responsables:** Producto y UX. **Revisar la regla de producto real antes de habilitar coordinación con personas usuarias.**
5. **Seguridad y moderación:** ¿qué categorías de reporte, medidas administrativas, apelaciones y plazos de revisión se requieren? ¿Cómo se gestiona el bloqueo cuando ya hay una changa asignada? **Responsables:** Producto y Operaciones. **Revisar antes de habilitar reportes, bloqueos o tareas reales.**
6. **Revisión administrativa de realización:** ¿qué evidencia se admite? ¿Quién revisa el caso y quién resuelve las apelaciones? ¿Qué sanciones proporcionales están disponibles y en qué plazos? ¿Qué ocurre con un pago pendiente si la changa se cierra como completada por decisión administrativa? **Responsables:** Producto y Operaciones, con asesoría legal. **Revisar antes de habilitar la revisión administrativa para casos reales.**
7. **Otros estados sin resolución:** ¿qué ocurre si nadie toma una changa, la persona asignada no se presenta, falta una declaración de pago o las partes no acuerdan cancelar? ¿La administración revisa estos casos además de las discrepancias de realización? **Responsables:** Producto y Operaciones. **Revisar antes de cerrar el ciclo de vida y habilitar tareas reales.**
8. **Liquidez del mercado:** ¿qué oferta y demanda por barrio y qué tiempo de asignación bastan para sostener un piloto o ampliar la zona? Mantener estas señales como investigación, no como metas de éxito fijadas, hasta contar con datos locales. **Responsables:** Producto e Investigación. **Revisar antes de decidir la escala del piloto o ampliar la zona.**
9. **Comunidad inicial:** ¿qué barrios de Córdoba se incluirán primero y qué evidencia mostrará que existe oferta suficiente de personas realizadoras y solicitudes? **Responsables:** Producto e Investigación. **Revisar antes de seleccionar los barrios del piloto.**
10. **Medición:** ¿qué período define una persona activa, cuándo se inicia el piloto y qué umbrales justificarían ampliar, corregir o detenerlo? **Responsables:** Producto y Analítica. **Revisar antes de iniciar el piloto.**
11. **Operación del lanzamiento:** ¿qué disponibilidad, canal de soporte, horario y objetivo de respuesta se comprometerán, incluida la intervención administrativa después de los 20 minutos? **Responsable:** Operaciones. **Revisar antes de habilitar tareas reales.**
12. **Monetización futura:** ¿se mantendrá el producto sin cobro, se explorará otro modelo o se financiará de otra manera después del lanzamiento inicial? **Responsables:** Producto y Finanzas. **Revisar antes de introducir cualquier cobro o monetización.**
13. **Experiencia y accesibilidad:** ¿qué requisitos de accesibilidad debe cumplir la interfaz y qué adaptación al español de Argentina necesita? **Responsable:** UX. **Revisar antes de aprobar el diseño de experiencia.**
## 9. Índice de supuestos

- §4.6 FR-14 — **[ASSUMPTION]** bloquear impide nuevas interacciones entre ambas personas; faltan confirmar el alcance y el efecto sobre changas asignadas.
- §5 Idioma — **[ASSUMPTION]** la interfaz inicial estará en español de Argentina.
