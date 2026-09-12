import api from './api'
import type {
  AuthResponse,
  ForgotPasswordPayload,
  LoginPayload,
  LogoutPayload,
  MessageResponse,
  RefreshPayload,
  ResetPasswordPayload,
} from '../types/auth'

export const authService = {
  login(payload: LoginPayload) {
    return api.post<AuthResponse>('/api/auth/login', payload)
  },
  refresh(payload: RefreshPayload) {
    return api.post<AuthResponse>('/api/auth/refresh', payload)
  },
  logout(payload: LogoutPayload) {
    return api.post<MessageResponse>('/api/auth/logout', payload)
  },
  forgotPassword(payload: ForgotPasswordPayload) {
    return api.post<MessageResponse>('/api/auth/forgot-password', payload)
  },
  resetPassword(payload: ResetPasswordPayload) {
    return api.post<MessageResponse>('/api/auth/reset-password', payload)
  },
}
