<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useTransactionStore } from '@/stores/transaction'
import { TransactionType } from '@/types'
import type { TransactionResponse } from '@/types'
import TransactionDialog from '@/components/TransactionDialog.vue'
import { ElMessage, ElMessageBox } from 'element-plus'

const transactionStore = useTransactionStore()

const dialogVisible = ref(false)
const editingTransaction = ref<TransactionResponse | null>(null)

onMounted(() => {
  transactionStore.fetchTransactions()
})

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
      <el-button type="primary" @click="handleAdd">新增交易</el-button>
    </div>

    <el-table
      v-loading="transactionStore.loading"
      :data="transactionStore.transactions"
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

    <TransactionDialog
      v-model:visible="dialogVisible"
      :editing-transaction="editingTransaction"
    />
  </div>
</template>
