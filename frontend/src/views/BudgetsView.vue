<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useBudgetStore } from '@/stores/budget'
import type { BudgetResponse } from '@/types'
import BudgetDialog from '@/components/BudgetDialog.vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowLeft, ArrowRight } from '@element-plus/icons-vue'

const budgetStore = useBudgetStore()

const now = new Date()
const currentYear = ref(now.getFullYear())
const currentMonth = ref(now.getMonth() + 1)

const dialogVisible = ref(false)
const editingBudget = ref<BudgetResponse | null>(null)

const monthLabel = computed(() => `${currentYear.value} 年 ${currentMonth.value} 月`)

onMounted(() => {
  budgetStore.fetchBudgets(currentYear.value, currentMonth.value)
})

function prevMonth() {
  if (currentMonth.value === 1) {
    currentMonth.value = 12
    currentYear.value--
  } else {
    currentMonth.value--
  }
  budgetStore.fetchBudgets(currentYear.value, currentMonth.value)
}

function nextMonth() {
  if (currentMonth.value === 12) {
    currentMonth.value = 1
    currentYear.value++
  } else {
    currentMonth.value++
  }
  budgetStore.fetchBudgets(currentYear.value, currentMonth.value)
}

function getPercentage(budget: BudgetResponse): number {
  if (budget.amount === 0) return 0
  return Math.min(Math.round((budget.spent / budget.amount) * 100), 100)
}

function getProgressStatus(budget: BudgetResponse): '' | 'success' | 'warning' | 'exception' {
  const pct = budget.amount === 0 ? 0 : (budget.spent / budget.amount) * 100
  if (pct >= 100) return 'exception'
  if (pct >= 80) return 'warning'
  return 'success'
}

function handleAdd() {
  editingBudget.value = null
  dialogVisible.value = true
}

async function handleCopyPreviousMonth() {
  try {
    await ElMessageBox.confirm(
      `確定要將上月預算複製到 ${monthLabel.value} 嗎？本月已有預算時無法複製。`,
      '複製上月預算',
      { confirmButtonText: '複製', cancelButtonText: '取消', type: 'info' },
    )

    const success = await budgetStore.copyPreviousMonth(currentYear.value, currentMonth.value)
    if (success) {
      ElMessage.success('已複製上月預算')
    } else {
      ElMessage.error(budgetStore.error ?? '複製上月預算失敗')
    }
  } catch {
    // 使用者取消
  }
}

function handleEdit(budget: BudgetResponse) {
  editingBudget.value = budget
  dialogVisible.value = true
}

async function handleDelete(budget: BudgetResponse) {
  const label = budget.categoryName ?? '整體預算'
  try {
    await ElMessageBox.confirm(
      `確定要刪除「${label}」的預算嗎？`,
      '確認刪除',
      { confirmButtonText: '刪除', cancelButtonText: '取消', type: 'warning' },
    )
    const success = await budgetStore.deleteBudget(budget.id)
    if (success) {
      ElMessage.success('預算已刪除')
    }
  } catch {
    // 使用者取消
  }
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 style="margin: 0">預算管理</h1>
      <div class="header-actions">
        <el-button :loading="budgetStore.loading" @click="handleCopyPreviousMonth">複製上月</el-button>
        <el-button type="primary" @click="handleAdd">新增預算</el-button>
      </div>
    </div>

    <!-- 月份切換 -->
    <div class="month-nav">
      <el-button :icon="ArrowLeft" circle @click="prevMonth" />
      <span class="month-label">{{ monthLabel }}</span>
      <el-button :icon="ArrowRight" circle @click="nextMonth" />
    </div>

    <div v-loading="budgetStore.loading">
      <el-empty v-if="budgetStore.budgets.length === 0 && !budgetStore.loading" description="本月尚未設定預算">
        <div class="empty-actions">
          <el-button @click="handleCopyPreviousMonth">複製上月預算</el-button>
          <el-button type="primary" @click="handleAdd">設定第一筆預算</el-button>
        </div>
      </el-empty>

      <div v-else class="budget-list">
        <el-card v-for="budget in budgetStore.budgets" :key="budget.id" class="budget-card" shadow="hover">
          <div class="budget-header">
            <span class="budget-name">
              <el-tag v-if="budget.categoryId === null" type="primary" size="small">整體</el-tag>
              <el-tag v-else size="small">{{ budget.categoryName }}</el-tag>
            </span>
            <div class="budget-actions">
              <el-button type="primary" text size="small" @click="handleEdit(budget)">編輯</el-button>
              <el-button type="danger" text size="small" @click="handleDelete(budget)">刪除</el-button>
            </div>
          </div>
          <div class="budget-amount">
            <span>已花費 ${{ budget.spent.toLocaleString() }} / ${{ budget.amount.toLocaleString() }}</span>
            <span :class="{ 'over-budget': budget.spent > budget.amount }">
              剩餘 ${{ Math.max(budget.amount - budget.spent, 0).toLocaleString() }}
            </span>
          </div>
          <el-progress
            :percentage="getPercentage(budget)"
            :status="getProgressStatus(budget)"
            :stroke-width="16"
            :text-inside="true"
          />
          <div v-if="budget.spent > budget.amount" class="over-budget-warning">
            超支 ${{ (budget.spent - budget.amount).toLocaleString() }}
          </div>
        </el-card>
      </div>
    </div>

    <BudgetDialog
      v-model:visible="dialogVisible"
      :editing-budget="editingBudget"
      :year="currentYear"
      :month="currentMonth"
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

.header-actions,
.empty-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.month-nav {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  margin-bottom: 24px;
}

.month-label {
  font-size: 18px;
  font-weight: bold;
  min-width: 120px;
  text-align: center;
}

.budget-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.budget-card {
  border-radius: 8px;
}

.budget-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.budget-name {
  font-weight: bold;
}

.budget-actions {
  display: flex;
  gap: 4px;
}

.budget-amount {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  font-size: 14px;
  color: #606266;
}

.over-budget {
  color: #f56c6c;
  font-weight: bold;
}

.over-budget-warning {
  margin-top: 8px;
  color: #f56c6c;
  font-weight: bold;
  font-size: 14px;
  text-align: right;
}
</style>
