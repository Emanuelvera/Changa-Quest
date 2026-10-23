---
title: 'Encontrar changas dentro de un radio elegido'
type: 'feature'
created: '2026-10-22'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'C:/Facultad 2026/Programacion aplicada/Project/PLAFinal/Changa-Quest/docs/METODOLOGIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El tablón actual lista todas las changas disponibles sin permitir que
una persona realizadora busque por cercanía ni conocer una distancia aproximada.

**Approach:** Agregar búsqueda manual con coordenadas ficticias de demo y radios
de 1, 3, 5 o 10 km. La API filtrará estrictamente por distancia en línea recta,
devolverá la distancia aproximada y conservará una preferencia solo cuando se
solicite explícitamente; la dirección exacta seguirá fuera de la respuesta.

## Boundaries & Constraints

**Always:** El radio inicial es 3 km; solo se aceptan 1, 3, 5 y 10 km; los
resultados disponibles quedan dentro del radio elegido; no se consulta GPS ni
mapas; la zona y la distancia son aproximadas; la dirección exacta nunca se
expone; el acceso exige cuenta adulta con correo verificado; la preferencia de
ubicación se guarda solo con confirmación explícita.

**Never:** No implementar GPS, mapas, seguimiento, toma de changas, selección
entre postulaciones ni ampliar silenciosamente el radio. No mostrar resultados
ficticios durante la carga ni inventar resultados cuando no existen.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Radio válido | Cuenta elegible, latitud/longitud manuales y 1/3/5/10 km | Lista disponible filtrada y ordenada, con distancia aproximada | N/A |
| Radio omitido | Ubicación válida sin radio | Se usa 3 km | N/A |
| Radio inválido | Radio distinto de 1, 3, 5 o 10 | No se ejecuta la búsqueda | 400 Problem Details |
| Sin coincidencias | Ubicación válida sin changas dentro del radio | Lista vacía, estado vacío explicable en cliente | N/A |
| Preferencia | Ubicación y `savePreference=true` | Se conserva para búsquedas posteriores | 400 si la ubicación es inválida |
| Privacidad | Cualquier resultado | Incluye zona, recompensa, materiales y distancia; no incluye dirección exacta | N/A |

</frozen-after-approval>

## Code Map

- `src/layers/ChangaQuest.Entities/Changas/Changa.cs` -- entidad y políticas de
  changa; agregar coordenadas ficticias de búsqueda.
- `src/layers/ChangaQuest.Entities/Accounts/AppUser.cs` -- identidad; agregar
  preferencia manual opcional.
- `src/layers/ChangaQuest.DataAccess/Accounts/ApplicationDbContext.cs` --
  mapeo EF Core y migración de campos.
- `src/layers/ChangaQuest.DataAccess/Changas/IChangaStore.cs` y
  `ChangaStore.cs` -- consulta de changas disponibles.
- `src/layers/ChangaQuest.BusinessLogic/Changas/ChangaManager.cs` -- reglas de
  radios, distancia, acceso y proyección pública.
- `src/layers/ChangaQuest.Presentation/Changas/ChangaEndpointExtensions.cs` --
  contrato HTTP y Problem Details.
- `src/client/ChangaQuest.Client/Pages/Tareas.razor` -- filtro, estados y
  tarjetas accesibles.
- `specs/api/openapi-v1.yaml` -- contrato canónico de la API.
- `tests/ChangaQuest.UnitTests/Changas/ChangaManagerTests.cs`,
  `tests/ChangaQuest.IntegrationTests/Changas/ChangaPersistenceTests.cs`,
  `tests/ChangaQuest.AcceptanceTests/Changas/ChangaApiTests.cs` -- patrones de
  pruebas existentes.

## Tasks & Acceptance

**Execution:**
- [ ] Actualizar OpenAPI y modelos con coordenadas y búsqueda por radio.
- [ ] Escribir pruebas rojas para radios, distancia, preferencia y privacidad.
- [ ] Implementar persistencia, reglas de negocio, endpoint y cliente.
- [ ] Ejecutar suite, revisar diff y actualizar el estado del sprint.

**Acceptance Criteria:**
- Given una cuenta verificada define una ubicación manual, when elige un radio
  permitido, then solo ve changas disponibles dentro de ese radio.
- Given no se eligió otro radio, when se muestran los controles, then el valor
  inicial es 3 km.
- Given coordenadas ficticias, when se calcula la distancia, then se muestra
  una aproximación en línea recta sin servicios externos.
- Given la persona confirma guardar la ubicación, when vuelve a buscar, then la
  preferencia está disponible; sin confirmación no se conserva.
- Given hay resultados, when se muestran, then cada tarjeta incluye descripción,
  recompensa, zona, distancia y materiales, pero no dirección exacta.
- Given no hay resultados o falla la búsqueda, when se presenta el tablón, then
  explica el estado y permite ajustar o reintentar sin inventar resultados.
- Given se usa teclado o celular, when se cambia el filtro, then los controles
  tienen nombres accesibles, foco visible y estados expresados con texto.

## Implementation Notes

La distancia se calculará con la fórmula de Haversine sobre coordenadas de demo,
redondeada para presentación. La API recibirá `latitude`, `longitude`,
`radiusKm` y `savePreference`; si se omite ubicación, usará la preferencia
guardada y responderá con error claro cuando no exista ninguna.

## Verification

**Commands:**
- `dotnet build` -- esperado: compilación exitosa.
- `dotnet test --test-modules "tests\**\bin\Debug\net10.0\*Tests.dll"` --
  esperado: todas las pruebas exitosas.
