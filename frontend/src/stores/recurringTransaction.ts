import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/api'
import type {
  RecurringTransactionResponse,
  CreateRecurringTransactionRequest,
  UpdateRecurringTransactionRequest,
} from '@/types'

export const useRecurringTransactionStore = defineStore('recurringTransaction', () => {
  const items = ref<RecurringTransactionResponse[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<RecurringTransactionResponse[]>('/recurring-transactions')
      items.value = response.data
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '取得定期交易失敗'
    } finally {
      loading.value = false
    }
  }

  async function create(request: CreateRecurringTransactionRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.post<RecurringTransactionResponse>('/recurring-transactions', request)
      await fetchAll()
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '新增定期交易失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function update(id: number, request: UpdateRecurringTransactionRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.put<RecurringTransactionResponse>(`/recurring-transactions/${id}`, request)
      await fetchAll()
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '更新定期交易失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function remove(id: number): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.delete(`/recurring-transactions/${id}`)
      await fetchAll()
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '刪除定期交易失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function executePending(): Promise<number> {
    try {
      const response = await api.post<{ createdCount: number }>('/recurring-transactions/execute')
      return response.data.createdCount
    } catch {
      return 0
    }
  }

  return {
    items,
    loading,
    error,
    fetchAll,
    create,
    update,
    remove,
    executePending,
  }
})
