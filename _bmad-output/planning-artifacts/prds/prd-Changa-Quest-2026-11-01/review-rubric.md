# PRD Quality Review — Changa Quest

## Overall verdict

El PRD presenta un producto coherente y una demo académica delimitada, con requisitos en general verificables y una representación honesta de lo que todavía no se sabe. No está listo para autorizar un lanzamiento real: siguen sin resolverse condiciones legales, de confianza, seguridad operativa y liquidez local, y varios flujos pueden quedar indefinidamente pendientes. La separación explícita entre demo y operación real es una fortaleza importante, siempre que se mantenga como una barrera efectiva.

## Decision-readiness — adequate

Un equipo puede avanzar con la demo: §7.2 delimita un recorrido principal y una rama de postulaciones, usa datos de ejemplo y excluye pagos reales. En cambio, el PRD no respalda todavía un sí/no de lanzamiento: §7.1 indica que «El lanzamiento queda condicionado a la revisión legal indicada en §5, a la definición de controles de confianza proporcionales, al procedimiento de reportes/revisión y a la validación de seguridad y demanda», pero no especifica evidencia de aprobación, criterios de salida ni responsables/fechas para cerrar esos bloqueos. La sección §9 los reconoce como preguntas, lo que evita presentarlos como decisiones ya tomadas.

### Findings
- **[high]** Convertir las condiciones de lanzamiento en una puerta de decisión verificable (§§5, 7.1, 9) — El veto legal y otros requisitos previos son explícitos, pero el documento no dice qué evidencia los satisface ni quién acepta el riesgo residual. *Fix:* definir criterios de aprobación/no aprobación para cada condición (incluidos asesoramiento legal, confianza/identidad, seguridad y moderación), responsables y evidencia requerida antes de habilitar trabajos reales.

## Substance over theater — strong

El contenido está ganado por las decisiones del producto: §1 fija dos resultados sociales como hipótesis y aclara que no hay entrevistas ni adopción demostrada; §2 contiene dos recorridos que sí impulsan requisitos como información de materiales, ubicación protegida y cierre con declaraciones separadas. El anexo caracteriza Clickie como «un comparable adyacente» y dice expresamente que no prueba demanda para changas vecinales; no vende novedad ni investigación como validación. Tampoco hay una lista de personas decorativas o NFRs genéricos presentados como especificaciones completas. No encuentro hallazgos sustantivos en esta dimensión.

## Strategic coherence — adequate

Hay una tesis reconocible: conectar necesidades cotidianas de bajo riesgo con ingresos locales, mediante publicación, asignación, coordinación consentida y cierre sin intermediación de pago (§§1, 4, 6). Los requisitos y los indicadores SM-2/SM-C2 guardan relación con esa tesis; SM-C1, además, evita tratar menos reportes como señal automática de mayor seguridad. La limitación es que los indicadores no permiten saber si el mercado local logra suficiente liquidez o si la propuesta resuelve necesidades de forma consistente: §8 deja activos y cantidades completadas sin umbrales, y §7.1 reconoce que no están definidos los barrios ni la densidad de oferta/demanda.

### Findings
- **[high]** Añadir señales de liquidez y validación del mercado local (§§7.1, 8, 9) — Las métricas cuentan personas activas, changas con declaraciones y recompensas declaradas, pero no distinguen si hay oferta y demanda suficientes por zona ni cuánto tarda una solicitud en encontrar una persona realizadora. Esto deja sin prueba central la viabilidad de un marketplace en Córdoba. *Fix:* incluir métricas de embudo y liquidez por zona (por ejemplo, solicitudes con postulaciones/asignación, tiempo hasta asignación, proporción sin resolver y repetición por rol) y definir cómo el piloto informará la decisión de ampliar, corregir o detener.

## Done-ness clarity — adequate

Los FR-1 a FR-15 tienen consecuencias comprobables en su mayoría; son particularmente claras las reglas de asignación única y de no revelar ubicación o teléfonos antes de cumplirse las condiciones (§§4.1–4.6). La claridad baja en las salidas de error y desacuerdo: FR-10 deja el trabajo «pendiente, sin reasignación ni resolución automática» cuando se rechaza; FR-11 lo deja pendiente si falta una declaración o hay desacuerdo; FR-13 remite inactividad y casos sin solución a OQ-6. Esos estados son comprensibles como límites del producto, pero no bastan para saber qué ocurre operativamente con una asignación abierta en un lanzamiento real.

