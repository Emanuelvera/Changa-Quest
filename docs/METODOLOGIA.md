# Metodología de desarrollo: SDD orientado a TDD

La verdad del proyecto vive en los documentos, no en el código. Esta guía resume cómo trabaja todo el equipo. La decisión formal es **AD-2** en `_bmad-output/planning-artifacts/architecture/architecture-Changa-Quest-2026-10-19/ARCHITECTURE-SPINE.md`.

## Principios

1. **La especificación manda.** Cada cambio parte de una especificación versionada con criterios de aceptación (Given/When/Then) vinculados a los FR del PRD.
2. **Primero las pruebas.** Se escriben pruebas derivadas de los criterios y se comprueba que fallan (**Red**).
3. **Implementar lo mínimo** para que pasen (**Green**) y después **refactorizar** sin romper la suite (**Refactor**).
4. **El código no redefine requisitos.** Si especificación y prueba discrepan, se corrige primero la especificación y sus pruebas, y recién después el código.
5. **La suite completa debe pasar** antes de aceptar un cambio.

## Fuentes de verdad

| Documento | Ruta |
|---|---|
| Brief y PRD | `_bmad-output/planning-artifacts/briefs/`, `prds/` |
| UX | `_bmad-output/planning-artifacts/ux-designs/` |
| Épicas e historias (alcance de la demo, 17 historias) | `_bmad-output/planning-artifacts/epics.md` |
| Arquitectura y reglas técnicas | `_bmad-output/planning-artifacts/architecture/.../ARCHITECTURE-SPINE.md` |
| Contrato de API | `specs/api/openapi-v1.yaml` |
| Spec por historia | `_bmad-output/implementation-artifacts/spec-<epica>-<historia>-*.md` |
| Estado del sprint | `_bmad-output/implementation-artifacts/sprint-status.yaml` |

## Flujo por historia

1. **Tomar la historia** en Jira y marcarla en curso. Una persona por historia.
2. **Rama propia** desde `master`: `story/<epica>-<historia>-<slug>` (por ejemplo `story/1-2-publicar-changa`).
3. **Spec:** generar o completar `spec-<epica>-<historia>-*.md` desde `epics.md` con BMAD (`bmad-build`). Debe quedar `ready-for-dev` sin dudas abiertas.
4. **Red:** escribir las pruebas (unidad, integración, aceptación y contrato según corresponda) y ejecutarlas para ver que fallan por el motivo correcto.
5. **Green:** implementar respetando las capas.
6. **Refactor** con la suite en verde.
7. **Contrato:** si cambia la API, actualizar `specs/api/openapi-v1.yaml` **antes** que API y cliente.
8. **Revisión:** revisión de código (`bmad-review`) y corrección de hallazgos.
9. **Pull Request** a `master` con la spec, las pruebas y el código juntos. Actualizar `sprint-status.yaml` y el ticket de Jira.

## Definición de hecho

- Criterios de aceptación cubiertos por pruebas escritas antes del código.
- Suite completa en verde.
- OpenAPI y cliente alineados con el comportamiento real.
- Reglas de capas respetadas.
- Spec y `sprint-status.yaml` actualizados.
- Revisión aprobada.

## Reglas de capas (resumen de AD-3 y AD-4)

- **BusinessLogic** es la única capa que decide reglas y transiciones. `ChangaManager` es dueño del ciclo de vida de la changa y su asignación.
- **Presentation** valida el contrato HTTP y delega. **API** solo registra dependencias.
- **DataAccess** contiene `DbContext` y consultas EF Core; ejecuta lo que piden los Managers y no decide negocio.
- La asignación es única aun con concurrencia; los errores de la API usan `application/problem+json` con códigos estables.

## Ejecutar las pruebas

El runner es Microsoft.Testing.Platform (ver `global.json`). `dotnet test` a secas no ejecuta pruebas en este repositorio. Usar:

```powershell
dotnet build
dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"
```

## Alcance de la demo (no construir)

Sin pago real, sin GPS obligatorio, sin comparación entre postulaciones por defecto, sin panel administrativo completo y sin envío real de correos (en Development el enlace de verificación se imprime en la consola del backend). Ante dudas de alcance, manda `epics.md`.

## Trabajar con BMAD

Cada integrante usa su propio BMAD sobre `_bmad/` y `.agents/skills/` del repositorio. Antes de empezar una historia, ejecutar `bmad-help` para ver el siguiente paso y no saltarse las fases de spec y pruebas.
