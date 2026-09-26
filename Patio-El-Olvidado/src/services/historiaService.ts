import api from './api'
import type { Historia, UpdateHistoriaPayload } from '../types/historia'

export const historiaService = {
  get() {
    return api.get<Historia>('/api/historia')
  },
  update(payload: UpdateHistoriaPayload) {
    return api.put<Historia>('/api/historia', payload)
  },
}