### Findings
- **[high]** Especificar el ciclo de vida de changas sin resolución (§§4.5–4.6, OQ-6) — FR-10, FR-11 y FR-13 permiten estados asignados o pendientes sin plazo, expiración, salida unilateral ni responsable de seguimiento. En un mercado de tareas reales eso puede dejar a ambas partes bloqueadas y generar riesgo de seguridad o soporte. *Fix:* definir estados, plazos y acciones para rechazo, inactividad, ausencia, falta de consentimiento y desacuerdo; si no habrá mediación, precisar cómo se cancela o libera la asignación y qué información/ayuda se ofrece.

## Scope honesty — strong

La distinción de alcance es explícita y útil: §7.1 describe el producto objetivo, §7.2 acota la demo local y §7.3 lista exclusiones; §6 niega expresamente que el éxito académico valide demanda, seguridad o impacto. Las inferencias están marcadas como `[ASSUMPTION]`, las preguntas abiertas están separadas de los compromisos y el documento no presenta monetización ni obligaciones legales como resueltas. La cantidad de preguntas abiertas es alta para un producto de este riesgo, pero está declarada y el lanzamiento está condicionado a resolver varias; no se detecta una omisión silenciosa sustantiva.

## Downstream usability — adequate

La terminología central y la trazabilidad son sólidas: hay glosario, FR-1–FR-15 y SM-1–SM-3/SM-C1–SM-C2 identificables, con referencias que en general resuelven (§§3–4, 8). Los dos UJ tienen protagonistas y contexto; además, la limitación de recorrido está reconocida en §2: «El segundo modo está definido en §4, pero aún no tiene un recorrido narrado por una persona usuaria». UX y arquitectura pueden extraer la mayor parte del flujo, pero esta rama importante del marketplace no tiene todavía el mismo nivel de contexto que la asignación por orden de llegada.

### Findings
- **[medium]** Añadir un recorrido para selección entre postulaciones (§§2.3, 4.2, 4.4) — FR-4 y FR-8 especifican postulaciones, selección y aviso a quienes no fueron elegidos, pero los únicos UJ narran la modalidad por orden de llegada. Falta contexto para diseñar la revisión/elección y el resultado para ambas partes. *Fix:* incorporar un UJ breve de publicación en modo selección, evaluación/asignación, notificación de no selección y coordinación posterior.

## Shape fit — strong

El formato encaja con un marketplace de consumo de dos roles y con una entrega que alimenta UX, arquitectura e historias: las jornadas con protagonistas son pertinentes, hay requisitos por capacidad, métricas de resultado y contramétricas, y el producto real está separado de una demo académica. La demo no se confunde con un piloto ni con permiso para usar datos o realizar trabajos reales (§7.2 y anexo §1). No se observan sobreformalización de un rol único ni ausencia de estructura para un producto con interacción entre partes.

## Mechanical notes

- **Glosario y referencias:** sin deriva terminológica ni referencias FR/UJ/SM rotas que afecten el uso posterior.
- **IDs:** FR-1 a FR-15 y UJ-1 a UJ-2 son únicos y continuos; SM-1 a SM-3 y SM-C1 a SM-C2 son distinguibles.
- **Índice de supuestos:** los tres `[ASSUMPTION]` en §§4.6, 5 y 7.2 están representados en §10; las entradas del índice corresponden a supuestos en línea.
- **Protagonistas UJ:** ambos recorridos nombran a sus protagonistas y aportan contexto; el hueco de la rama de postulaciones se detalla en Downstream usability.
- **[low] Fechas/procedencia:** los metadatos del PRD indican `created: 2026-11-01` y el anexo §3 afirma que las páginas se consultaron «el 1 de noviembre de 2026». Ambas fechas son posteriores a la fecha de esta revisión (19 de octubre de 2026), por lo que la procedencia no puede corroborarse aún. *Fix:* verificar y corregir las fechas o aclarar su carácter previsto antes de tratar la investigación como evidencia ya obtenida.
