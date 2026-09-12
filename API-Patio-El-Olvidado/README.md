# Patio El Olvidado — API

## Estructura (Clean Architecture)

- `src/PatioElOlvidado.Domain`
- `src/PatioElOlvidado.Application`
- `src/PatioElOlvidado.Infrastructure`
- `src/PatioElOlvidado.API`
- `tests/PatioElOlvidado.Application.Tests`

Abrí la solution: `PatioElOlvidado.sln`

## Development — usuario seed

| Campo | Valor |
|-------|-------|
| Email | `admin@patioelolvidado.local` |
| Password | `Admin123!` |

Documentado también en `src/PatioElOlvidado.API/appsettings.Development.json` (`Seed`). **Solo Development.**

## Auth endpoints

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`

En Development, `IEmailSender` = `LoggingEmailSender` (el token de reset aparece en logs).

## Cómo correr

```bash
dotnet run --project src/PatioElOlvidado.API --launch-profile https
```

Swagger: `https://localhost:7211/swagger`
