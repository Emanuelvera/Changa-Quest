# Changa Quest

Tablón vecinal de changas con recompensa: una persona publica una tarea pequeña y otra la cumple. Proyecto final de Programación Aplicada 2 (.NET 10, ASP.NET Core, EF Core, SQL Server LocalDB/Express, Blazor WebAssembly, xUnit v3), con arquitectura N-Layer.

## Empezar

1. Leer [docs/METODOLOGIA.md](docs/METODOLOGIA.md) (SDD orientado a TDD) y [AGENTS.md](AGENTS.md).
2. Alcance y backlog: [epics.md](_bmad-output/planning-artifacts/epics.md) (3 épicas, 17 historias).
3. Compilar y probar:

```powershell
dotnet build
dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"
```

## Estructura

- `src/layers/`: Entities, DataAccess, BusinessLogic, Presentation.
- `src/backend/ChangaQuest.Api/` y `src/client/ChangaQuest.Client/`: API y cliente Blazor.
- `tests/`: pruebas de unidad, integración y aceptación.
- `specs/api/openapi-v1.yaml`: contrato de la API.
- `_bmad-output/`: documentos de planificación y especificaciones (fuente de verdad).
