export interface Promocion {
  id: number
  nombre: string
  descripcion: string
  vigenteDesde: string
  vigenteHasta: string
  activo: boolean
}

export interface PromocionFilter {
  q?: string
  activo?: boolean
  vigente?: boolean
}

export interface CreatePromocionPayload {
  nombre: string
  descripcion: string
  vigenteDesde: string
  vigenteHasta: string
}

export interface UpdatePromocionPayload extends CreatePromocionPayload {
  activo: boolean
}
