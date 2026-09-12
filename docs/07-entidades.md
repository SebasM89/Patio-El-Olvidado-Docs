# Entidades

## Usuario
- id
- nombre
- email (único)
- passwordHash
- rolId
- estado
- ultimoAcceso
- intentosFallidos
- bloqueadoHasta

## Rol
- id
- nombre (único)
- descripcion

## Permiso
- id
- codigo (único)
- nombre
- descripcion

## RolPermiso
- rolId
- permisoId

## RefreshToken
- id
- usuarioId
- token (único)
- expiresAt
- createdAt
- revokedAt
- replacedByToken

## PasswordResetToken
- id
- usuarioId
- token (único)
- expiresAt
- createdAt
- usedAt

## Administrador

## Empleado
- tarifaHora
- horasTrabajadas

## Cliente
- telefono
- visitas

## Producto
- id
- nombre
- descripcion
- precio
- categoria
- imagen
- etiquetas
- activo

## Pedido
- id
- tipo (Local | ParaLlevar)
- estado (EnPreparacion | Listo | Entregado | Cancelado)
- subtotal
- total
- clienteId (opcional)
- creadoPorUsuarioId
- fechaCreacion

## DetallePedido
- id
- pedidoId
- productoId
- cantidad
- precioUnitario (snapshot)

## Pago
- metodo
- estado
- monto

## AuditoriaLog

## ConfiguracionSistema

## Reserva
- fecha
- hora
- personas
- estado

## Caja
- fecha
- total

## Inventario

## Proveedor

## Promocion

## Mesa

## Notificacion
