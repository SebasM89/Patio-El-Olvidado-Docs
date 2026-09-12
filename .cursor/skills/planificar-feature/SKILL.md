---
name: planificar-feature
description: Flujo del arquitecto para planificar un feature del restaurante usando docs/. Use when planning modules, ADRs, or before implementation of Patio El Olvidado features.
icon: compass
color: blue
---

# Planificar feature (Arquitecto)

## Pasos

1. Identificá el módulo en `docs/06-modulos.md` y RF en `docs/04-requisitos-funcionales.md`.
2. Listá RN en `docs/08-reglas-negocio.md` y CU en `docs/09-casos-de-uso.md` afectados.
3. Contrastá con `docs/07-entidades.md` y `Script-BD-Patio-El-Olvidado.sql`.
4. Revisá capas actuales en `API-Patio-El-Olvidado/` y pantallas en `Patio-El-Olvidado/src/`.
5. Emití el plan con el formato del agente `arquitecto` (decisiones, archivos, criterios de aceptación).
6. No implementes código de producción.

## Checklist MVP

- [ ] Está en `docs/02-alcance.md` (incluido)
- [ ] No invade “fuera de alcance”
- [ ] Roles Admin / Empleado / Cliente respetados (`docs/03-funcionalidades.md`)
