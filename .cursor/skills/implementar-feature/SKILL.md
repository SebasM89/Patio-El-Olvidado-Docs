---
name: implementar-feature
description: Flujo del desarrollador para implementar features API/Vue de Patio El Olvidado según docs y Clean Architecture. Use when coding backend or frontend features after a plan exists.
icon: code
color: green
---

# Implementar feature (Desarrollador)

## Orden recomendado (API)

1. Domain (entidades / value objects)
2. Application (DTOs, interfaces, servicios, validaciones)
3. Infrastructure (EF, repositorios, Unit of Work)
4. Presentation (controllers, JWT policies)
5. Swagger / AutoMapper profiles
6. Tests mínimos del servicio o endpoint

## Orden recomendado (Vue)

1. Tipos/DTOs del cliente
2. Servicio HTTP
3. Store/composable si aplica
4. Vista/componentes del flujo
5. Rutas y guards por rol

## Verificación local antes de entregar

- `dotnet build` en `API-Patio-El-Olvidado`
- `npm run build` en `Patio-El-Olvidado`
- Listar RN tocadas y cómo probarlas
