import api from './api'
import type {
  Cliente,
  ClienteFilter,
  ClientePayload,
  HistorialConsumoItem,
} from '../types/cliente'

export const clienteService = {
  list(filter: ClienteFilter = {}) {
    return api.get<Cliente[]>('/api/clientes', { params: filter })
  },
  getById(id: number) {
    return api.get<Cliente>(`/api/clientes/${id}`)
  },
  getMe() {
    return api.get<Cliente>('/api/clientes/me')
  },
  create(payload: ClientePayload) {
    return api.post<Cliente>('/api/clientes', payload)
  },
  update(id: number, payload: ClientePayload) {
    return api.put<Cliente>(`/api/clientes/${id}`, payload)
  },
  remove(id: number) {
    return api.delete(`/api/clientes/${id}`)
  },
  historial(id: number) {
    return api.get<HistorialConsumoItem[]>(`/api/clientes/${id}/historial`)
  },
  meHistorial() {
    return api.get<HistorialConsumoItem[]>('/api/clientes/me/historial')
  },
}
