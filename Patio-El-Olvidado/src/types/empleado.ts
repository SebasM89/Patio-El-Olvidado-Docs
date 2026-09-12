export interface Empleado {
  id: number
  nombre: string
  puesto: string | null
  telefono: string | null
  tarifaHora: number
  horasTrabajadas: number
  usuarioId: number | null
  activo: boolean
}

export interface EmpleadoFilter {
  q?: string
  activo?: boolean
}

export interface EmpleadoPayload {
  nombre: string
  puesto?: string | null
  telefono?: string | null
  tarifaHora: number
  usuarioId?: number | null
  activo: boolean
}

export interface Fichaje {
  id: number
  empleadoId: number
  entradaUtc: string
  salidaUtc: string | null
  horas: number | null
  abierto: boolean
}

export interface LiquidacionPreview {
  empleadoId: number
  periodoDesde: string
  periodoHasta: string
  horas: number
  tarifaHora: number
  monto: number
}

export interface Liquidacion {
  id: number
  empleadoId: number
  periodoDesde: string
  periodoHasta: string
  horas: number
  tarifaHoraSnapshot: number
  monto: number
  generadaEnUtc: string
  generadaPorUsuarioId: number
}

export interface CreateLiquidacionPayload {
  periodoDesde: string
  periodoHasta: string
}
