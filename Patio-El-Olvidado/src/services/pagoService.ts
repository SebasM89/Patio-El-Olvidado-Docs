import api from './api'
import type { CajaDia, CreatePagoPayload, Pago } from '../types/pago'

export const pagoService = {
  listByPedido(pedidoId: number) {
    return api.get<Pago[]>('/api/pagos', { params: { pedidoId } })
  },
  getById(id: number) {
    return api.get<Pago>(`/api/pagos/${id}`)
  },
  create(payload: CreatePagoPayload) {
    return api.post<Pago>('/api/pagos', payload)
  },
  anular(id: number) {
    return api.post<Pago>(`/api/pagos/${id}/anular`)
  },
  cajaHoy() {
    return api.get<CajaDia>('/api/caja/hoy')
  },
}
