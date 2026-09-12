import api from './api'
import type {
  CreateLiquidacionPayload,
  Empleado,
  EmpleadoFilter,
  EmpleadoPayload,
  Fichaje,
  Liquidacion,
  LiquidacionPreview,
} from '../types/empleado'

export const empleadoService = {
  list(filter: EmpleadoFilter = {}) {
    return api.get<Empleado[]>('/api/empleados', { params: filter })
  },
  getById(id: number) {
    return api.get<Empleado>(`/api/empleados/${id}`)
  },
  getMe() {
    return api.get<Empleado>('/api/empleados/me')
  },
  create(payload: EmpleadoPayload) {
    return api.post<Empleado>('/api/empleados', payload)
  },
  update(id: number, payload: EmpleadoPayload) {
    return api.put<Empleado>(`/api/empleados/${id}`, payload)
  },
  remove(id: number) {
    return api.delete(`/api/empleados/${id}`)
  },
  listFichajes(id: number) {
    return api.get<Fichaje[]>(`/api/empleados/${id}/fichajes`)
  },
  listMeFichajes() {
    return api.get<Fichaje[]>('/api/empleados/me/fichajes')
  },
  entrada(id: number) {
    return api.post<Fichaje>(`/api/empleados/${id}/fichajes/entrada`)
  },
  salida(id: number) {
    return api.post<Fichaje>(`/api/empleados/${id}/fichajes/salida`)
  },
  entradaMe() {
    return api.post<Fichaje>('/api/empleados/me/fichajes/entrada')
  },
  salidaMe() {
    return api.post<Fichaje>('/api/empleados/me/fichajes/salida')
  },
  previewLiquidacion(id: number, desde: string, hasta: string) {
    return api.get<LiquidacionPreview>(`/api/empleados/${id}/liquidacion/preview`, {
      params: { desde, hasta },
    })
  },
  previewMeLiquidacion(desde: string, hasta: string) {
    return api.get<LiquidacionPreview>('/api/empleados/me/liquidacion/preview', {
      params: { desde, hasta },
    })
  },
  listLiquidaciones(id: number) {
    return api.get<Liquidacion[]>(`/api/empleados/${id}/liquidaciones`)
  },
  listMeLiquidaciones() {
    return api.get<Liquidacion[]>('/api/empleados/me/liquidaciones')
  },
  generarLiquidacion(id: number, payload: CreateLiquidacionPayload) {
    return api.post<Liquidacion>(`/api/empleados/${id}/liquidaciones`, payload)
  },
}
