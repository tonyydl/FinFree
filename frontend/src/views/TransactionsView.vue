<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useTransactionStore } from '@/stores/transaction'
import { useAccountStore } from '@/stores/account'
import { TransactionType } from '@/types'
import type { TransactionResponse } from '@/types'
import TransactionDialog from '@/components/TransactionDialog.vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'

const transactionStore = useTransactionStore()
const accountStore = useAccountStore()

const dialogVisible = ref(false)
const editingTransaction = ref<TransactionResponse | null>(null)

// 篩選條件
const searchKeyword = ref('')
const filterType = ref<TransactionType | ''>('')
const filterAccountId = ref<number | ''>('')
const filterStartDate = ref<string | null>(null)
const filterEndDate = ref<string | null>(null)

// 分頁
const currentPage = ref(1)
const pageSize = ref(10)

onMounted(() => {
  transactionStore.fetchTransactions()
  accountStore.fetchAccounts()
})

// 篩選後的資料
const filteredTransactions = computed(() => {
  let result = transactionStore.transactions

  // 關鍵字搜尋（說明、分類名稱）
  if (searchKeyword.value.trim()) {
    const keyword = searchKeyword.value.trim().toLowerCase()
    result = result.filter(t =>
      (t.description && t.description.toLowerCase().includes(keyword)) ||
      t.categoryName.toLowerCase().includes(keyword),
    )
  }

  // 類型篩選
  if (filterType.value !== '') {
    result = result.filter(t => t.type === filterType.value)
  }

  // 帳戶篩選
  if (filterAccountId.value !== '') {
    result = result.filter(t => t.accountId === filterAccountId.value)
  }

  // 日期範圍篩選
  const start = filterStartDate.value
  const end = filterEndDate.value
  if (start || end) {
    result = result.filter(t => {
      const d = new Date(t.date)
      if (start) {
        const startDate = new Date(start)
        if (d < startDate) return false
      }
      if (end) {
        const endDate = new Date(end)
        endDate.setHours(23, 59, 59, 999)
        if (d > endDate) return false
      }
      return true
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

function handleStartDateChange() {
  if (filterStartDate.value && filterEndDate.value && filterStartDate.value > filterEndDate.value) {
    ElMessage.warning('開始日期不能晚於結束日期')
    filterStartDate.value = null
    return
  }
  handleFilterChange()
}

function handleEndDateChange() {
  if (filterStartDate.value && filterEndDate.value && filterEndDate.value < filterStartDate.value) {
    ElMessage.warning('結束日期不能早於開始日期')
    filterEndDate.value = null
    return
  }
  handleFilterChange()
}

function clearFilters() {
  searchKeyword.value = ''
  filterType.value = ''
  filterAccountId.value = ''
  filterStartDate.value = null
  filterEndDate.value = null
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

// 匯入
const importVisible = ref(false)
const importAccountId = ref<number | null>(null)
const importFile = ref<File | null>(null)
const importing = ref(false)

function handleImportOpen() {
  importAccountId.value = null
  importFile.value = null
  importVisible.value = true
}

function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  importFile.value = target.files?.[0] ?? null
}

async function handleImportSubmit() {
  if (!importFile.value) {
    ElMessage.warning('請選擇 CSV 檔案')
    return
  }
  if (!importAccountId.value) {
    ElMessage.warning('請選擇匯入帳戶')
    return
  }

  importing.value = true
  try {
    const formData = new FormData()
    formData.append('file', importFile.value)
    formData.append('accountId', String(importAccountId.value))

    const res = await api.post<{ imported: number; failed: number; errors: string[] }>(
      '/transactions/import',
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    )
    const { imported, failed, errors } = res.data
    if (imported > 0) {
      ElMessage.success(`匯入成功 ${imported} 筆${failed > 0 ? `，失敗 ${failed} 筆` : ''}`)
      transactionStore.fetchTransactions()
      importVisible.value = false
    }
    if (failed > 0 && imported === 0) {
      ElMessage.error(`匯入失敗：${errors[0]}`)
    }
  } catch {
    // 錯誤由 Axios 攔截器處理
  } finally {
    importing.value = false
  }
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
    <div class="page-header">
      <h1 style="margin: 0">交易記錄</h1>
      <div style="display: flex; gap: 8px">
        <el-button @click="handleImportOpen">匯入 CSV</el-button>
        <el-button :loading="exporting" @click="handleExport">匯出 CSV</el-button>
        <el-button type="primary" @click="handleAdd">新增交易</el-button>
      </div>
    </div>

    <!-- 篩選列 -->
    <el-card style="margin-bottom: 20px" shadow="never">
      <div class="filter-bar">
        <el-input
          v-model="searchKeyword"
          placeholder="搜尋說明或分類"
          clearable
          class="filter-search"
          @input="handleFilterChange"
        />
        <div class="filter-item">
          <span class="filter-label">類型：</span>
          <el-select
            v-model="filterType"
            placeholder="全部"
            clearable
            class="filter-type"
            @change="handleFilterChange"
          >
            <el-option label="收入" :value="TransactionType.Income" />
            <el-option label="支出" :value="TransactionType.Expense" />
          </el-select>
        </div>
        <div class="filter-item">
          <span class="filter-label">帳戶：</span>
          <el-select
            v-model="filterAccountId"
            placeholder="全部"
            clearable
            class="filter-type"
            @change="handleFilterChange"
          >
            <el-option
              v-for="acc in accountStore.accounts"
              :key="acc.id"
              :label="acc.name"
              :value="acc.id"
            />
          </el-select>
        </div>
        <div class="filter-item">
          <span class="filter-label">開始：</span>
          <el-date-picker
            v-model="filterStartDate"
            type="date"
            placeholder="開始日期"
            value-format="YYYY-MM-DD"
            clearable
            class="filter-date"
            @change="handleStartDateChange"
          />
        </div>
        <div class="filter-item">
          <span class="filter-label">結束：</span>
          <el-date-picker
            v-model="filterEndDate"
            type="date"
            placeholder="結束日期"
            value-format="YYYY-MM-DD"
            clearable
            class="filter-date"
            @change="handleEndDateChange"
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

      <el-table-column label="帳戶" width="120">
        <template #default="{ row }">
          {{ row.accountName ?? '--' }}
        </template>
      </el-table-column>

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
    <div v-if="totalCount > 0" class="pagination-wrapper">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :page-sizes="[10, 20, 50]"
        :total="totalCount"
        :small="true"
        layout="total, sizes, prev, pager, next"
      />
    </div>

    <TransactionDialog
      v-model:visible="dialogVisible"
      :editing-transaction="editingTransaction"
    />

    <!-- 匯入對話框 -->
    <el-dialog
      v-model="importVisible"
      title="匯入 CSV"
      width="420px"
    >
      <div style="margin-bottom: 16px; color: #909399; font-size: 13px">
        支援從 FinFree 匯出的 CSV 格式（日期、類型、分類、金額、備註）。
      </div>
      <el-form label-position="top">
        <el-form-item label="匯入至帳戶">
          <el-select
            v-model="importAccountId"
            placeholder="請選擇帳戶"
            style="width: 100%"
          >
            <el-option
              v-for="acc in accountStore.accounts"
              :key="acc.id"
              :label="acc.name"
              :value="acc.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="CSV 檔案">
          <input
            type="file"
            accept=".csv"
            style="width: 100%"
            @change="handleFileChange"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="importVisible = false">取消</el-button>
        <el-button type="primary" :loading="importing" @click="handleImportSubmit">
          開始匯入
        </el-button>
      </template>
    </el-dialog>
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

.filter-bar {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.filter-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.filter-label {
  white-space: nowrap;
}

.filter-search {
  width: 200px;
}

.filter-type {
  width: 120px;
}

.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
  overflow-x: auto;
}

@media (max-width: 767px) {
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
  }

  .filter-item {
    width: 100%;
  }

  .filter-search,
  .filter-type,
  .filter-date {
    width: 100% !important;
    flex: 1;
  }
}
</style>
