# Instrucciones para agentes (Changa Quest)

- Idioma de documentos y comunicación: español.
- Metodología obligatoria: SDD orientado a TDD. Leer `docs/METODOLOGIA.md` antes de cambiar código.
- Orden de trabajo: spec de la historia → pruebas en rojo → implementación mínima → refactor. No escribir código de producción sin una prueba que falle antes.
- Fuente de alcance: `_bmad-output/planning-artifacts/epics.md`. Arquitectura: `ARCHITECTURE-SPINE.md` (AD-2, AD-3, AD-4).
- Capas: Entities, DataAccess, BusinessLogic, Presentation, API. Las reglas de negocio solo en BusinessLogic.
- Cambios de API: actualizar primero `specs/api/openapi-v1.yaml`.
- Pruebas: `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` (Microsoft.Testing.Platform; `dotnet test` solo no ejecuta nada).
- No commitear `bin/`, `obj/`, secretos ni configuración personal.
