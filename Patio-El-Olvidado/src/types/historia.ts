export interface Historia {
  id: number
  titulo: string
  texto: string
  actualizadoUtc: string | null
  actualizadoPorUsuarioId: number | null
}

export interface UpdateHistoriaPayload {
  titulo: string
  texto: string
}
