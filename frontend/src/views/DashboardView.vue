<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useBudgetStore } from '@/stores/budget'
import api from '@/api'
import { TransactionType } from '@/types'
import type { StatisticsResponse, TransactionResponse, BudgetResponse } from '@/types'
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
const budgetStore = useBudgetStore()
const statistics = ref<StatisticsResponse | null>(null)
const transactions = ref<TransactionResponse[]>([])
const loading = ref(false)

const now = new Date()
const currentYear = now.getFullYear()
const currentMonth = now.getMonth() + 1

onMounted(async () => {
  loading.value = true
  try {
    const [statsRes, txRes] = await Promise.all([
      api.get<StatisticsResponse>('/transactions/statistics'),
      api.get<TransactionResponse[]>('/transactions'),
      budgetStore.fetchBudgets(currentYear, currentMonth),
    ])
    statistics.value = statsRes.data
    transactions.value = txRes.data
  } catch {
    // 錯誤由 Axios 攔截器處理
  } finally {
    loading.value = false
  }
})

// 最近 5 筆交易
const recentTransactions = computed(() => transactions.value.slice(0, 5))

// 預算摘要
function getBudgetPercentage(b: BudgetResponse): number {
  if (b.amount === 0) return 0
  return Math.min(Math.round((b.spent / b.amount) * 100), 100)
}

function getBudgetStatus(b: BudgetResponse): '' | 'success' | 'warning' | 'exception' {
  const pct = b.amount === 0 ? 0 : (b.spent / b.amount) * 100
  if (pct >= 100) return 'exception'
  if (pct >= 80) return 'warning'
  return 'success'
}

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
      <el-col :xs="24" :sm="8" style="margin-bottom: 12px">
        <el-card shadow="hover">
          <template #header>總收入</template>
          <p style="font-size: 24px; color: #67c23a; margin: 0">
            {{ formatCurrency(statistics?.totalIncome) }}
          </p>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" style="margin-bottom: 12px">
        <el-card shadow="hover">
          <template #header>總支出</template>
          <p style="font-size: 24px; color: #f56c6c; margin: 0">
            {{ formatCurrency(statistics?.totalExpense) }}
          </p>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="8" style="margin-bottom: 12px">
        <el-card shadow="hover">
          <template #header>餘額</template>
          <p style="font-size: 24px; color: #409eff; margin: 0">
            {{ formatCurrency(statistics?.balance) }}
          </p>
        </el-card>
      </el-col>
    </el-row>

    <!-- 本月預算摘要 -->
    <el-card v-if="budgetStore.budgets.length > 0" shadow="hover" style="margin-top: 20px; margin-bottom: 20px">
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center">
          <span>本月預算 ({{ currentYear }}/{{ currentMonth }})</span>
          <el-button text type="primary" @click="$router.push('/budgets')">查看全部</el-button>
        </div>
      </template>
      <div class="budget-summary">
        <div v-for="b in budgetStore.budgets" :key="b.id" class="budget-summary-item">
          <div class="budget-summary-header">
            <span>{{ b.categoryName ?? '整體預算' }}</span>
            <span :class="{ 'over-budget': b.spent > b.amount }">
              ${{ b.spent.toLocaleString() }} / ${{ b.amount.toLocaleString() }}
            </span>
          </div>
          <el-progress
            :percentage="getBudgetPercentage(b)"
            :status="getBudgetStatus(b)"
            :stroke-width="12"
            :text-inside="true"
          />
        </div>
      </div>
    </el-card>

    <el-row v-if="hasTransactions" :gutter="20" style="margin-top: 8px">
      <el-col :xs="24" :sm="12" style="margin-bottom: 12px">
        <el-card shadow="hover">
          <template #header>支出分佈</template>
          <div style="max-height: 300px; display: flex; justify-content: center">
            <Doughnut :data="doughnutData" :options="doughnutOptions" />
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" style="margin-bottom: 12px">
        <el-card shadow="hover">
          <template #header>月收支趨勢</template>
          <Bar :data="barData" :options="barOptions" />
        </el-card>
      </el-col>
    </el-row>

    <!-- 最近交易 -->
    <el-card v-if="recentTransactions.length > 0" shadow="hover" style="margin-top: 8px">
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center">
          <span>最近交易</span>
          <el-button text type="primary" @click="$router.push('/transactions')">查看全部</el-button>
        </div>
      </template>
      <div class="recent-list">
        <div v-for="t in recentTransactions" :key="t.id" class="recent-item">
          <div class="recent-left">
            <span class="recent-category">{{ t.categoryName }}</span>
            <span class="recent-desc">{{ t.description || '--' }}</span>
          </div>
          <div class="recent-right">
            <span :style="{ color: t.type === TransactionType.Income ? '#67c23a' : '#f56c6c', fontWeight: 'bold' }">
              {{ t.type === TransactionType.Income ? '+' : '-' }} ${{ t.amount.toLocaleString() }}
            </span>
            <span class="recent-date">{{ new Date(t.date).toLocaleDateString('zh-TW') }}</span>
          </div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.budget-summary {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.budget-summary-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
  font-size: 14px;
}

.over-budget {
  color: #f56c6c;
  font-weight: bold;
}

.recent-list {
  display: flex;
  flex-direction: column;
}

.recent-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
}

.recent-item:last-child {
  border-bottom: none;
}

.recent-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.recent-category {
  font-weight: bold;
  font-size: 14px;
}

.recent-desc {
  font-size: 12px;
  color: #909399;
}

.recent-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
}

.recent-date {
  font-size: 12px;
  color: #909399;
}
</style>
