import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/api'
import type {
  TransactionResponse,
  CreateTransactionRequest,
  UpdateTransactionRequest,
} from '@/types'

export const useTransactionStore = defineStore('transaction', () => {
  const transactions = ref<TransactionResponse[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchTransactions(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<TransactionResponse[]>('/transactions')
      transactions.value = response.data
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '取得交易記錄失敗'
    } finally {
      loading.value = false
    }
  }

  async function createTransaction(request: CreateTransactionRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<TransactionResponse>('/transactions', request)
      transactions.value.unshift(response.data)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '新增交易記錄失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function updateTransaction(id: number, request: UpdateTransactionRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.put<TransactionResponse>(`/transactions/${id}`, request)
      const index = transactions.value.findIndex(t => t.id === id)
      if (index !== -1) transactions.value[index] = response.data
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '更新交易記錄失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function deleteTransaction(id: number): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.delete(`/transactions/${id}`)
      transactions.value = transactions.value.filter(t => t.id !== id)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '刪除交易記錄失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  return {
    transactions,
    loading,
    error,
    fetchTransactions,
    createTransaction,
    updateTransaction,
    deleteTransaction,
  }
})
