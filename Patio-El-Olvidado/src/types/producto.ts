export interface Producto {
  id: number
  nombre: string
  descripcion: string | null
  precio: number
  categoria: string
  imagen: string | null
  etiquetas: string | null
  activo: boolean
}

export interface ProductoFilter {
  q?: string
  categoria?: string
  etiqueta?: string
  soloActivos?: boolean
}

export interface ProductoPayload {
  nombre: string
  descripcion?: string | null
  precio: number
  categoria: string
  imagen?: string | null
  etiquetas?: string | null
  activo: boolean
}
