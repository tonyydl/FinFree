import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/api'
import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
  TransferRequest,
} from '@/types'

export const useAccountStore = defineStore('account', () => {
  const accounts = ref<AccountResponse[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAccounts(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<AccountResponse[]>('/accounts')
      accounts.value = response.data
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '取得帳戶失敗'
    } finally {
      loading.value = false
    }
  }

  async function createAccount(request: CreateAccountRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<AccountResponse>('/accounts', request)
      accounts.value.push(response.data)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '新增帳戶失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function updateAccount(id: number, request: UpdateAccountRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.put<AccountResponse>(`/accounts/${id}`, request)
      const index = accounts.value.findIndex(a => a.id === id)
      if (index !== -1) accounts.value[index] = response.data
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '更新帳戶失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function deleteAccount(id: number): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.delete(`/accounts/${id}`)
      accounts.value = accounts.value.filter(a => a.id !== id)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '刪除帳戶失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function transfer(request: TransferRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.post('/accounts/transfer', request)
      await fetchAccounts()
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '轉帳失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  return {
    accounts,
    loading,
    error,
    fetchAccounts,
    createAccount,
    updateAccount,
    deleteAccount,
    transfer,
  }
})
