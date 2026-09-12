import api from './api'
import type { CambiarEstadoPayload, Pedido, PedidoFilter, PedidoPayload } from '../types/pedido'

export const pedidoService = {
  list(filter: PedidoFilter = {}) {
    return api.get<Pedido[]>('/api/pedidos', { params: filter })
  },
  getById(id: number) {
    return api.get<Pedido>(`/api/pedidos/${id}`)
  },
  create(payload: PedidoPayload) {
    return api.post<Pedido>('/api/pedidos', payload)
  },
  update(id: number, payload: PedidoPayload) {
    return api.put<Pedido>(`/api/pedidos/${id}`, payload)
  },
  cambiarEstado(id: number, payload: CambiarEstadoPayload) {
    return api.patch<Pedido>(`/api/pedidos/${id}/estado`, payload)
  },
}
