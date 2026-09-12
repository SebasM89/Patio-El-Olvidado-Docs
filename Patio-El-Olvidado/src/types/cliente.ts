export interface Cliente {
  id: number
  nombre: string
  telefono: string
  email: string | null
  visitas: number
  usuarioId: number | null
  activo: boolean
}

export interface ClienteFilter {
  q?: string
  activo?: boolean
}

export interface ClientePayload {
  nombre: string
  telefono: string
  email?: string | null
  usuarioId?: number | null
  activo: boolean
}

export interface HistorialConsumoItem {
  pedidoId: number
  fechaCreacion: string
  tipo: string
  estado: string
  subtotal: number
  total: number
  descuentoMonto: number
  pagosCompletados: number
}
