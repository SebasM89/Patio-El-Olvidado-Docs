---
name: verificar-feature
description: Flujo del tester para validar features de Patio El Olvidado contra RF/RN con evidencia de build y pruebas. Use after implementations or before accepting a feature as done.
icon: check-circle
color: orange
---

# Verificar feature (Tester)

## Pasos

1. Leé criterios de aceptación del plan / mensaje del usuario.
2. Mapeá a RF, RN y CU en `docs/`.
3. Ejecutá builds y tests relevantes.
4. Probá casos felices y bordes (bloqueos, roles, estados de pedido, descuentos, caja).
5. Emití veredicto con evidencia (comandos + resultados).

## Matriz mínima RN

| RN | Qué comprobar |
|----|----------------|
| RN-01 | Sin token no opera |
| RN-02 | 3 fallos → bloqueo ~30s |
| RN-03 | Solo admin modifica menú |
| RN-04 | Solo EnPreparacion editable |
| RN-05 | 5ª visita → descuento |
| RN-06 | Reservas sin solapamiento |
| RN-07 | Empleado solo sus horas |
| RN-08 | Pago actualiza caja |

Solo aplicá las filas del feature bajo prueba.
