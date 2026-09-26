import api from './api'
import type {
  CreatePromocionPayload,
  Promocion,
  PromocionFilter,
  UpdatePromocionPayload,
} from '../types/promocion'

export const promocionService = {
  list(filter: PromocionFilter = {}) {
    return api.get<Promocion[]>('/api/promociones', { params: filter })
  },
  getById(id: number) {
    return api.get<Promocion>(`/api/promociones/${id}`)
  },
  create(payload: CreatePromocionPayload) {
    return api.post<Promocion>('/api/promociones', payload)
  },
  update(id: number, payload: UpdatePromocionPayload) {
    return api.put<Promocion>(`/api/promociones/${id}`, payload)
  },
  remove(id: number) {
    return api.delete(`/api/promociones/${id}`)
  },
}
