# Revisión adversarial de invariantes

**Veredicto inicial:** se identificaron divergencias posibles en ownership, transiciones concurrentes, privacidad y formato HTTP.

## Hallazgos y resolución

- **Alta — Ownership del ciclo de vida:** Managers distintos podían mutar el estado de la misma changa. **Resuelto:** AD-3 asigna las mutaciones a `ChangaManager`.
- **Alta — Transiciones que compiten:** asignar y cancelar al mismo tiempo podía crear un estado inconsistente. **Resuelto:** AD-4 exige validación frente al estado persistido y confirmación atómica para transiciones incompatibles.
- **Alta — Consentimiento y privacidad:** no estaban combinadas las condiciones de asignación y consentimiento de ambos teléfonos. **Resuelto:** AD-5 fija las precondiciones y exige validarlas al exponer datos sensibles.
- **Media — Proyección por capa:** DataAccess podía elegir qué información consideraba pública. **Resuelto:** AD-5 asigna esa decisión a BusinessLogic y deja la ejecución de consultas a DataAccess.
- **Media — Contrato de errores:** distintos endpoints podían usar formatos incompatibles. **Resuelto:** AD-6 fija `application/problem+json`.

## Deferred

El proveedor EF, el detalle de las transacciones, el esquema y los estados específicos se derivan de la especificación de ciclo de vida y se validan con pruebas de integración; no se prescriben aquí.
