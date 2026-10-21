# Revisión de versiones del stack

**Veredicto:** stack compatible y vigente al 2026-10-19; se precisó la versión exacta del SDK.

## Hallazgo resuelto

- **Baja — .NET SDK/runtime:** la versión 10.0.13 es la del runtime, no la del SDK. El spine ahora separa SDK 10.0.402, runtime 10.0.13 y ASP.NET Core 10.0.13.

## Evidencia verificada

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core): .NET 10 LTS y runtime 10.0.13 vigente.
- [Notas de .NET 10.0.13](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.13/10.0.13.md): SDK 10.0.402 y ASP.NET Core 10.0.13.
- [Novedades de EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew): EF Core 10 LTS requiere .NET 10.
- [Historial de versiones EF Core](https://learn.microsoft.com/en-us/ef/core/what-is-new/): EF Core 10.0.13 es el parche vigente revisado.

El supuesto de usar .NET 10 queda pendiente de confirmar contra el entorno exigido por la cátedra.
