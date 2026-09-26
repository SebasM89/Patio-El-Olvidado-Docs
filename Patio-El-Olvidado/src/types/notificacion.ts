export interface Notificacion {
  id: number
  titulo: string
  mensaje: string
  tipo: string
  leida: boolean
  fechaUtc: string
  leidaUtc: string | null
  stockItemId: number
  movimientoStockId: number
}

export interface NotificacionConteo {
  cantidad: number
}
