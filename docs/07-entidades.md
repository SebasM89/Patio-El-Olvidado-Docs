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
- id
- nombre
- puesto (opcional)
- telefono (opcional)
- tarifaHora (> 0)
- horasTrabajadas (≥ 0; se incrementa al cerrar fichaje)
- usuarioId (opcional, único → Usuarios rol Empleado)
- activo (soft-delete)

## Fichaje
- id
- empleadoId
- entradaUtc
- salidaUtc (nullable mientras abierto)
- horas (nullable; al cerrar = salida − entrada)

## Liquidacion
- id
- empleadoId
- periodoDesde / periodoHasta (date UTC)
- horas
- tarifaHoraSnapshot
- monto (horas × tarifa al generar)
- generadaEnUtc
- generadaPorUsuarioId (Admin)

## Cliente
- id
- nombre
- telefono
- email (opcional, único si no null)
- visitas (≥ 0; se incrementa al cobro completo del pedido)
- usuarioId (opcional, único → Usuarios rol Cliente)
- activo (soft-delete)

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
- total (puede ser menor al subtotal por RN-05 fidelización 10%)
- clienteId (opcional, FK Clientes ON DELETE SET NULL)
- visitaContabilizada (idempotencia del ++visitas al cobro)
- creadoPorUsuarioId
- fechaCreacion

## DetallePedido
- id
- pedidoId
- productoId
- cantidad
- precioUnitario (snapshot)

## Pago
- id
- pedidoId (FK Pedido)
- metodo (Efectivo | Tarjeta | Transferencia)
- estado (Completado | Anulado)
- monto
- fechaPago (UTC)
- cajaId (FK Caja)

## AuditoriaLog

## ConfiguracionSistema

## Reserva
- fecha
- hora
- personas
- estado

## Caja
- id
- fecha (date UTC, única = un registro/día)
- totalEfectivo
- totalTarjeta
- totalTransferencia
- total (derivado = suma de los tres)
## Inventario

## Proveedor

## Promocion

## Mesa

## Notificacion
