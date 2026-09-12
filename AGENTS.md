# Patio El Olvidado — Docs y agentes

Fuente de verdad del producto + configuración de agentes Cursor. El código vive en repos separados.

## Contenido de este repo

| Ruta | Contenido |
|------|-----------|
| `docs/` | Objetivos, alcance, RF, RN, módulos, entidades, flujos |
| `AGENTS.md` | Guía compartida para agentes |
| `.cursor/` | Agentes, skills, rules, MCP |
| `Script-BD-Patio-El-Olvidado.sql` | Script inicial SQL Server |

## Repos de código

| Repo | Rol | Ramas |
|------|-----|--------|
| [API-Patio-El-Olvidado](https://github.com/SebasM89/API-Patio-El-Olvidado) | Backend ASP.NET Core 8 | `DEV`, `PRE`, `main` |
| [Patio-El-Olvidado](https://github.com/SebasM89/Patio-El-Olvidado) | Frontend Vue 3 | `DEV`, `PRE`, `main` |
| **Este repo** | Docs + agentes | `DEV`, `PRE`, `main` |

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

- Testing funcional → `DEV`
- Homologación → `PRE`
- Producción → `main` (solo el usuario)
- Skill: `/commits-promocion`

## Flujo recomendado

Arquitecto planifica → Desarrollador implementa → Tester valida → `/commits-promocion` a `DEV` → (OK) homologar a `PRE`.
