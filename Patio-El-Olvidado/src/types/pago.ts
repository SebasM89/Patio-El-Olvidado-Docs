export type PagoMetodo = 'Efectivo' | 'Tarjeta' | 'Transferencia'
export type PagoEstado = 'Completado' | 'Anulado'

export interface Pago {
  id: number
  pedidoId: number
  metodo: PagoMetodo | string
  estado: PagoEstado | string
  monto: number
  fechaPago: string
  cajaId: number
}

export interface CreatePagoPayload {
  pedidoId: number
  metodo: PagoMetodo
  monto: number
}

export interface CajaDia {
  fecha: string
  totalEfectivo: number
  totalTarjeta: number
  totalTransferencia: number
  total: number
}
