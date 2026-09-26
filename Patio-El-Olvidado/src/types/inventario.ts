export type UnidadStock = 'Unidad' | 'Kg' | 'L'
export type TipoMovimientoStock = 'Entrada' | 'Salida'

export interface StockItem {
  id: number
  nombre: string
  descripcion?: string | null
  unidad: UnidadStock
  cantidadActual: number
  stockMinimo: number
  activo: boolean
  enAlerta: boolean
}

export interface StockItemFilter {
  q?: string
  activo?: boolean
  enAlerta?: boolean
}

export interface CreateStockItemPayload {
  nombre: string
  descripcion?: string | null
  unidad: UnidadStock
  stockMinimo: number
  cantidadInicial: number
  activo: boolean
}

export interface UpdateStockItemPayload {
  nombre: string
  descripcion?: string | null
  stockMinimo: number
  activo: boolean
}

export interface MovimientoStock {
  id: number
  stockItemId: number
  tipo: TipoMovimientoStock
  cantidad: number
  motivo?: string | null
  fechaUtc: string
  registradoPorUsuarioId: number
  registradoPorNombre?: string | null
}

export interface RegistrarMovimientoPayload {
  tipo: TipoMovimientoStock
  cantidad: number
  motivo?: string | null
}
