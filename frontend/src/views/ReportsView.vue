<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import api from '@/api'
import type { MonthlyReportResponse, RangeReportResponse } from '@/types'
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
const reportMode = ref<'month' | 'range'>('month')
const rangeDates = ref<[string, string]>([
  `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`,
  now.toISOString().slice(0, 10),
])
const loading = ref(false)
const report = ref<MonthlyReportResponse | RangeReportResponse | null>(null)

async function fetchReport() {
  loading.value = true
  try {
    if (reportMode.value === 'month') {
      const res = await api.get<MonthlyReportResponse>('/transactions/report', {
        params: { year: selectedYear.value, month: selectedMonth.value },
      })
      report.value = res.data
    } else {
      const [startDate, endDate] = rangeDates.value
      const res = await api.get<RangeReportResponse>('/transactions/report/range', {
        params: { startDate, endDate },
      })
      report.value = res.data
    }
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

watch([reportMode, selectedYear, selectedMonth, rangeDates], fetchReport, { immediate: true })

function formatCurrency(value: number): string {
  return `$${value.toLocaleString()}`
}

const reportTitle = computed(() => {
  if (reportMode.value === 'month') return '月份報表'
  return '區間報表'
})

const reportPeriodLabel = computed(() => {
  if (reportMode.value === 'month') return `${selectedYear.value}年${selectedMonth.value}月`
  const [startDate, endDate] = rangeDates.value
  return `${startDate} 至 ${endDate}`
})

const normalizedReport = computed(() => report.value ? {
  totalIncome: report.value.totalIncome,
  totalExpense: report.value.totalExpense,
  balance: report.value.balance,
  transactionCount: report.value.transactionCount,
  expenseByCategory: report.value.expenseByCategory,
  incomeByCategory: report.value.incomeByCategory,
  dailyExpenses: report.value.dailyExpenses,
} : null)

function exportCsv() {
  if (!normalizedReport.value) return

  const rows: string[][] = [
    [reportTitle.value, reportPeriodLabel.value],
    [],
    ['摘要'],
    ['項目', '金額'],
    ['總收入', normalizedReport.value.totalIncome.toString()],
    ['總支出', normalizedReport.value.totalExpense.toString()],
    ['結餘', normalizedReport.value.balance.toString()],
    ['交易筆數', normalizedReport.value.transactionCount.toString()],
    [],
    ['支出分類'],
    ['分類', '金額', '佔比'],
    ...normalizedReport.value.expenseByCategory.map(c => [c.categoryName, c.amount.toString(), `${c.percentage}%`]),
    [],
    ['收入分類'],
    ['分類', '金額', '佔比'],
    ...normalizedReport.value.incomeByCategory.map(c => [c.categoryName, c.amount.toString(), `${c.percentage}%`]),
    [],
    ['每日收支'],
    ['日期', '收入', '支出'],
    ...normalizedReport.value.dailyExpenses.map(d => [d.date, d.income.toString(), d.expense.toString()]),
  ]

  const csv = '\uFEFF' + rows.map(r => r.map(cell => `"${cell}"`).join(',')).join('\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  const filePeriod = reportMode.value === 'month'
    ? `${selectedYear.value}${String(selectedMonth.value).padStart(2, '0')}`
    : `${rangeDates.value[0].split('-').join('')}-${rangeDates.value[1].split('-').join('')}`
  a.download = `FinFree_報表_${filePeriod}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

const chartTextColor = computed(() => themeStore.isDark ? '#E5EAF3' : '#606266')
const gridColor = computed(() => themeStore.isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)')

const COLORS = [
  '#f56c6c', '#e6a23c', '#409eff', '#67c23a', '#909399',
  '#b37feb', '#36cfc9', '#ff85c0', '#ffc53d', '#597ef7',
]

// 支出分類圓餅圖
const expenseDoughnutData = computed(() => ({
  labels: normalizedReport.value?.expenseByCategory.map(c => c.categoryName) ?? [],
  datasets: [{
    data: normalizedReport.value?.expenseByCategory.map(c => c.amount) ?? [],
    backgroundColor: COLORS.slice(0, normalizedReport.value?.expenseByCategory.length ?? 0),
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
          const item = normalizedReport.value?.expenseByCategory[ctx.dataIndex]
          return ` ${formatCurrency(ctx.raw)} (${item?.percentage ?? 0}%)`
        },
      },
    },
  },
}))

// 每日收支長條圖
const dailyBarData = computed(() => ({
  labels: normalizedReport.value?.dailyExpenses.map(d => d.date.slice(5)) ?? [],
  datasets: [
    {
      label: '收入',
      data: normalizedReport.value?.dailyExpenses.map(d => d.income) ?? [],
      backgroundColor: '#67c23a',
    },
    {
      label: '支出',
      data: normalizedReport.value?.dailyExpenses.map(d => d.expense) ?? [],
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
      <h1 style="margin: 0">{{ reportTitle }}</h1>
      <div class="report-controls">
        <el-radio-group v-model="reportMode">
          <el-radio-button label="month">月份</el-radio-button>
          <el-radio-button label="range">自訂區間</el-radio-button>
        </el-radio-group>

        <template v-if="reportMode === 'month'">
          <el-select v-model="selectedYear" style="width: 100px">
            <el-option v-for="y in yearOptions" :key="y" :value="y" :label="`${y} 年`" />
          </el-select>
          <el-select v-model="selectedMonth" style="width: 90px">
            <el-option v-for="m in monthOptions" :key="m.value" :value="m.value" :label="m.label" />
          </el-select>
        </template>

        <el-date-picker
          v-else
          v-model="rangeDates"
          type="daterange"
          range-separator="至"
          start-placeholder="開始日期"
          end-placeholder="結束日期"
          value-format="YYYY-MM-DD"
          :clearable="false"
          style="width: 260px"
        />

        <el-button :disabled="!normalizedReport || normalizedReport.transactionCount === 0" @click="exportCsv">匯出 CSV</el-button>
      </div>
    </div>

    <div v-loading="loading">
      <!-- 摘要卡片 -->
      <el-row :gutter="20" style="margin-bottom: 20px">
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>收入</template>
            <p style="font-size: 22px; color: #67c23a; margin: 0">
              {{ formatCurrency(normalizedReport?.totalIncome ?? 0) }}
            </p>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>支出</template>
            <p style="font-size: 22px; color: #f56c6c; margin: 0">
              {{ formatCurrency(normalizedReport?.totalExpense ?? 0) }}
            </p>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>結餘</template>
            <p :style="{ fontSize: '22px', color: (normalizedReport?.balance ?? 0) >= 0 ? '#67c23a' : '#f56c6c', margin: '0' }">
              {{ formatCurrency(normalizedReport?.balance ?? 0) }}
            </p>
          </el-card>
        </el-col>
        <el-col :xs="12" :sm="6" style="margin-bottom: 12px">
          <el-card shadow="hover">
            <template #header>交易筆數</template>
            <p style="font-size: 22px; color: #409eff; margin: 0">
              {{ normalizedReport?.transactionCount ?? 0 }} 筆
            </p>
          </el-card>
        </el-col>
      </el-row>

      <template v-if="normalizedReport && normalizedReport.transactionCount > 0">
        <!-- 每日收支趨勢 -->
        <el-card v-if="normalizedReport.dailyExpenses.length > 0" shadow="hover" style="margin-bottom: 20px">
          <template #header>每日收支趨勢</template>
          <Bar :data="dailyBarData" :options="barOptions" style="max-height: 280px" />
        </el-card>

        <el-row :gutter="20">
          <!-- 支出分類圓餅圖 -->
          <el-col :xs="24" :sm="12" style="margin-bottom: 20px">
            <el-card shadow="hover" style="height: 100%">
              <template #header>支出分類</template>
              <div v-if="normalizedReport.expenseByCategory.length > 0">
                <div style="display: flex; justify-content: center; margin-bottom: 16px">
                  <Doughnut :data="expenseDoughnutData" :options="doughnutOptions" style="max-height: 220px" />
                </div>
                <div class="category-list">
                  <div
                    v-for="(item, i) in normalizedReport.expenseByCategory"
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
              <div v-if="normalizedReport.incomeByCategory.length > 0" class="category-list">
                <div
                  v-for="(item, i) in normalizedReport.incomeByCategory"
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

      <el-empty v-else-if="!loading" description="此期間尚無交易記錄" />
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

.report-controls {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
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
