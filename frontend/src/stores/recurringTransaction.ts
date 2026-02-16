import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/api'
import type {
  RecurringTransactionResponse,
  CreateRecurringTransactionRequest,
  UpdateRecurringTransactionRequest,
  PagedResult,
  RecurringTransactionQueryParams,
} from '@/types'

export const useRecurringTransactionStore = defineStore('recurringTransaction', () => {
  const items = ref<RecurringTransactionResponse[]>([])
  const pagedItems = ref<RecurringTransactionResponse[]>([])
  const total = ref(0)
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

  async function fetchPaged(params: RecurringTransactionQueryParams): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const query: Record<string, string | number | boolean> = {
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 10,
      }
      if (params.frequency !== undefined && params.frequency !== null) query.frequency = params.frequency
      if (params.isActive !== undefined && params.isActive !== null) query.isActive = params.isActive

      const response = await api.get<PagedResult<RecurringTransactionResponse>>('/recurring-transactions', { params: query })
      pagedItems.value = response.data.items
      total.value = response.data.total
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
      const response = await api.post<RecurringTransactionResponse>('/recurring-transactions', request)
      items.value.push(response.data)
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
      const response = await api.put<RecurringTransactionResponse>(`/recurring-transactions/${id}`, request)
      const index = items.value.findIndex(i => i.id === id)
      if (index !== -1) items.value[index] = response.data
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
      items.value = items.value.filter(i => i.id !== id)
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
    pagedItems,
    total,
    loading,
    error,
    fetchAll,
    fetchPaged,
    create,
    update,
    remove,
    executePending,
  }
})
