- source_spec: `_bmad-output/implementation-artifacts/spec-2-4-calificar-a-la-otra-parte-tras-el-cierre.md`
  summary: Resuelto al integrar `dev`: el estado `Closed` se unificó con `Completed` (Story 2.3) y se agregó la prueba `Rating_opens_only_after_real_close_through_completion_and_both_payment_declarations`, que recorre el flujo real hasta calificar.
  evidence: Ya no se fuerza el estado en esa prueba; las demás de calificación siguen preparando `Completed` directamente por velocidad.
