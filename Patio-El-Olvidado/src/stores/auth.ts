import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authService } from '../services/authService'
import type { LoginPayload, UsuarioAuth } from '../types/auth'

const ACCESS_KEY = 'peo_access_token'
const REFRESH_KEY = 'peo_refresh_token'
const USER_KEY = 'peo_user'

function loadUser(): UsuarioAuth | null {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as UsuarioAuth
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(localStorage.getItem(ACCESS_KEY))
  const refreshToken = ref<string | null>(localStorage.getItem(REFRESH_KEY))
  const usuario = ref<UsuarioAuth | null>(loadUser())
  const loading = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => !!accessToken.value && !!usuario.value)
  const rol = computed(() => usuario.value?.rol ?? null)

  function persistSession(access: string, refresh: string, user: UsuarioAuth) {
    accessToken.value = access
    refreshToken.value = refresh
    usuario.value = user
    localStorage.setItem(ACCESS_KEY, access)
    localStorage.setItem(REFRESH_KEY, refresh)
    localStorage.setItem(USER_KEY, JSON.stringify(user))
  }

  function clearSession() {
    accessToken.value = null
    refreshToken.value = null
    usuario.value = null
    localStorage.removeItem(ACCESS_KEY)
    localStorage.removeItem(REFRESH_KEY)
    localStorage.removeItem(USER_KEY)
  }

  async function login(payload: LoginPayload) {
    loading.value = true
    error.value = null
    try {
      const { data } = await authService.login(payload)
      persistSession(data.accessToken, data.refreshToken, {
        id: data.usuario.id,
        nombre: data.usuario.nombre,
        email: data.usuario.email,
        rol: data.usuario.rol,
      })
    } catch (e: unknown) {
      clearSession()
      const msg =
        (e as { response?: { data?: { message?: string }; status?: number } })?.response?.data
          ?.message ?? 'No se pudo iniciar sesión.'
      error.value = msg
      throw e
    } finally {
      loading.value = false
    }
  }

  async function refreshSession() {
    if (!refreshToken.value) throw new Error('Sin refresh token')
    const { data } = await authService.refresh({ refreshToken: refreshToken.value })
    persistSession(data.accessToken, data.refreshToken, {
      id: data.usuario.id,
      nombre: data.usuario.nombre,
      email: data.usuario.email,
      rol: data.usuario.rol,
    })
  }

  async function logout() {
    try {
      if (refreshToken.value) {
        await authService.logout({ refreshToken: refreshToken.value })
      }
    } finally {
      clearSession()
    }
  }

  return {
    accessToken,
    refreshToken,
    usuario,
    loading,
    error,
    isAuthenticated,
    rol,
    login,
    logout,
    refreshSession,
    clearSession,
  }
})
