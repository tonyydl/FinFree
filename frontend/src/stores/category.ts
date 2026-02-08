import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/api'
import type {
  CategoryResponse,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@/types'

export const useCategoryStore = defineStore('category', () => {
  const categories = ref<CategoryResponse[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchCategories(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<CategoryResponse[]>('/categories')
      categories.value = response.data
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '取得分類失敗'
    } finally {
      loading.value = false
    }
  }

  async function createCategory(request: CreateCategoryRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.post<CategoryResponse>('/categories', request)
      await fetchCategories()
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '新增分類失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function updateCategory(id: number, request: UpdateCategoryRequest): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.put<CategoryResponse>(`/categories/${id}`, request)
      await fetchCategories()
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '更新分類失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  async function deleteCategory(id: number): Promise<boolean> {
    loading.value = true
    error.value = null
    try {
      await api.delete(`/categories/${id}`)
      await fetchCategories()
      return true
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      error.value = axiosErr.response?.data?.message ?? '刪除分類失敗'
      return false
    } finally {
      loading.value = false
    }
  }

  return {
    categories,
    loading,
    error,
    fetchCategories,
    createCategory,
    updateCategory,
    deleteCategory,
  }
})
