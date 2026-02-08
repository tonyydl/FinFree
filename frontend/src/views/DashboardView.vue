<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import api from '@/api'
import { TransactionType } from '@/types'
import type { StatisticsResponse, TransactionResponse } from '@/types'
import { Doughnut, Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  ArcElement,
  BarElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Legend,
} from 'chart.js'

ChartJS.register(ArcElement, BarElement, CategoryScale, LinearScale, Tooltip, Legend)

const authStore = useAuthStore()
const statistics = ref<StatisticsResponse | null>(null)
const transactions = ref<TransactionResponse[]>([])
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    const [statsRes, txRes] = await Promise.all([
      api.get<StatisticsResponse>('/transactions/statistics'),
      api.get<TransactionResponse[]>('/transactions'),
    ])
    statistics.value = statsRes.data
    transactions.value = txRes.data
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

// 支出分佈圓餅圖資料
const CATEGORY_COLORS = [
  '#409eff', '#67c23a', '#e6a23c', '#f56c6c', '#909399',
  '#b37feb', '#36cfc9', '#ff85c0', '#ffc53d', '#597ef7',
]

const expenseByCategory = computed(() => {
  const map = new Map<string, number>()
  transactions.value
    .filter(t => t.type === TransactionType.Expense)
    .forEach(t => {
      const current = map.get(t.categoryName) ?? 0
      map.set(t.categoryName, current + t.amount)
    })
  return map
})

const doughnutData = computed(() => ({
  labels: [...expenseByCategory.value.keys()],
  datasets: [{
    data: [...expenseByCategory.value.values()],
    backgroundColor: CATEGORY_COLORS.slice(0, expenseByCategory.value.size),
  }],
}))

const doughnutOptions = {
  responsive: true,
  plugins: {
    legend: { position: 'bottom' as const },
  },
}

// 月收支趨勢長條圖資料
const monthlyData = computed(() => {
  const map = new Map<string, { income: number; expense: number }>()

  transactions.value.forEach(t => {
    const d = new Date(t.date)
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    if (!map.has(key)) map.set(key, { income: 0, expense: 0 })
    const entry = map.get(key)!
    if (t.type === TransactionType.Income) {
      entry.income += t.amount
    } else {
      entry.expense += t.amount
    }
  })

  // 按月份排序
  const sorted = [...map.entries()].sort((a, b) => a[0].localeCompare(b[0]))
  return sorted
})

const barData = computed(() => ({
  labels: monthlyData.value.map(([month]) => month),
  datasets: [
    {
      label: '收入',
      data: monthlyData.value.map(([, d]) => d.income),
      backgroundColor: '#67c23a',
    },
    {
      label: '支出',
      data: monthlyData.value.map(([, d]) => d.expense),
      backgroundColor: '#f56c6c',
    },
  ],
}))

const barOptions = {
  responsive: true,
  plugins: {
    legend: { position: 'bottom' as const },
  },
  scales: {
    y: { beginAtZero: true },
  },
}

const hasTransactions = computed(() => transactions.value.length > 0)
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

    <el-row v-if="hasTransactions" :gutter="20" style="margin-top: 20px">
      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>支出分佈</template>
          <div style="max-height: 300px; display: flex; justify-content: center">
            <Doughnut :data="doughnutData" :options="doughnutOptions" />
          </div>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>月收支趨勢</template>
          <Bar :data="barData" :options="barOptions" />
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>
