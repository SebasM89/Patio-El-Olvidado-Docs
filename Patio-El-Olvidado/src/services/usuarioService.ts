import api from './api'
import type {
  CambiarEstadoUsuarioPayload,
  RolCatalogo,
  Usuario,
  UsuarioFilter,
  UsuarioPayload,
} from '../types/usuario'

export const usuarioService = {
  list(filter: UsuarioFilter = {}) {
    return api.get<Usuario[]>('/api/usuarios', { params: filter })
  },
  getById(id: number) {
    return api.get<Usuario>(`/api/usuarios/${id}`)
  },
  listRoles() {
    return api.get<RolCatalogo[]>('/api/roles')
  },
  create(payload: UsuarioPayload) {
    return api.post<Usuario>('/api/usuarios', payload)
  },
  update(id: number, payload: UsuarioPayload) {
    return api.put<Usuario>(`/api/usuarios/${id}`, payload)
  },
  cambiarEstado(id: number, payload: CambiarEstadoUsuarioPayload) {
    return api.patch<Usuario>(`/api/usuarios/${id}/estado`, payload)
  },
}
