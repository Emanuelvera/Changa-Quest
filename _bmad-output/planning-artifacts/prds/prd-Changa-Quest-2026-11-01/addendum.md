# Anexo del PRD: Changa Quest

Este anexo conserva detalles técnicos, decisiones de contexto y fuentes útiles para UX, arquitectura y revisión del lanzamiento. No reemplaza los requisitos ni convierte hipótesis en hechos validados.

## 1. Restricciones académicas y técnicas

- El trabajo final de Programación Aplicada 2 debe desarrollarse con .NET y Entity Framework.
- La guía académica indicada por el equipo es `C:\Users\emanu\Downloads\Guia-API-Rest-N-Layers.pdf`. El PDF está en el equipo de Ema y no está incluido en el repositorio; debe compartirse con quienes implementen o definan la arquitectura.
- Los ejemplos de la guía usan ASP.NET Core Web API y Entity Framework Core. El documento describe las responsabilidades de presentación y controladores, DTOs, lógica de negocio (Stores/Managers), acceso a datos y entidades, además de inyección y dirección de dependencias.
- `Core` aparece como componente compartido y no debe tratarse como una quinta capa. La separación y sus responsabilidades deben seguir la guía; la extracción detallada de dependencias y límites corresponde a arquitectura.
- El PRD no decide nombres de proyectos, motor de base de datos, despliegue ni estrategia de persistencia.
- La primera demostración debe ejecutarse en un servidor local. El producto objetivo es una aplicación web adaptable, mobile-first; no una aplicación móvil nativa en su primera etapa.
- La demostración académica no equivale a autorización para operar con personas, tareas, ubicaciones o pagos reales.

## 2. Navegación de decisiones

Las decisiones de producto y sus requisitos canónicos están en el [PRD](./prd.md), especialmente en usuarios y recorridos, funcionalidades, requisitos transversales, alcance, métricas y preguntas abiertas. Este anexo conserva los detalles técnicos y la evidencia externa que sirven a arquitectura y a la revisión del lanzamiento, evitando mantener una segunda versión de las reglas de negocio.

## 3. Investigación pública para el contexto de lanzamiento

La investigación se limitó a las páginas públicas indicadas como contexto para este borrador. Las descripciones de plataformas son afirmaciones de esas plataformas, no una verificación independiente de disponibilidad, desempeño o seguridad.

### Plataformas y modelos observados

- [Clickie](https://clickie.com.ar/) declara operar en Córdoba Capital y otras ciudades. Su página describe servicios para el hogar, precio cerrado o presupuestos de profesionales, contacto por la plataforma, pago mediante Clickie y una garantía propia. Declara verificar identidad, antecedentes y matrícula cuando aplica. Es un comparable adyacente de servicios profesionales del hogar; la fuente no prueba demanda para changas vecinales de bajo riesgo.
- [Términos de Zolvers](https://zolvers.com/terminos-y-condiciones) describen una plataforma que conecta a personas que demandan y ofrecen servicios, permite buscar y seleccionar perfiles, y afirma no intervenir en las transacciones posteriores. Las condiciones y responsabilidades que la plataforma se atribuye no constituyen asesoramiento jurídico para Changa Quest.
- [Términos de GetNinjas](https://www.getninjas.com.br/termos-de-uso) describen un modelo brasileño de solicitudes de presupuestos y contacto directo entre las partes. Es un comparable de otro mercado y no evidencia de comportamiento de usuarios en Córdoba.

Las páginas consultadas no establecen el volumen local de solicitudes, la densidad de personas realizadoras por barrio, la tasa de finalización, el tiempo de asignación ni la seguridad efectiva de los servicios. No se afirma que los ejemplos anteriores agoten el panorama competitivo ni que el concepto sea único.

### Contexto normativo que requiere revisión profesional

- La página oficial argentina de [Casas particulares](https://www.argentina.gob.ar/trabajo/casasparticulares) incluye limpieza y jardinería entre las tareas del régimen.
- La página oficial para [Empleadores de casas particulares](https://www.argentina.gob.ar/trabajo/casasparticulares/empleadores) indica que empleadores deben registrar a quienes realizan trabajo doméstico o de cuidado, independientemente de las horas, y menciona aportes, obra social y seguro por accidentes de trabajo.

Estas fuentes no determinan la clasificación jurídica de Changa Quest, de la relación entre las partes ni de cada tarea puntual. No son asesoramiento legal. Un análisis profesional de obligaciones laborales, impositivas, de consumo, privacidad y seguridad es una condición previa acordada para operar con usuarios reales.

## 4. Evidencia pendiente

- Entrevistas o pruebas con personas solicitantes y realizadoras en Córdoba.
- Evidencia de demanda y oferta por barrio, frecuencia de las tareas y disposición a usar el producto.
- Comparación directa de flujos y precios de alternativas locales, incluidas recomendaciones informales y grupos comunitarios.
- Validación de tiempos, categorías seguras, controles de identidad, procesos de moderación, soporte y obligaciones jurídicas.
- Revisión de las fuentes públicas al momento de una decisión de lanzamiento, porque sus páginas y condiciones pueden cambiar.
