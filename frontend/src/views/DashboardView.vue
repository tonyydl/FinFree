<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import api from '@/api'
import type { StatisticsResponse } from '@/types'

const authStore = useAuthStore()
const statistics = ref<StatisticsResponse | null>(null)
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    const response = await api.get<StatisticsResponse>('/transactions/statistics')
    statistics.value = response.data
  } catch {
    // 錯誤由 Axios 攔截器處理
  } finally {
    loading.value = false
  }
})

function formatCurrency(value: number | undefined): string {
  if (value === undefined) return '--'
  return `$${value.toLocaleString()}`
}
</script>

<template>
  <div>
    <h1>歡迎回來，{{ authStore.username }}</h1>

    <el-row :gutter="20" style="margin-top: 20px" v-loading="loading">
      <el-col :span="8">
        <el-card shadow="hover">
          <template #header>總收入</template>
          <p style="font-size: 24px; color: #67c23a; margin: 0">
            {{ formatCurrency(statistics?.totalIncome) }}
          </p>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover">
          <template #header>總支出</template>
          <p style="font-size: 24px; color: #f56c6c; margin: 0">
            {{ formatCurrency(statistics?.totalExpense) }}
          </p>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover">
          <template #header>餘額</template>
          <p style="font-size: 24px; color: #409eff; margin: 0">
            {{ formatCurrency(statistics?.balance) }}
          </p>
        </el-card>
      </el-col>
    </el-row>

    <el-card style="margin-top: 20px" shadow="hover">
      <template #header>交易筆數</template>
      <p style="font-size: 24px; margin: 0">
        {{ statistics?.transactionCount ?? '--' }} 筆
      </p>
    </el-card>
  </div>
</template>
