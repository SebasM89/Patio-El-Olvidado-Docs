---
name: administrar-esquema
description: Flujo del DBA para diseñar y alinear el esquema SQL Server y EF Core de Patio El Olvidado con docs/07 y el script canónico. Use when changing tables, FKs, indexes, seeds, or DbContext for a feature.
---

# Administrar esquema (DBA)

## Orden

1. Leé el plan del Arquitecto y `docs/07-entidades.md`.
2. Contrastá `Script-BD-Patio-El-Olvidado.sql` y `AppDbContext` / `DbSeeder`.
3. Definí tablas, FKs, CHECKs, UNIQUE e índices. No dupliques legado snake_case.
4. Actualizá el script canónico y el patch idempotente del seeder.
5. Mapeá EF (Fluent API). Dejá el contrato escrito para el Desarrollador.
6. No implementes controllers ni Vue.

## Contrato para el Desarrollador

```markdown
## Tablas
## Columnas y nullability
## FKs y ON DELETE
## Índices y únicos
## Seeds
## Qué no debe redefinir la aplicación
```

## Verificación mínima

- El script crea el modelo nuevo en una base vacía.
- El seeder no falla si la base Development ya existe.
- Nombres alineados con `docs/07-entidades.md`.
