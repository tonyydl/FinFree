import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/api'
import type { CategoryResponse } from '@/types'

export const useCategoryStore = defineStore('category', () => {
  const categories = ref<CategoryResponse[]>([])
  const loading = ref(false)

  async function fetchCategories(): Promise<void> {
    loading.value = true
    try {
      const response = await api.get<CategoryResponse[]>('/categories')
      categories.value = response.data
    } catch {
      // 錯誤由 Axios 攔截器處理
    } finally {
      loading.value = false
    }
  }

  return {
    categories,
    loading,
    fetchCategories,
  }
})
