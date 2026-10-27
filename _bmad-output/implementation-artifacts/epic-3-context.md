# Epic 3 Context: Confianza, reportes y bloqueo

## Alcance

Epic 3 cubre capacidades del producto real para reportar interacciones, registrar bloqueos y permitir revisión administrativa. Estas capacidades están fuera de la demo académica y no deben presentar datos ficticios como controles operativos reales.

## Historia anterior

Story 3.1 registra reportes autenticados con motivo obligatorio y evidencia opcional. Los reportes quedan pendientes y su lectura se restringe a cuentas administradoras. La implementación no aplica sanciones ni bloqueos.

## Story 3.2

La historia registra una relación de bloqueo entre dos cuentas después de una confirmación explícita. El bloqueo no cancela ni resuelve changas asignadas. El alcance efectivo del bloqueo —qué interacciones impide y cómo se comporta frente a asignaciones existentes— permanece pendiente de PRD OQ-5.

## Restricciones de arquitectura

- BusinessLogic decide autorización, idempotencia y reglas de bloqueo.
- DataAccess persiste la relación y sus restricciones de integridad.
- Presentation expone Problem Details con códigos estables.
- El contrato OpenAPI se actualiza antes de API y cliente.
- La UX debe explicar el límite sobre changas asignadas antes de confirmar.

## Fuera de alcance

- Aplicar el bloqueo para impedir mensajes, postulaciones, toma, coordinación u otras interacciones.
- Cancelar, liberar, reasignar o modificar changas asignadas.
- Desbloquear cuentas.
- Revisión administrativa, sanciones, apelaciones o notificaciones.
