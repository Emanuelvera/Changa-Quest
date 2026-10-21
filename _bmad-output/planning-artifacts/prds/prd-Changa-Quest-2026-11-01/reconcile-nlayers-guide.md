# Reconciliación técnica: guía N-Layers

**Fuente inspeccionada:** `Guia-API-Rest-N-Layers.pdf` (23 páginas; texto legible). Se contrastó con el PRD y `addendum.md`. No se modificaron esos documentos.

## Diferencias importantes

1. **Capas y proyectos:** la guía define cuatro capas principales —Presentation, BusinessLogic, DataAccess y Entities—. `Core` es soporte transversal y el proyecto API/Backend es el host, no una quinta capa. El addendum distorsiona la estructura al decir que son cinco capas e incluir Core.
2. **Dirección de dependencias:** no está recogida la regla arquitectónica explícita: DataAccess → Entities; BusinessLogic → DataAccess + Entities; Presentation → BusinessLogic + Entities; API → Presentation. La guía prohíbe las dependencias inversas indicadas en sus páginas 6–8 para evitar acoplamiento circular.
3. **Tecnología descrita:** la guía trata una API REST en C#/.NET, con host ASP.NET Core Web API y Entity Framework Core (`AppDbContext`). El addendum solo fija .NET y Entity Framework genérico, y difiere el framework web. Si la guía es normativa para el trabajo, esa omisión y la diferencia de especificidad deben resolverse; no asumiría que cualquier framework web cumple. La configuración con SQL Server aparece como ejemplo, no como motor obligatorio.
4. **Límites entre capas:** faltan reglas de diseño relevantes: controllers HTTP simples, sin lógica de negocio compleja; Inputs/Outputs separados de las Entities para no exponer directamente modelos internos; contratos Store/Manager, con Managers coordinando reglas y acceso a datos, y configuración de dependencias por DI. La guía presenta estas responsabilidades como parte de la estructura propuesta.

La guía respalda la separación de responsabilidades y el uso de EF, pero no fija un proveedor de base de datos. Conviene corregir la descripción de capas y decidir qué elementos concretos de esta guía son requisitos académicos vinculantes antes de cerrar arquitectura.
