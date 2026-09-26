export interface Usuario {
  id: number
  nombre: string
  email: string
  rolId: number
  rolNombre: string
  estado: string
  ultimoAcceso: string | null
}

export interface RolCatalogo {
  id: number
  nombre: string
}

export interface UsuarioFilter {
  q?: string
  rol?: string
  estado?: string
}

export interface UsuarioPayload {
  nombre: string
  email: string
  rolId: number
  password?: string
}

export interface CambiarEstadoUsuarioPayload {
  estado: 'Activo' | 'Inactivo'
}
