import api from './api'
import type {
  CreateStockItemPayload,
  MovimientoStock,
  RegistrarMovimientoPayload,
  StockItem,
  StockItemFilter,
  UpdateStockItemPayload,
} from '../types/inventario'

export const inventarioService = {
  list(filter: StockItemFilter = {}) {
    return api.get<StockItem[]>('/api/inventario', { params: filter })
  },
  alertas() {
    return api.get<StockItem[]>('/api/inventario/alertas')
  },
  getById(id: number) {
    return api.get<StockItem>(`/api/inventario/${id}`)
  },
  create(payload: CreateStockItemPayload) {
    return api.post<StockItem>('/api/inventario', payload)
  },
  update(id: number, payload: UpdateStockItemPayload) {
    return api.put<StockItem>(`/api/inventario/${id}`, payload)
  },
  movimientos(id: number) {
    return api.get<MovimientoStock[]>(`/api/inventario/${id}/movimientos`)
  },
  registrarMovimiento(id: number, payload: RegistrarMovimientoPayload) {
    return api.post<MovimientoStock>(`/api/inventario/${id}/movimientos`, payload)
  },
}
