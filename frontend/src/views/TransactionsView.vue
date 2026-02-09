<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useTransactionStore } from '@/stores/transaction'
import { TransactionType } from '@/types'
import type { TransactionResponse } from '@/types'
import TransactionDialog from '@/components/TransactionDialog.vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'

const transactionStore = useTransactionStore()

const dialogVisible = ref(false)
const editingTransaction = ref<TransactionResponse | null>(null)

// 篩選條件
const filterType = ref<TransactionType | ''>('')
const filterDateRange = ref<[string, string] | null>(null)

// 分頁
const currentPage = ref(1)
const pageSize = ref(10)

onMounted(() => {
  transactionStore.fetchTransactions()
})

// 篩選後的資料
const filteredTransactions = computed(() => {
  let result = transactionStore.transactions

  // 類型篩選
  if (filterType.value !== '') {
    result = result.filter(t => t.type === filterType.value)
  }

  // 日期範圍篩選
  if (filterDateRange.value) {
    const [start, end] = filterDateRange.value
    const startDate = new Date(start)
    const endDate = new Date(end)
    endDate.setHours(23, 59, 59, 999)
    result = result.filter(t => {
      const d = new Date(t.date)
      return d >= startDate && d <= endDate
    })
  }

  return result
})

// 分頁後的資料
const paginatedTransactions = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return filteredTransactions.value.slice(start, start + pageSize.value)
})

const totalCount = computed(() => filteredTransactions.value.length)

function handleFilterChange() {
  currentPage.value = 1
}

function clearFilters() {
  filterType.value = ''
  filterDateRange.value = null
  currentPage.value = 1
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('zh-TW')
}

function formatAmount(amount: number, type: TransactionType): string {
  const prefix = type === TransactionType.Income ? '+' : '-'
  return `${prefix} $${amount.toLocaleString()}`
}

function handleAdd() {
  editingTransaction.value = null
  dialogVisible.value = true
}

function handleEdit(row: TransactionResponse) {
  editingTransaction.value = row
  dialogVisible.value = true
}

const exporting = ref(false)

async function handleExport() {
  exporting.value = true
  try {
    const response = await api.get('/transactions/export', { responseType: 'blob' })
    const blob = new Blob([response.data], { type: 'text/csv;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    const today = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    link.download = `FinFree_交易記錄_${today}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('匯出成功')
  } catch {
    ElMessage.error('匯出失敗')
  } finally {
    exporting.value = false
  }
}

async function handleDelete(row: TransactionResponse) {
  try {
    await ElMessageBox.confirm(
      `確定要刪除這筆${row.type === TransactionType.Income ? '收入' : '支出'}記錄嗎？（${row.categoryName} $${row.amount.toLocaleString()}）`,
      '確認刪除',
      {
        confirmButtonText: '刪除',
        cancelButtonText: '取消',
        type: 'warning',
      },
    )
    const success = await transactionStore.deleteTransaction(row.id)
    if (success) {
      ElMessage.success('交易記錄已刪除')
    }
  } catch {
    // 使用者取消刪除，不需處理
  }
}
</script>

<template>
  <div>
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px">
      <h1 style="margin: 0">交易記錄</h1>
      <div style="display: flex; gap: 8px">
        <el-button :loading="exporting" @click="handleExport">匯出 CSV</el-button>
        <el-button type="primary" @click="handleAdd">新增交易</el-button>
      </div>
    </div>

    <!-- 篩選列 -->
    <el-card style="margin-bottom: 20px" shadow="never">
      <div style="display: flex; align-items: center; gap: 16px; flex-wrap: wrap">
        <div style="display: flex; align-items: center; gap: 8px">
          <span>類型：</span>
          <el-select
            v-model="filterType"
            placeholder="全部"
            clearable
            style="width: 120px"
            @change="handleFilterChange"
          >
            <el-option label="收入" :value="TransactionType.Income" />
            <el-option label="支出" :value="TransactionType.Expense" />
          </el-select>
        </div>
        <div style="display: flex; align-items: center; gap: 8px">
          <span>日期：</span>
          <el-date-picker
            v-model="filterDateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="開始日期"
            end-placeholder="結束日期"
            value-format="YYYY-MM-DD"
            @change="handleFilterChange"
          />
        </div>
        <el-button text @click="clearFilters">清除篩選</el-button>
      </div>
    </el-card>

    <el-table
      v-loading="transactionStore.loading"
      :data="paginatedTransactions"
      stripe
      style="width: 100%"
    >
      <el-table-column label="日期" width="120">
        <template #default="{ row }">
          {{ formatDate(row.date) }}
        </template>
      </el-table-column>

      <el-table-column label="類型" width="100">
        <template #default="{ row }">
          <el-tag :type="row.type === TransactionType.Income ? 'success' : 'danger'" size="small">
            {{ row.type === TransactionType.Income ? '收入' : '支出' }}
          </el-tag>
        </template>
      </el-table-column>

      <el-table-column prop="categoryName" label="分類" width="120" />

      <el-table-column label="金額" width="150">
        <template #default="{ row }">
          <span :style="{ color: row.type === TransactionType.Income ? '#67c23a' : '#f56c6c', fontWeight: 'bold' }">
            {{ formatAmount(row.amount, row.type) }}
          </span>
        </template>
      </el-table-column>

      <el-table-column prop="description" label="說明" show-overflow-tooltip />

      <el-table-column label="操作" width="150" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" text size="small" @click="handleEdit(row)">編輯</el-button>
          <el-button type="danger" text size="small" @click="handleDelete(row)">刪除</el-button>
        </template>
      </el-table-column>

      <template #empty>
        <el-empty description="尚無交易記錄">
          <el-button type="primary" @click="handleAdd">新增第一筆交易</el-button>
        </el-empty>
      </template>
    </el-table>

    <!-- 分頁 -->
    <div v-if="totalCount > 0" style="display: flex; justify-content: flex-end; margin-top: 16px">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :page-sizes="[10, 20, 50]"
        :total="totalCount"
        layout="total, sizes, prev, pager, next"
      />
    </div>

    <TransactionDialog
      v-model:visible="dialogVisible"
      :editing-transaction="editingTransaction"
    />
  </div>
</template>
