---
name: commits-promocion
description: Commits, push y promoción de ramas Patio El Olvidado. Publica en dev para testing funcional, homologa en pre; nunca despliega a main. Use when the user asks to commit, push, open PR, promote to pre, or publish for testing.
icon: git-branch
color: purple
---

# Commits y promoción (dev → pre → main)

## Política de ramas (obligatoria)

| Rama | Uso | Quién |
|------|-----|--------|
| `feature/*` o `fix/*` | Trabajo diario | Agente + usuario |
| `DEV` | Testing funcional | Agente publica aquí |
| `PRE` | Homologación | Agente solo tras OK en `DEV` |
| `main` | Producción / despliegue | **Solo el usuario** |

Usar exactamente `DEV` y `PRE` (mayúsculas), como en los remotos actuales.

Repos: `API-Patio-El-Olvidado` y `Patio-El-Olvidado` son repos **separados**. Commitear/pushear en el repo que corresponda al cambio. Docs/agentes en la raíz local no están en ninguno hasta que el usuario defina el repo destino.

**Nunca** mergear, pushear ni abrir PR hacia `main` a menos que el usuario lo pida explícitamente.

## Cuándo aplicar

- Usuario pide commit / push / PR / “subí a testing” / “homologá”
- Tras implementación lista para testing funcional
- Tras tester APROBADO y pedido de pasar a `pre`

## Prechecks

1. Confirmar que el directorio raíz es un repo git (`git status`). Si no lo es, avisar y pedir `git init` + remote (no inventar el remote).
2. Confirmar ramas `dev` y `pre` (crearlas en remoto solo si el usuario lo autoriza).
3. No commitear secretos (`.env`, tokens, `mcp.env`, passwords).
4. Seguir protocolo de commits del usuario (status + diff + log → mensaje → add → commit → status).

## Flujo A — Publicar en `DEV` (testing funcional)

1. Trabajar en `feature/<slug>` o `fix/<slug>` desde `DEV` actualizado.
2. Commit con mensaje claro (por qué, no solo qué). Ejemplo:

```
feat(pedidos): calcular total según RN de descuentos

Habilita testing funcional del flujo pedido→pago en DEV.
```

3. `git push -u origin HEAD`
4. Abrir PR **hacia `DEV`** (MCP `github` o `gh pr create`):
   - base: `DEV`
   - head: la feature branch
   - body: RF/RN tocados + cómo probar
5. Si el usuario pide merge directo a `DEV` sin PR, hacerlo solo con confirmación explícita.
6. Informar: “Listo en `DEV` para testing funcional. Homologación a `PRE` cuando el tester apruebe.”

## Flujo B — Homologar a `PRE`

Solo si:

- Hay evidencia de testing funcional OK en `DEV`, **o**
- El usuario dice explícitamente “homologá / pasá a PRE”

Pasos:

1. Actualizar `PRE` desde remoto.
2. Abrir PR `DEV` → `PRE` (preferido) o merge con autorización.
3. No tocar `main`.
4. Informar que el despliegue a `main` lo hace el usuario.

## Flujo C — Prohibido por defecto

- PR/merge `PRE` → `main` o `DEV` → `main`
- Push directo a `main`
- Tag de release a producción

Si el usuario lo pide, repetir la solicitud y ejecutar solo ese paso, sin automatizar deploys.

## Mensajes de commit

- Prefijo: `feat|fix|docs|refactor|test|chore`
- Alcance opcional: módulo (`auth`, `menu`, `pedidos`, `caja`, …)
- Cuerpo: RN/RF relevantes si aplica

## Checklist final al usuario

```markdown
## Git
- Branch: ...
- Commit: ...
- Destino: DEV | PRE
- PR URL: ...
- main: sin cambios (correcto)
```

## Referencias

- Política detallada: [reference.md](reference.md)
