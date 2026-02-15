import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/api'
import type {
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from '@/types'

export const useBudgetStore = defineStore('budget', () => {
  const budgets = ref<BudgetResponse[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchBudgets(year: number, month: number): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<BudgetResponse[]>(`/budgets/${year}/${month}`)
      budgets.value = response.data
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '取得預算失敗'
    } finally {
      loading.value = false
    }
  }

  async function createBudget(request: CreateBudgetRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<BudgetResponse>('/budgets', request)
      budgets.value.push(response.data)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '新增預算失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function updateBudget(id: number, request: UpdateBudgetRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      const response = await api.put<BudgetResponse>(`/budgets/${id}`, request)
      const index = budgets.value.findIndex(b => b.id === id)
      if (index !== -1) budgets.value[index] = response.data
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '更新預算失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function deleteBudget(id: number): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.delete(`/budgets/${id}`)
      budgets.value = budgets.value.filter(b => b.id !== id)
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '刪除預算失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  return {
    budgets,
    loading,
    error,
    fetchBudgets,
    createBudget,
    updateBudget,
    deleteBudget,
  }
})
