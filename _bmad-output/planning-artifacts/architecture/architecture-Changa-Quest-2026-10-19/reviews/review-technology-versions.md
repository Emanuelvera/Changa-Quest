# Tecnología y versiones — verificación de arquitectura

**Fecha de verificación:** 2026-10-20  
**Artefacto revisado:** `C:\Users\emanu\OneDrive\Desktop\Changa-Quest\_bmad-output\planning-artifacts\architecture\architecture-Changa-Quest-2026-10-19\ARCHITECTURE-SPINE.md`  
**Alcance:** verificación de las tecnologías y versiones declaradas, sus compatibilidades y las reservas pendientes. La arquitectura spine no se modificó durante la revisión.

## Veredicto

**Aprobado con verificaciones de implementación pendientes.** No se encontró una incompatibilidad ni un emparejamiento no admitido entre las versiones especificadas de .NET, ASP.NET Core, EF Core, el proveedor SQL Server de EF Core, Blazor WebAssembly y xUnit.net. La elección del motor SQL Server local —incluidas la versión e instancia de LocalDB/Express— y la configuración del runner de pruebas permanecen sin especificar, tal como indica la arquitectura.

## Verificación por tecnología

| Tecnología declarada | Evidencia consultada | Resultado |
|---|---|---|
| **.NET SDK 10.0.402** | Las notas oficiales de .NET 10.0.13 identifican SDK 10.0.402 como el SDK que incluye los runtimes 10.0.13. | **Verificado.** Coincide con la versión declarada. |
| **.NET runtime 10.0.13 LTS** | Las notas oficiales identifican el runtime 10.0.13; la política de soporte de Microsoft clasifica .NET 10 como LTS y señala soporte hasta noviembre de 2028. | **Verificado.** Versión y condición LTS correctas para la fecha de revisión. |
| **ASP.NET Core Web API 10.0.13** | Las notas oficiales de .NET 10.0.13 incluyen ASP.NET Core Runtime y paquetes de ASP.NET Core en la versión 10.0.13. | **Verificado.** Emparejamiento coherente con el runtime .NET 10.0.13. |
| **Entity Framework Core 10.0.13 LTS** | Microsoft Learn indica que EF Core 10 es LTS, se publicó en noviembre de 2025 y recibe soporte hasta el 10 de noviembre de 2028. También requiere el SDK y runtime de .NET 10. NuGet publica el paquete `Microsoft.EntityFrameworkCore` 10.0.13 para `net10.0`. | **Verificado.** Encaja con el SDK/runtime .NET 10 indicado. |
| **EF Core SQL Server provider 10.0.13** | NuGet publica `Microsoft.EntityFrameworkCore.SqlServer` 10.0.13. Su metadata especifica dependencias EF Core 10.0.13 y un grupo `net10.0`. Microsoft Learn confirma que es el proveedor EF Core para SQL Server. | **Verificado.** La versión del proveedor coincide con EF Core y su plataforma objetivo es .NET 10. |
| **Blazor WebAssembly — ASP.NET Core 10.0.13** | NuGet publica `Microsoft.AspNetCore.Components.WebAssembly` 10.0.13 para `net10.0`; Microsoft Learn documenta Blazor WebAssembly como UI cliente ejecutada en el navegador mediante WebAssembly. | **Verificado.** La versión del paquete y el objetivo .NET son coherentes con la pila .NET 10. |
| **xUnit.net 4.0.2 (xUnit v3)** | Las notas oficiales de xUnit describen la versión 4.0.2 como una versión del framework Core v3, publicada el 9 de octubre de 2026. La ficha NuGet de `xunit.v3` 4.0.2 indica soporte para .NET 8 o posterior, lo que incluye .NET 10. | **Verificado con aclaración de paquete.** La versión corresponde a xUnit v3 y es compatible con .NET 10; el paquete que debe quedar explícito al implementar es `xunit.v3`. |

## Evidencia de compatibilidad

- **SDK, runtime y ASP.NET Core:** las notas oficiales de .NET 10.0.13 informan que el SDK 10.0.402 incluye los runtimes .NET y ASP.NET Core 10.0.13. Esto respalda la combinación declarada sin exigir una versión de runtime distinta.
- **EF Core y .NET:** Microsoft Learn especifica que EF Core 10 requiere .NET 10 SDK para compilar y .NET 10 runtime para ejecutarse; no funciona sobre versiones anteriores de .NET. La arquitectura usa .NET 10.
- **Proveedor SQL Server y EF Core:** la metadata de NuGet confirma que el paquete SQL Server 10.0.13 depende de componentes EF Core 10.0.13 y está dirigido a `net10.0`; coincide con la versión EF Core seleccionada.
- **Blazor WebAssembly y .NET:** la metadata de `Microsoft.AspNetCore.Components.WebAssembly` 10.0.13 declara un grupo de dependencias `net10.0` y componentes ASP.NET Core 10.0.13.
- **xUnit y .NET:** la ficha NuGet de `xunit.v3` 4.0.2 declara soporte para .NET 8 o posterior; .NET 10 satisface ese requisito.

