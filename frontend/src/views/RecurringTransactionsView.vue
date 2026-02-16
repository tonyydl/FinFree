<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useRecurringTransactionStore } from '@/stores/recurringTransaction'
import { TransactionType, RecurrenceFrequency } from '@/types'
import type { RecurringTransactionResponse } from '@/types'
import RecurringTransactionDialog from '@/components/RecurringTransactionDialog.vue'
import { ElMessage, ElMessageBox } from 'element-plus'

const store = useRecurringTransactionStore()

const dialogVisible = ref(false)
const editingItem = ref<RecurringTransactionResponse | null>(null)

// 篩選
const filterFrequency = ref<RecurrenceFrequency | ''>('')
const filterActive = ref<boolean | ''>('')

// 分頁
const currentPage = ref(1)
const pageSize = ref(10)

function fetchData() {
  store.fetchPaged({
    page: currentPage.value,
    pageSize: pageSize.value,
    frequency: filterFrequency.value !== '' ? filterFrequency.value : undefined,
    isActive: filterActive.value !== '' ? filterActive.value : undefined,
  })
}

onMounted(() => {
  fetchData()
})

watch([currentPage, pageSize], fetchData)

function handleFilterChange() {
  currentPage.value = 1
  fetchData()
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('zh-TW')
}

function formatFrequency(freq: RecurrenceFrequency): string {
  const map: Record<RecurrenceFrequency, string> = {
    [RecurrenceFrequency.Daily]: '每日',
    [RecurrenceFrequency.Weekly]: '每週',
    [RecurrenceFrequency.Monthly]: '每月',
    [RecurrenceFrequency.Yearly]: '每年',
  }
  return map[freq]
}

function handleAdd() {
  editingItem.value = null
  dialogVisible.value = true
}

function handleEdit(row: RecurringTransactionResponse) {
  editingItem.value = row
  dialogVisible.value = true
}

async function handleToggleActive(row: RecurringTransactionResponse) {
  const newActive = !row.isActive
  const success = await store.update(row.id, { isActive: newActive })
  if (success) {
    ElMessage.success(newActive ? '已啟用' : '已停用')
    fetchData()
  }
}

async function handleDelete(row: RecurringTransactionResponse) {
  try {
    await ElMessageBox.confirm(
      `確定要刪除「${row.categoryName}」的定期交易嗎？（$${row.amount.toLocaleString()} / ${formatFrequency(row.frequency)}）`,
      '確認刪除',
      {
        confirmButtonText: '刪除',
        cancelButtonText: '取消',
        type: 'warning',
      },
    )
    const success = await store.remove(row.id)
    if (success) {
      ElMessage.success('定期交易已刪除')
      fetchData()
    }
  } catch {
    // 使用者取消
  }
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 style="margin: 0">定期交易</h1>
      <el-button type="primary" @click="handleAdd">新增定期交易</el-button>
    </div>

    <!-- 篩選列 -->
    <el-card style="margin-bottom: 20px" shadow="never">
      <div class="filter-bar">
        <div class="filter-item">
          <span class="filter-label">頻率：</span>
          <el-select
            v-model="filterFrequency"
            placeholder="全部"
            clearable
            class="filter-select"
            @change="handleFilterChange"
          >
            <el-option label="每日" :value="RecurrenceFrequency.Daily" />
            <el-option label="每週" :value="RecurrenceFrequency.Weekly" />
            <el-option label="每月" :value="RecurrenceFrequency.Monthly" />
            <el-option label="每年" :value="RecurrenceFrequency.Yearly" />
          </el-select>
        </div>
        <div class="filter-item">
          <span class="filter-label">狀態：</span>
          <el-select
            v-model="filterActive"
            placeholder="全部"
            clearable
            class="filter-select"
            @change="handleFilterChange"
          >
            <el-option label="啟用" :value="true" />
            <el-option label="停用" :value="false" />
          </el-select>
        </div>
      </div>
    </el-card>

    <el-table
      v-loading="store.loading"
      :data="store.pagedItems"
      stripe
      style="width: 100%"
    >
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

      <el-table-column label="金額" width="140">
        <template #default="{ row }">
          <span :style="{ color: row.type === TransactionType.Income ? '#67c23a' : '#f56c6c', fontWeight: 'bold' }">
            ${{ row.amount.toLocaleString() }}
          </span>
        </template>
      </el-table-column>

      <el-table-column label="頻率" width="100">
        <template #default="{ row }">
          {{ formatFrequency(row.frequency) }}
        </template>
      </el-table-column>

      <el-table-column label="下次執行" width="130">
        <template #default="{ row }">
          {{ row.isActive ? formatDate(row.nextOccurrenceDate) : '--' }}
        </template>
      </el-table-column>

      <el-table-column label="結束日期" width="130">
        <template #default="{ row }">
          {{ row.endDate ? formatDate(row.endDate) : '永久' }}
        </template>
      </el-table-column>

      <el-table-column label="狀態" width="90">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
            {{ row.isActive ? '啟用' : '停用' }}
          </el-tag>
        </template>
      </el-table-column>

      <el-table-column prop="description" label="說明" show-overflow-tooltip />

      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" text size="small" @click="handleEdit(row)">編輯</el-button>
          <el-button :type="row.isActive ? 'warning' : 'success'" text size="small" @click="handleToggleActive(row)">
            {{ row.isActive ? '停用' : '啟用' }}
          </el-button>
          <el-button type="danger" text size="small" @click="handleDelete(row)">刪除</el-button>
        </template>
      </el-table-column>

      <template #empty>
        <el-empty description="尚無定期交易">
          <el-button type="primary" @click="handleAdd">新增第一筆定期交易</el-button>
        </el-empty>
      </template>
    </el-table>

    <!-- 分頁 -->
    <div v-if="store.total > 0" class="pagination-wrapper">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :page-sizes="[10, 20, 50]"
        :total="store.total"
        :small="true"
        layout="total, sizes, prev, pager, next"
      />
    </div>

    <RecurringTransactionDialog
      v-model:visible="dialogVisible"
      :editing-item="editingItem"
      @saved="fetchData"
    />
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

.filter-select {
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

  .filter-select {
    width: 100% !important;
    flex: 1;
  }
}
</style>
