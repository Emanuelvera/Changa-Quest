# Revisión rubric-walker — Architecture Spine Changa Quest

**Artefacto revisado:** `ARCHITECTURE-SPINE.md`  
**Insumo de capacidades:** `_bmad-output/specs/spec-changa-quest/SPEC.md`  
**Fecha:** 2026-10-20  
**Veredicto:** **Apto con dos ajustes de trazabilidad y alcance antes de tratarlo como contrato único de implementación.** El spine ya fija las principales fronteras y salvaguardas; no encontré una decisión diferida que por sí sola impida comenzar la demo, siempre que las decisiones reservadas para la inicialización se cierren antes de que trabajen en paralelo las unidades afectadas.

## Resultado por criterio

### Divergencias reales y calidad de las AD

- **AD-1 — Dependencias N-Layers:** cierra una divergencia arquitectónica real: fija un grafo explícito, permite al host obtener el registro mediante Presentation y exige validar automáticamente las referencias. El grafo y la regla son congruentes; dejar el mecanismo de validación para la inicialización no permite cambiar el grafo permitido.
- **AD-2 — SDD/TDD:** fija una fuente común de intención y un ciclo verificable para cambios funcionales. Pedir que se registre el fallo inicial hace el paso Red observable sin imponer una herramienta o formato que no constituye una decisión de arquitectura.
- **AD-3 — Propiedad de reglas y mutaciones:** cierra la posibilidad de que Presentation, DataAccess o varios Managers decidan transiciones por separado. `ChangaManager` como autoridad única de ciclo de vida y asignación es una regla concreta; los detalles de cada transición permanecen en la especificación.
- **AD-4 — Asignación/concurrencia:** aborda un riesgo real y no obvio, y requiere estado persistido, confirmación atómica y pruebas concurrentes con el proveedor elegido. Sin embargo, el ámbito declarado en `Binds` (FR-7 y FR-8) es más estrecho que la regla, que cubre todas las transiciones incompatibles del ciclo de vida. Ver **R-1**.
- **AD-5 — Privacidad:** previene exposición por proyecciones, contratos o consultas, y extiende correctamente el control a direcciones y teléfonos. Exige revalidación en cada consulta sensible, no solo ocultamiento en la interfaz.
- **AD-6 — Errores HTTP:** una forma uniforme para todas las rutas públicas cierra una fuente real de incompatibilidad entre clientes. El uso de Problem Details, la distinción entre errores de negocio y técnicos y la protección de información interna son reglas suficientes a esta altura.

En conjunto, las AD describen decisiones de arquitectura y no una lista de tecnologías por sí sola. No observé una AD obviamente superflua; el principal ajuste es hacer coincidir el alcance declarado de AD-4 con el alcance efectivo de su regla.

### Cobertura de capacidades del SPEC

El contenido funcional está representado sustantivamente en el mapa FR y en las reglas: CAP-1 (publicar) corresponde a FR-1..FR-4/AD-2..AD-4; CAP-2 (búsqueda y ubicación) a FR-5..FR-6/AD-5; CAP-3 (asignación y consentimiento) a FR-7..FR-9/AD-3..AD-5; CAP-4 (ciclo de vida y pagos declarados) a FR-10..FR-13/AD-3; CAP-5 (calificaciones) está agrupada con FR-10..FR-13/AD-3; y CAP-6 (reportes/revisión) a FR-14..FR-15/AD-3. No encontré una capacidad funcional claramente ausente del mapa.

No obstante, el SPEC usa identificadores **CAP-1..CAP-6** y el spine solo declara `binds: FR-1..FR-15` y agrupa FR en el mapa. Por ello, el lector no puede verificar directamente la cobertura ni distinguir con rapidez las capacidades del producto de los límites propios de la demo descritos en SPEC. La cobertura es inferible, no auditable por referencia directa. Ver **R-2**.

La política de la demo sí aparece parcialmente —entorno local con datos ficticios, no habilitar trabajo real, y exclusiones administrativas—, pero el mapa no marca explícitamente que la demo usa asignación por orden de llegada y solo permite cancelación previa a la asignación, mientras que algunas capacidades del producto contemplan selección entre postulaciones y flujos posteriores. AD-2 remite a criterios documentados y reduce el riesgo, pero una referencia explícita al alcance CAP del SPEC evitaría que el mapa se leyera como alcance ejecutable de la demo.

### Diferidos y riesgo de decisiones incompatibles

