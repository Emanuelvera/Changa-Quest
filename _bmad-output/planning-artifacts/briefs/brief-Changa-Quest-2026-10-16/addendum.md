# Anexo: insumos para diseño y arquitectura

## Restricción académica

Changa Quest es el trabajo final de Programación Aplicada 2. La implementación debe usar .NET y Entity Framework y seguir la guía `Guia-API-Rest-N-Layers.pdf`, proporcionada como material local y no incluida en este repositorio.

La guía describe una solución con una API REST y proyectos separados por responsabilidad:

- **Presentación:** recibe solicitudes del cliente, interpreta sus datos y expone controladores y endpoints.
- **Lógica de Negocio:** aplica validaciones y reglas de negocio; puede ofrecer servicios para integraciones externas.
- **Acceso a Datos:** consulta la base de datos y prepara los datos de respuesta.
- **Entidades:** contiene entidades y elementos compartidos entre capas.
- **Core:** contiene funciones y bibliotecas compartidas de las que dependen las capas.

El objetivo es separar responsabilidades, reducir el acoplamiento y facilitar el mantenimiento y la evolución. Los nombres de proyectos, la dirección de dependencias y las decisiones concretas de implementación corresponden al trabajo de arquitectura.

## Implicaciones para el diseño posterior

- La asignación por orden de llegada debe impedir que dos personas queden asignadas a la misma changa, incluso ante intentos simultáneos. La solución técnica queda para arquitectura.
- La dirección exacta de una changa solo se muestra a la persona asignada; antes se muestra el barrio o zona y una distancia aproximada.
- La ubicación GPS del trabajador se solicita bajo acción explícita y solo se usa para esa búsqueda, sin conservarla ni hacer seguimiento. Una dirección ingresada manualmente puede guardarse como preferencia.
- El límite del radio, la precisión de la zona aproximada y el dato de contacto que se comparte tras la asignación se especificarán en el diseño UX/PRD.
- Si falta una confirmación o existe un desacuerdo, la changa queda pendiente; el MVP no incluye mediación ni reportes administrativos.

## Límites de la evidencia

El impacto esperado —resolver necesidades cotidianas y generar ingresos locales— aún no está validado mediante investigación con usuarios, pilotos ni métricas.
