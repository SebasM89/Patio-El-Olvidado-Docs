import api from './api'
import type { Mesa, Reserva, ReservaFilter, ReservaPayload } from '../types/reserva'

export const mesaService = {
  list() {
    return api.get<Mesa[]>('/api/mesas')
  },
}

export const reservaService = {
  list(filter: ReservaFilter = {}) {
    return api.get<Reserva[]>('/api/reservas', { params: filter })
  },
  getById(id: number) {
    return api.get<Reserva>(`/api/reservas/${id}`)
  },
  create(payload: ReservaPayload) {
    return api.post<Reserva>('/api/reservas', payload)
  },
  cambiarEstado(id: number, estado: 'Cancelada' | 'Finalizada') {
    return api.patch<Reserva>(`/api/reservas/${id}/estado`, { estado })
  },
}
