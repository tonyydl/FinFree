<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import api from '@/api'
import type { MonthlyReportResponse } from '@/types'
import { useThemeStore } from '@/stores/theme'
import { Bar, Doughnut } from 'vue-chartjs'
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

const themeStore = useThemeStore()

const now = new Date()
const selectedYear = ref(now.getFullYear())
const selectedMonth = ref(now.getMonth() + 1)
const loading = ref(false)
const report = ref<MonthlyReportResponse | null>(null)

async function fetchReport() {
  loading.value = true
  try {
    const res = await api.get<MonthlyReportResponse>('/transactions/report', {
      params: { year: selectedYear.value, month: selectedMonth.value },
    })
    report.value = res.data
  } catch {
    // 錯誤由 Axios 攔截器處理
  } finally {
    loading.value = false
  }
}

// 月份選項
const monthOptions = Array.from({ length: 12 }, (_, i) => ({ value: i + 1, label: `${i + 1} 月` }))

// 年份選項（近 5 年）
const yearOptions = Array.from({ length: 5 }, (_, i) => now.getFullYear() - i)

watch([selectedYear, selectedMonth], fetchReport, { immediate: true })

function formatCurrency(value: number): string {
  return `$${value.toLocaleString()}`
}

const chartTextColor = computed(() => themeStore.isDark ? '#E5EAF3' : '#606266')
const gridColor = computed(() => themeStore.isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)')

const COLORS = [
  '#f56c6c', '#e6a23c', '#409eff', '#67c23a', '#909399',
  '#b37feb', '#36cfc9', '#ff85c0', '#ffc53d', '#597ef7',
]

// 支出分類圓餅圖
const expenseDoughnutData = computed(() => ({
  labels: report.value?.expenseByCategory.map(c => c.categoryName) ?? [],
  datasets: [{
    data: report.value?.expenseByCategory.map(c => c.amount) ?? [],
    backgroundColor: COLORS.slice(0, report.value?.expenseByCategory.length ?? 0),
  }],
}))

const doughnutOptions = computed(() => ({
  responsive: true,
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: { color: chartTextColor.value, font: { size: 12 } },
    },
    tooltip: {
      callbacks: {
        label: (ctx: any) => {
          const item = report.value?.expenseByCategory[ctx.dataIndex]
          return ` ${formatCurrency(ctx.raw)} (${item?.percentage ?? 0}%)`
        },
      },
    },
  },
}))

// 每日收支長條圖
const dailyBarData = computed(() => ({
  labels: report.value?.dailyExpenses.map(d => d.date.slice(5)) ?? [],
  datasets: [
    {
      label: '收入',
      data: report.value?.dailyExpenses.map(d => d.income) ?? [],
      backgroundColor: '#67c23a',
    },
    {
      label: '支出',
      data: report.value?.dailyExpenses.map(d => d.expense) ?? [],
      backgroundColor: '#f56c6c',
    },
  ],
}))

const barOptions = computed(() => ({
  responsive: true,
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: { color: chartTextColor.value },
    },
  },
  scales: {
    x: {
      ticks: { color: chartTextColor.value, maxRotation: 45 },
      grid: { color: gridColor.value },
    },
    y: {
      beginAtZero: true,
      ticks: { color: chartTextColor.value },
      grid: { color: gridColor.value },
    },
  },
}))
</script>

<template>
  <div>
    <div class="page-header">
      <h1 style="margin: 0">月份報表</h1>
      <div style="display: flex; gap: 8px; align-items: center">
        <el-select v-model="selectedYear" style="width: 100px">
          <el-option v-for="y in yearOptions" :key="y" :value="y" :label="`${y} 年`" />
        </el-select>
        <el-select v-model="selectedMonth" style="width: 90px">
          <el-option v-for="m in monthOptions" :key="m.value" :value="m.value" :label="m.label" />
        </el-select>
      </div>
    </div>

    <div v-loading="loading">
      <!-- 摘要卡片 -->
      <el-row :gutter="20" style="margin-bottom: 20px">
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>收入</template>
            <p style="font-size: 22px; color: #67c23a; margin: 0">
              {{ formatCurrency(report?.totalIncome ?? 0) }}
            </p>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>支出</template>
            <p style="font-size: 22px; color: #f56c6c; margin: 0">
              {{ formatCurrency(report?.totalExpense ?? 0) }}
            </p>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>結餘</template>
            <p :style="{ fontSize: '22px', color: (report?.balance ?? 0) >= 0 ? '#67c23a' : '#f56c6c', margin: '0' }">
              {{ formatCurrency(report?.balance ?? 0) }}
            </p>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>交易筆數</template>
            <p style="font-size: 22px; color: #409eff; margin: 0">
              {{ report?.transactionCount ?? 0 }} 筆
            </p>
          </el-card>
        </el-col>
      </el-row>

      <template v-if="report && report.transactionCount > 0">
        <!-- 每日收支趨勢 -->
        <el-card v-if="report.dailyExpenses.length > 0" shadow="hover" style="margin-bottom: 20px">
          <template #header>每日收支趨勢</template>
          <Bar :data="dailyBarData" :options="barOptions" style="max-height: 280px" />
        </el-card>

        <el-row :gutter="20">
          <!-- 支出分類圓餅圖 -->
          <el-col :xs="24" :sm="12" style="margin-bottom: 20px">
            <el-card shadow="hover" style="height: 100%">
              <template #header>支出分類</template>
              <div v-if="report.expenseByCategory.length > 0">
                <div style="display: flex; justify-content: center; margin-bottom: 16px">
                  <Doughnut :data="expenseDoughnutData" :options="doughnutOptions" style="max-height: 220px" />
                </div>
                <div class="category-list">
                  <div
                    v-for="(item, i) in report.expenseByCategory"
                    :key="item.categoryName"
                    class="category-item"
                  >
                    <div class="category-left">
                      <span class="color-dot" :style="{ background: COLORS[i] }" />
                      <span>{{ item.categoryName }}</span>
                    </div>
                    <div class="category-right">
                      <span style="font-weight: bold">{{ formatCurrency(item.amount) }}</span>
                      <span style="color: #909399; font-size: 12px">{{ item.percentage }}%</span>
                    </div>
                  </div>
                </div>
              </div>
              <el-empty v-else description="本月無支出" :image-size="60" />
            </el-card>
          </el-col>

          <!-- 收入分類 -->
          <el-col :xs="24" :sm="12" style="margin-bottom: 20px">
            <el-card shadow="hover" style="height: 100%">
              <template #header>收入分類</template>
              <div v-if="report.incomeByCategory.length > 0" class="category-list">
                <div
                  v-for="(item, i) in report.incomeByCategory"
                  :key="item.categoryName"
                  class="category-item"
                >
                  <div class="category-left">
                    <span class="color-dot" :style="{ background: COLORS[i] }" />
                    <span>{{ item.categoryName }}</span>
                  </div>
                  <div class="category-right">
                    <span style="font-weight: bold; color: #67c23a">{{ formatCurrency(item.amount) }}</span>
                    <span style="color: #909399; font-size: 12px">{{ item.percentage }}%</span>
                  </div>
                </div>
              </div>
              <el-empty v-else description="本月無收入" :image-size="60" />
            </el-card>
          </el-col>
        </el-row>
      </template>

      <el-empty v-else-if="!loading" description="本月尚無交易記錄" />
    </div>
  </div>
</template>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  flex-wrap: wrap;
  gap: 12px;
}

.category-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.category-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.category-item:last-child {
  border-bottom: none;
}

.category-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.color-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  flex-shrink: 0;
}

.category-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
}
</style>
