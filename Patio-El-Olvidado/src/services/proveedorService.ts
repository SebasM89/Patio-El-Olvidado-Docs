import api from './api'
import type {
  CreateProveedorPayload,
  Proveedor,
  ProveedorFilter,
  UpdateProveedorPayload,
} from '../types/proveedor'

export const proveedorService = {
  list(filter: ProveedorFilter = {}) {
    return api.get<Proveedor[]>('/api/proveedores', { params: filter })
  },
  getById(id: number) {
    return api.get<Proveedor>(`/api/proveedores/${id}`)
  },
  create(payload: CreateProveedorPayload) {
    return api.post<Proveedor>('/api/proveedores', payload)
  },
  update(id: number, payload: UpdateProveedorPayload) {
    return api.put<Proveedor>(`/api/proveedores/${id}`, payload)
  },
}