Los diferidos de entidades/modelos, endpoints, límites finos de Managers/Stores, motor del producto real, identidad de producción, CI y operaciones de lanzamiento son decisiones razonables para esta altitud. No son invariantes que el código no pueda revelar, y el documento fija condiciones de cierre: inicializar la solución o resolver antes del piloto real.

Para la demo, SQL Server LocalDB/Express, .NET/EF Core, Blazor WebAssembly y el límite API REST están fijados. La forma de alojar/entregar los assets del cliente y la instancia local todavía permite más de una topología, pero `Deferred` exige resolverla al inicializar la solución y preservar la frontera REST. Considero esto un diferimiento aceptable **si** se decide antes de que los equipos del cliente y del API trabajen de forma independiente; no requiere fijar ahora el servidor estático ni la topología de producción.

### Dimensiones operacionales y ambientales

La envolvente operacional no está omitida: el seed define demo local y datos ficticios, identifica cliente/API/base de datos, y bloquea el uso real hasta resolver condiciones legales, seguridad, confianza y operación. El proveedor real, hosting, configuración por entorno, secretos, copias de seguridad, monitoreo, disponibilidad y soporte están explícitamente diferidos hasta antes de un piloto. La división entre decisiones de demo y de lanzamiento real es apropiada para la altitud iniciativa. La única condición práctica señalada arriba es cerrar la integración local cliente/API al inicializar la solución.

### Stack y verificación mecánica

El lint de `lint_spine.py` terminó con **0 hallazgos**. Las versiones citadas son congruentes con las fuentes consultadas al 2026-10-20: .NET 10.0.13 y SDK 10.0.402 aparecen en las notas oficiales de la publicación del 13 de octubre; EF Core 10 está documentado como LTS; NuGet publica `Microsoft.EntityFrameworkCore.SqlServer` 10.0.13 y `xunit.v3` 4.0.2, que declara soporte para .NET 8 o posterior. No detecté una tecnología nombrada que esté sin verificar o sea incompatible con el stack declarado.

Fuentes comprobadas:

- [.NET 10.0.13 release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.13/10.0.13.md)
- [EF Core 10 — What's New](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
- [EF Core SQL Server 10.0.13](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.13)
- [xunit.v3 4.0.2](https://www.nuget.org/packages/xunit.v3/4.0.2)

## Hallazgos

### R-1 — El ámbito de AD-4 no coincide con su regla

**Gravedad: Media**  
**Ubicación:** `ARCHITECTURE-SPINE.md`, AD-4 (líneas 74–78); mapa de capacidades (líneas 151–156).

`Binds` limita AD-4 a FR-7 y FR-8, pero la regla exige atomicidad para **toda** transición incompatible del ciclo de vida de una changa. En el mapa, FR-10..FR-13 quedan gobernados por AD-2 y AD-3, no AD-4. Dos unidades podrían interpretar de forma distinta si la regla de concurrencia abarca cancelación, finalización, cierre o decisión administrativa, aunque el texto de la regla parezca incluirlas.

**Ajuste recomendado:** o bien ampliar `Binds` y las referencias del mapa a los FR de ciclo de vida afectados, o bien limitar la regla de AD-4 a asignación/toma/selección concurrente. Mantener explícito el requisito de que las transiciones incompatibles no puedan sobrescribirse si esa cobertura más amplia es intencional.

### R-2 — El mapa no enlaza directamente las capacidades CAP del SPEC

**Gravedad: Baja**  
**Ubicación:** `ARCHITECTURE-SPINE.md`, frontmatter `binds` y `Capability → Architecture Map` (líneas 8–10, 147–156); `SPEC.md`, sección Capabilities.

El mapa cubre funcionalmente las seis capacidades, pero solo lista rangos FR. Al no citar CAP-1..CAP-6 ni establecer el enlace CAP↔FR, un lector debe reconstruirlo por títulos y no puede auditar la cobertura contra el SPEC, en particular la diferencia entre el producto objetivo y la demo académica.

**Ajuste recomendado:** añadir CAP-1..CAP-6 al mapa o incluir el identificador CAP junto al rango FR correspondiente, preservando el detalle FR si aporta trazabilidad. Indicar también qué alcance de cada capacidad aplica a la demo y cuál queda en el producto objetivo.

## Conclusión

El spine fija decisiones útiles y verificables para capas, propiedad de reglas, concurrencia, privacidad y errores; los diferidos operacionales están delimitados por hitos y no constituyen una omisión de dimensión. Antes de usarlo como contrato de implementación, alinear el ámbito de AD-4 y hacer directa la trazabilidad hacia CAP-1..CAP-6. No se modificó `ARCHITECTURE-SPINE.md`.
