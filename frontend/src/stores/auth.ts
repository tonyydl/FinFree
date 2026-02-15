import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/api'
import type { LoginRequest, RegisterRequest, AuthResponse } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  // ----- State -----
  const token = ref<string | null>(localStorage.getItem('token'))
  const username = ref<string | null>(null)
  const email = ref<string | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  // 從 localStorage 還原使用者資料
  const savedUser = localStorage.getItem('user')
  if (savedUser) {
    try {
      const parsed = JSON.parse(savedUser)
      username.value = parsed.username ?? null
      email.value = parsed.email ?? null
    } catch {
      localStorage.removeItem('user')
    }
  }

  // ----- Getters -----
  const isAuthenticated = computed(() => !!token.value)

  // ----- Actions -----
  async function login(request: LoginRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<AuthResponse>('/auth/login', request)
      setAuthData(response.data)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '登入失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function register(request: RegisterRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<AuthResponse>('/auth/register', request)
      setAuthData(response.data)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '註冊失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function logout(): Promise<void> {
    try {
      await api.post('/auth/logout')
    } catch {
      // 即使後端呼叫失敗，仍清除本地狀態
    }
    token.value = null
    username.value = null
    email.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  function setAuthData(data: AuthResponse): void {
    token.value = data.token
    username.value = data.username
    email.value = data.email
    localStorage.setItem('token', data.token)
    localStorage.setItem('user', JSON.stringify({
      username: data.username,
      email: data.email,
    }))
  }

  return {
    token,
    username,
    email,
    loading,
    error,
    isAuthenticated,
    login,
    register,
    logout,
  }
})
