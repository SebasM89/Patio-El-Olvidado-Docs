export interface Mesa {
  id: number
  numero: number
  capacidad: number
  ubicacion: string | null
}

export type EstadoReserva = 'Confirmada' | 'Cancelada' | 'Finalizada'

export interface Reserva {
  id: number
  clienteId: number
  clienteNombre: string
  mesaId: number
  mesaNumero: number
  fecha: string
  horaInicio: string
  horaFin: string
  personas: number
  estado: EstadoReserva
  creadoPorUsuarioId: number
  fechaCreacion: string
}

export interface ReservaPayload {
  mesaId: number
  fecha: string
  horaInicio: string
  horaFin: string
  personas: number
  clienteId?: number | null
}

export interface ReservaFilter {
  fecha?: string
  mesaId?: number
}
