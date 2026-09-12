export interface UsuarioAuth {
  id: number
  nombre: string
  email: string
  rol: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  usuario: UsuarioAuth
}

export interface MessageResponse {
  message: string
}

export interface LoginPayload {
  email: string
  password: string
}

export interface RefreshPayload {
  refreshToken: string
}

export interface LogoutPayload {
  refreshToken: string
}

export interface ForgotPasswordPayload {
  email: string
}

export interface ResetPasswordPayload {
  token: string
  newPassword: string
}
