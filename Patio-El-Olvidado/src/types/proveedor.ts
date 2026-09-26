export interface Proveedor {
  id: number
  nombre: string
  contacto?: string | null
  telefono?: string | null
  email?: string | null
  notas?: string | null
  activo: boolean
}

export interface ProveedorFilter {
  q?: string
  activo?: boolean
}

export interface CreateProveedorPayload {
  nombre: string
  contacto?: string | null
  telefono?: string | null
  email?: string | null
  notas?: string | null
  activo: boolean
}

export interface UpdateProveedorPayload {
  nombre: string
  contacto?: string | null
  telefono?: string | null
  email?: string | null
  notas?: string | null
  activo: boolean
}
