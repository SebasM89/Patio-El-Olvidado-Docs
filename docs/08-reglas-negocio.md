RN-01
Un usuario debe autenticarse para operar.

RN-02
Después de 3 intentos fallidos el acceso queda bloqueado 30 segundos.

RN-03
Solo el administrador puede modificar el menú.

RN-04
Solo se pueden modificar pedidos en estado EnPreparacion.

RN-05
La quinta visita genera un descuento automático.

RN-06
Una reserva no puede superponerse con otra.

RN-07
Los empleados solo pueden registrar sus propias horas.

RN-08
Los pagos actualizan automáticamente la caja.

RN-09
La cantidad actual de un ítem solo cambia al registrar un movimiento, en la misma transacción. El saldo no puede quedar negativo. Los movimientos no se modifican ni se eliminan.

RN-10
Solo el administrador crea, edita y registra movimientos de inventario. El empleado consulta. El cliente no accede.

RN-11
Solo el administrador crea, edita y da de baja lógica a proveedores (Activo). El empleado consulta. El cliente no accede. Una salida de stock no lleva proveedor. Si el movimiento indica proveedor, debe existir y estar activo. El vínculo se fija al insertar el movimiento y no se modifica.

RN-12
Cuando un movimiento deja un ítem activo en alerta (saldo menor o igual al mínimo) y antes no lo estaba, se graba un aviso in-app por cada usuario con rol Admin y estado Activo, en la misma transacción. Si ya estaba en alerta, no se duplica. Una salida rechazada por saldo, el alta inicial, la edición del mínimo y una entrada que no cruza el umbral no generan aviso. Sin administradores activos el movimiento igual se registra. No hay correo, alta manual ni borrado. Admin y Empleado consultan y marcan como leídas solo las propias. El cliente no accede.

RN-13
Solo el administrador crea, edita y da de baja lógica a promociones. El cliente consulta las activas cuya vigencia incluye la fecha UTC de hoy. El empleado no accede. Una promoción no modifica el subtotal ni el total del pedido y no se combina con el descuento de RN-05. No hay cupón, código ni campaña.

RN-14
Hay una sola historia. Solo Admin edita título y texto. Admin, Empleado y Cliente autenticados consultan. No hay alta, baja ni lectura sin sesión. No es el historial de consumo (CU09 /mi-historial no se toca). El texto no modifica precios, pedidos ni promociones.