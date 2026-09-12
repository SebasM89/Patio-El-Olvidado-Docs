# Referencia: ambientes y MCP

## Ambientes

```
feature/* ──PR──► DEV (testing funcional)
                    │
                    │ (tester OK / pedido explícito)
                    ▼
                   PRE (homologación)
                    │
                    │ (SOLO usuario)
                    ▼
                   main (producción)
```

Repos separados (estado actual):

- `github.com/SebasM89/API-Patio-El-Olvidado`
- `github.com/SebasM89/Patio-El-Olvidado`
- `github.com/SebasM89/Patio-El-Olvidado-Docs` (docs, agentes, script SQL)

La carpeta local `Proyecto-Final/` agrupa API + front + docs; este repo Docs ignora las carpetas de código vía `.gitignore`.

## MCP del proyecto (`.cursor/mcp.json`)

### `github`

- Auth: variable `GITHUB_PERSONAL_ACCESS_TOKEN`
- Uso: crear/listar PRs, issues, revisar estado de ramas
- PRs típicos: `feature/*` → `dev`, luego `dev` → `pre`

### `mssql`

- Auth/conexión: `MSSQL_*` (ver `.cursor/mcp.env.example`)
- BD esperada: `ElOlvidado` (script `Script-BD-Patio-El-Olvidado.sql`)
- Uso: listar tablas, contrastar con `docs/07-entidades.md`, queries de verificación
- Preferir SELECT / inspección; DDL solo con pedido explícito del usuario

## Setup local (una vez)

1. Copiar valores de `.cursor/mcp.env.example` a variables de entorno de Windows.
2. En PowerShell (sesión o `$PROFILE`):

```powershell
[System.Environment]::SetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", "ghp_...", "User")
[System.Environment]::SetEnvironmentVariable("MSSQL_SERVER", "localhost", "User")
[System.Environment]::SetEnvironmentVariable("MSSQL_PORT", "1433", "User")
[System.Environment]::SetEnvironmentVariable("MSSQL_DATABASE", "ElOlvidado", "User")
[System.Environment]::SetEnvironmentVariable("MSSQL_USER", "sa", "User")
[System.Environment]::SetEnvironmentVariable("MSSQL_PASSWORD", "TU_PASSWORD", "User")
[System.Environment]::SetEnvironmentVariable("MSSQL_ENCRYPT", "false", "User")
[System.Environment]::SetEnvironmentVariable("MSSQL_TRUST_SERVER_CERTIFICATE", "true", "User")
```

3. Cerrar y reabrir Cursor.
4. Settings → MCP: verificar que `github` y `mssql` estén en verde.

## Repo git en la raíz

Si aún no hay `.git` en `Proyecto-Final/`:

```powershell
git init
git remote add origin <URL_DEL_REPO>
git checkout -b main
# commits iniciales...
git checkout -b pre
git checkout -b dev
git push -u origin main pre dev
```

El agente no debe asumir la URL del remote: pedirla al usuario.
