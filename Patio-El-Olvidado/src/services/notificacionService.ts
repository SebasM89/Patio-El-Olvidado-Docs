import api from './api'
import type { Notificacion, NotificacionConteo } from '../types/notificacion'

export const notificacionService = {
  list(leida?: boolean) {
    return api.get<Notificacion[]>('/api/notificaciones', {
      params: leida === undefined ? {} : { leida },
    })
  },
  conteo() {
    return api.get<NotificacionConteo>('/api/notificaciones/conteo')
  },
  marcarLeida(id: number) {
    return api.patch<Notificacion>(`/api/notificaciones/${id}/leida`)
  },
}
