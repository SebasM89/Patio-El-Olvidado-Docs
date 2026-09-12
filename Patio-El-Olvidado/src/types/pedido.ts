export type PedidoTipo = 'Local' | 'ParaLlevar'
export type PedidoEstado = 'EnPreparacion' | 'Listo' | 'Entregado' | 'Cancelado'

export interface DetallePedido {
  id: number
  productoId: number
  productoNombre: string | null
  cantidad: number
  precioUnitario: number
  subtotal: number
}

export interface Pedido {
  id: number
  tipo: PedidoTipo | string
  estado: PedidoEstado | string
  subtotal: number
  total: number
  descuentoMonto?: number
  descuentoAplicado?: boolean
  fechaCreacion: string
  clienteId: number | null
  creadoPorUsuarioId: number
  detalles: DetallePedido[]
}

export interface PedidoFilter {
  estado?: string
  tipo?: string
  desde?: string
  hasta?: string
}

export interface DetallePedidoLinePayload {
  productoId: number
  cantidad: number
}

export interface PedidoPayload {
  tipo: PedidoTipo
  clienteId?: number | null
  detalles: DetallePedidoLinePayload[]
}

export interface CambiarEstadoPayload {
  estado: Exclude<PedidoEstado, 'EnPreparacion'>
}
