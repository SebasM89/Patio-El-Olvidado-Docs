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
- id
- clienteId (FK Clientes ON DELETE NO ACTION)
- mesaId (FK Mesas ON DELETE NO ACTION)
- fecha (DATE, día civil, no UTC)
- horaInicio / horaFin (TIME; horaFin > horaInicio)
- personas (> 0)
- estado (Confirmada | Cancelada | Finalizada)
- creadoPorUsuarioId (FK Usuarios ON DELETE NO ACTION)
- fechaCreacion (DATETIME2, default SYSUTCDATETIME())

## Caja
- id
- fecha (date UTC, única = un registro/día)
- totalEfectivo
- totalTarjeta
- totalTransferencia
- total (derivado = suma de los tres)
## Inventario

### StockItem (tabla StockItems)
- id INT IDENTITY PK
- nombre NVARCHAR(100) NOT NULL, único (UX_StockItems_Nombre)
- descripcion NVARCHAR(300) NULL
- unidad NVARCHAR(10) NOT NULL, nombre del enum (Unidad | Kg | L)
- cantidadActual DECIMAL(12,3) NOT NULL DEFAULT 0, CHECK >= 0
- stockMinimo DECIMAL(12,3) NOT NULL DEFAULT 0, CHECK >= 0
- activo BIT NOT NULL DEFAULT 1
- version ROWVERSION NOT NULL
- índice IX_StockItems_Activo (Activo)
- Sin FK a Productos. Sin semilla.

### MovimientoStock (tabla MovimientosStock)
- id INT IDENTITY PK
- stockItemId INT NOT NULL, FK StockItems ON DELETE NO ACTION
- tipo NVARCHAR(10) NOT NULL, nombre del enum (Entrada | Salida)
- cantidad DECIMAL(12,3) NOT NULL, CHECK > 0
- motivo NVARCHAR(200) NULL
- fechaUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
- registradoPorUsuarioId INT NOT NULL, FK Usuarios ON DELETE NO ACTION
- proveedorId INT NULL, FK Proveedores ON DELETE NO ACTION. Solo entradas: CHECK CK_MovimientosStock_ProveedorSoloEntrada (ProveedorId IS NULL OR Tipo = N'Entrada')
- índice IX_MovimientosStock_StockItemId_FechaUtc (StockItemId, FechaUtc DESC)
- índice filtrado IX_MovimientosStock_ProveedorId (ProveedorId) WHERE ProveedorId IS NOT NULL
- Sin semilla.

## Proveedor (tabla Proveedores)
- id INT IDENTITY PK
- nombre NVARCHAR(100) NOT NULL, único (UX_Proveedores_Nombre)
- contacto NVARCHAR(100) NULL
- telefono NVARCHAR(30) NULL
- email NVARCHAR(150) NULL, sin único
- notas NVARCHAR(300) NULL
- activo BIT NOT NULL DEFAULT 1
- índice IX_Proveedores_Activo (Activo)
- Sin ROWVERSION, sin semilla, sin CUIT.

## Promocion (tabla Promociones)
- id INT IDENTITY PK
- nombre NVARCHAR(100) NOT NULL
- descripcion NVARCHAR(500) NOT NULL
- vigenteDesde DATE NOT NULL
- vigenteHasta DATE NOT NULL
- activo BIT NOT NULL DEFAULT 1
- CHECK CK_Promociones_Vigencia (VigenteHasta >= VigenteDesde)
- índice IX_Promociones_Activo_Vigencia (Activo, VigenteDesde, VigenteHasta)
- Sin FKs, sin unicidad de nombre, sin ROWVERSION, sin semilla
- Baja lógica: Activo = 0. No hay DELETE físico en el esquema.
- No agrega PromocionId ni columnas a Pedidos.

## Mesa
- id
- numero (único)
- capacidad (> 0)
- ubicacion (opcional)
- Sin Activo y sin CRUD. Catálogo semilla: números 1–4, capacidades 2, 4, 4, 6.

## Notificacion (tabla Notificaciones)
- id INT IDENTITY PK
- usuarioId INT NOT NULL, FK Usuarios ON DELETE NO ACTION
- titulo NVARCHAR(120) NOT NULL
- mensaje NVARCHAR(500) NOT NULL
- tipo NVARCHAR(20) NOT NULL, nombre del enum (solo StockAlerta). CHECK CK_Notificaciones_Tipo
- leida BIT NOT NULL DEFAULT 0
- leidaUtc DATETIME2 NULL
- fechaUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
- stockItemId INT NOT NULL, FK StockItems ON DELETE NO ACTION
- movimientoStockId INT NOT NULL, FK MovimientosStock ON DELETE NO ACTION
- índice IX_Notificaciones_UsuarioId_FechaUtc (UsuarioId, FechaUtc DESC)
- índice filtrado IX_Notificaciones_UsuarioId_NoLeidas (UsuarioId) WHERE Leida = 0
- único UX_Notificaciones_Usuario_Movimiento (UsuarioId, MovimientoStockId)
- Sin ROWVERSION, sin semilla. No agrega columnas a StockItems ni a MovimientosStock.

## HistoriaRestaurante (tabla dbo.HistoriaRestaurante)
- Una sola fila. Sin IDENTITY, sin Activo, sin ROWVERSION, sin índices extra.
- Id INT NOT NULL PK (PK_HistoriaRestaurante). CHECK CK_HistoriaRestaurante_Singleton (Id = 1)
- Titulo NVARCHAR(120) NOT NULL. CHECK CK_HistoriaRestaurante_Titulo (LEN(Titulo) BETWEEN 1 AND 120)
- Texto NVARCHAR(4000) NOT NULL. CHECK CK_HistoriaRestaurante_Texto (LEN(Texto) BETWEEN 1 AND 4000)
- ActualizadoUtc DATETIME2 NULL (NULL en la semilla)
- ActualizadoPorUsuarioId INT NULL, FK FK_HistoriaRestaurante_Usuarios → Usuarios(Id) ON DELETE NO ACTION (NULL en la semilla)
- Semilla solo si no existe Id = 1. Si la fila ya existe, no se actualizan título ni texto:
  - Id = 1
  - Titulo = N'Patio El Olvidado'
  - Texto = N'Patio El Olvidado es el restaurante que este sistema administra. El administrador puede reemplazar este texto.'
  - ActualizadoUtc y ActualizadoPorUsuarioId NULL
- No agrega columnas a Usuarios ni a otras tablas.