## Hallazgos

1. **Sin incompatibilidad detectada — Informativo.** Los números de versión y las plataformas objetivo revisados son coherentes entre .NET 10.0.13, SDK 10.0.402, ASP.NET Core 10.0.13, EF Core 10.0.13, el proveedor SQL Server 10.0.13, Blazor WebAssembly 10.0.13 y xUnit v3 4.0.2.
2. **Nombre del paquete xUnit debe precisarse — Bajo.** La arquitectura declara “xUnit.net 4.0.2 (xUnit v3)”, pero no fija el identificador NuGet. La ficha verificable de esta versión corresponde a `xunit.v3` 4.0.2. Se recomienda fijar ese ID en la configuración de pruebas para evitar ambigüedad al inicializar la solución.
3. **Instancia y versión de SQL Server pendientes — Medio.** La arquitectura menciona SQL Server LocalDB/Express local, pero deja pendiente verificar la instancia disponible y su compatibilidad al inicializar la solución. La versión del proveedor EF está verificada; eso no verifica la versión ni disponibilidad del motor SQL Server instalado.
4. **Runner y host de pruebas pendientes — Bajo.** La arquitectura deja la configuración de runner xUnit, el host HTTP de aceptación y el pipeline CI para la inicialización. No se verificó aquí una combinación concreta de esos componentes porque no se especifican versiones.

## Recomendaciones

- Mantener las versiones verificadas y fijar, al crear los proyectos, `TargetFramework` en `net10.0` donde corresponda.
- Registrar explícitamente el paquete **`xunit.v3` 4.0.2** en el proyecto de pruebas, junto con el runner/adaptador que se elija; verificar su integración con el SDK y la infraestructura CI seleccionados.
- Al inicializar la solución, comprobar la instalación y versión efectiva de LocalDB/Express y ejecutar pruebas de integración contra ese motor. No considerar un proveedor InMemory como evidencia de compatibilidad o persistencia SQL Server.
- Revalidar las versiones parcheadas y el estado de soporte antes de implementar o desplegar, ya que las versiones de servicio pueden cambiar después de la fecha de esta revisión.

## Fuentes oficiales consultadas

1. Microsoft, **.NET 10.0.13 release notes** — SDK 10.0.402, runtimes .NET/ASP.NET Core 10.0.13 y lista de paquetes:  
   https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.13/10.0.13.md
2. Microsoft Learn, **.NET releases, patches, and support** — versiones soportadas y periodo LTS de .NET 10:  
   https://learn.microsoft.com/en-us/dotnet/core/releases-and-support
3. Microsoft, **.NET and .NET Core support policy**:  
   https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
4. Microsoft Learn, **What’s new in EF Core 10** — LTS, periodo de soporte y requisitos de .NET 10:  
   https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew
5. NuGet, **Microsoft.EntityFrameworkCore 10.0.13 metadata**:  
   https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore/10.0.13/microsoft.entityframeworkcore.nuspec
6. NuGet, **Microsoft.EntityFrameworkCore.SqlServer 10.0.13 metadata** — dependencias y objetivo `net10.0`:  
   https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlserver/10.0.13/microsoft.entityframeworkcore.sqlserver.nuspec
7. Microsoft Learn, **Microsoft SQL Server Database Provider — EF Core**:  
   https://learn.microsoft.com/en-us/ef/core/providers/sql-server/
8. NuGet, **Microsoft.AspNetCore.Components.WebAssembly 10.0.13 metadata**:  
   https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.components.webassembly/10.0.13/microsoft.aspnetcore.components.webassembly.nuspec
9. Microsoft Learn, **ASP.NET Core Blazor**:  
   https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-10.0
10. xUnit.net, **Core Framework v3 4.0.2 release notes**:  
    https://xunit.net/releases/v3/4.0.2
11. NuGet, **xunit.v3 4.0.2** — soporte de .NET 8 o posterior:  
    https://www.nuget.org/packages/xunit.v3/4.0.2

## Referencia al documento revisado

Las versiones aparecen en la tabla de Stack, líneas 103–113; las verificaciones de LocalDB/Express y runner/CI están diferidas en las líneas 158–163 de `ARCHITECTURE-SPINE.md`.

**Original review status:** The reviewer could not write the file; this preserved report is based on its complete returned review.
