# Patio El Olvidado — Monorepo (docs + agentes + código)

Sistema integral de gestión para restaurante. Todo el proyecto vive en este repositorio.

## Estructura

| Ruta | Contenido |
|------|-----------|
| `docs/` | **Fuente de verdad** del producto (objetivos, alcance, RF, RN, módulos, entidades, flujos) |
| `AGENTS.md` | Guía compartida para agentes |
| `.cursor/` | Agentes, skills, rules, MCP |
| `API-Patio-El-Olvidado/` | Backend ASP.NET Core 8 Web API |
| `Patio-El-Olvidado/` | Frontend Vue 3 + TypeScript + Vite |
| `Script-BD-Patio-El-Olvidado.sql` | Script inicial de SQL Server |

## Antes de cualquier cambio

1. Leer `docs/00-overview.md` y el doc específico del módulo tocado.
2. Respetar `docs/cursor-rules.md` (Clean Architecture, SOLID, patrones listados).
3. Alinear entidades/API con `docs/07-entidades.md` y el script SQL.
4. Aplicar reglas de negocio de `docs/08-reglas-negocio.md`.
5. No inventar alcance fuera de `docs/02-alcance.md` (MVP).

## Stack obligatorio

- Frontend: Vue 3 + TypeScript + Vite
- Backend: ASP.NET Core 8 Web API
- ORM: Entity Framework Core
- BD: SQL Server
- Capas: Presentation → Application → Domain → Infrastructure
- Patrones: Repository, Service, DI, DTO, Unit of Work
- Extra: JWT, FluentValidation, AutoMapper, Swagger

## Roles de agentes

- `/arquitecto` — diseño, límites, ADRs, planes; no implementa features
- `/desarrollador` — implementa según plan y docs
- `/tester` — verifica con evidencia (tests, builds, flujos)

## MCP

Configurados en `.cursor/mcp.json` (credenciales vía variables de entorno; plantilla en `.cursor/mcp.env.example`):

- `mssql` — esquema/datos SQL Server (`ElOlvidado`)
- `github` — PRs, ramas e issues

Setup local: `.cursor/scripts/setup-mcp-env.ps1`

## Git (ambientes)

Repo único: [Patio-El-Olvidado-Docs](https://github.com/SebasM89/Patio-El-Olvidado-Docs)

- Testing funcional → `DEV`
- Homologación → `PRE`
- Producción → `main` (solo el usuario)
- Skill: `/commits-promocion`

Los repos históricos `API-Patio-El-Olvidado` y `Patio-El-Olvidado` quedan como legado; el desarrollo continúa en este monorepo.

## Flujo recomendado

Arquitecto planifica → Desarrollador implementa → Tester valida → `/commits-promocion` a `DEV` → (OK) homologar a `PRE`.
