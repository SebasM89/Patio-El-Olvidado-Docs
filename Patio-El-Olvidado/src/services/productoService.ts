import api from './api'
import type { Producto, ProductoFilter, ProductoPayload } from '../types/producto'

export const productoService = {
  list(filter: ProductoFilter = {}) {
    return api.get<Producto[]>('/api/productos', { params: filter })
  },
  getById(id: number) {
    return api.get<Producto>(`/api/productos/${id}`)
  },
  create(payload: ProductoPayload) {
    return api.post<Producto>('/api/productos', payload)
  },
  update(id: number, payload: ProductoPayload) {
    return api.put<Producto>(`/api/productos/${id}`, payload)
  },
  remove(id: number) {
    return api.delete(`/api/productos/${id}`)
  },
}
