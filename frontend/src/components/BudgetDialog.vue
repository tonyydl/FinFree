<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useCategoryStore } from '@/stores/category'
import { useBudgetStore } from '@/stores/budget'
import { TransactionType } from '@/types'
import type { BudgetResponse } from '@/types'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  visible: boolean
  editingBudget: BudgetResponse | null
  year: number
  month: number
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
}>()

const categoryStore = useCategoryStore()
const budgetStore = useBudgetStore()

const form = ref({
  categoryId: '' as number | '',
  amount: 0,
})

const isEdit = computed(() => props.editingBudget !== null)
const dialogTitle = computed(() => isEdit.value ? '編輯預算' : '新增預算')

// 只顯示支出分類（預算主要控制支出）
const expenseCategories = computed(() =>
  categoryStore.categories.filter(c => c.type === TransactionType.Expense)
)

watch(() => props.visible, (val) => {
  if (val) {
    categoryStore.fetchCategories()
    if (props.editingBudget) {
      form.value.categoryId = props.editingBudget.categoryId ?? ''
      form.value.amount = props.editingBudget.amount
    } else {
      form.value.categoryId = ''
      form.value.amount = 0
    }
  }
})

async function handleSubmit() {
  if (form.value.amount <= 0) {
    ElMessage.warning('金額必須大於 0')
    return
  }

  let success: boolean
  if (isEdit.value) {
    success = await budgetStore.updateBudget(
      props.editingBudget!.id,
      { amount: form.value.amount },
      props.year,
      props.month,
    )
  } else {
    success = await budgetStore.createBudget({
      categoryId: form.value.categoryId === '' ? null : form.value.categoryId,
      amount: form.value.amount,
      year: props.year,
      month: props.month,
    })
  }

  if (success) {
    ElMessage.success(isEdit.value ? '預算已更新' : '預算已新增')
    emit('update:visible', false)
  } else {
    ElMessage.error(budgetStore.error ?? '操作失敗')
  }
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="dialogTitle"
    width="400px"
    @update:model-value="emit('update:visible', $event)"
  >
    <el-form label-width="80px">
      <el-form-item label="分類">
        <el-select
          v-model="form.categoryId"
          placeholder="整體預算"
          clearable
          style="width: 100%"
          :disabled="isEdit"
        >
          <el-option label="整體預算（全部支出）" value="" />
          <el-option
            v-for="cat in expenseCategories"
            :key="cat.id"
            :label="cat.name"
            :value="cat.id"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="預算金額">
        <el-input-number
          v-model="form.amount"
          :min="0"
          :precision="0"
          :step="1000"
          style="width: 100%"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:visible', false)">取消</el-button>
      <el-button type="primary" :loading="budgetStore.loading" @click="handleSubmit">
        {{ isEdit ? '更新' : '新增' }}
      </el-button>
    </template>
  </el-dialog>
</template>
