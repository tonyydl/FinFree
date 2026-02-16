<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { useRecurringTransactionStore } from '@/stores/recurringTransaction'
import { useCategoryStore } from '@/stores/category'
import { useAccountStore } from '@/stores/account'
import { TransactionType, RecurrenceFrequency } from '@/types'
import type { RecurringTransactionResponse } from '@/types'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  visible: boolean
  editingItem: RecurringTransactionResponse | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  'saved': []
}>()

const store = useRecurringTransactionStore()
const categoryStore = useCategoryStore()
const accountStore = useAccountStore()
const formRef = ref<FormInstance>()

const isEditing = computed(() => !!props.editingItem)
const dialogTitle = computed(() => isEditing.value ? '編輯定期交易' : '新增定期交易')

const form = reactive({
  amount: null as number | null,
  type: TransactionType.Expense,
  categoryId: null as number | null,
  description: '',
  frequency: RecurrenceFrequency.Monthly,
  startDate: '',
  endDate: '' as string | '',
  isActive: true,
  accountId: null as number | null,
})

const rules: FormRules = {
  amount: [
    { required: true, message: '請輸入金額', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '金額必須大於 0', trigger: 'blur' },
  ],
  categoryId: [
    { required: true, message: '請選擇分類', trigger: 'change' },
  ],
  startDate: [
    { required: true, message: '請選擇起始日期', trigger: 'change' },
  ],
  accountId: [
    { required: true, message: '請選擇帳戶', trigger: 'change' },
  ],
}

const frequencyOptions = [
  { value: RecurrenceFrequency.Daily, label: '每日' },
  { value: RecurrenceFrequency.Weekly, label: '每週' },
  { value: RecurrenceFrequency.Monthly, label: '每月' },
  { value: RecurrenceFrequency.Yearly, label: '每年' },
]

const filteredCategories = computed(() =>
  categoryStore.categories.filter(c => c.type === form.type),
)

watch(() => form.type, () => {
  form.categoryId = null
})

watch(() => props.visible, (val) => {
  if (val) {
    categoryStore.fetchCategories()
    accountStore.fetchAccounts()
    if (props.editingItem) {
      form.amount = props.editingItem.amount
      form.type = props.editingItem.type
      form.categoryId = props.editingItem.categoryId
      form.description = props.editingItem.description ?? ''
      form.frequency = props.editingItem.frequency
      form.startDate = props.editingItem.startDate
      form.endDate = props.editingItem.endDate ?? ''
      form.isActive = props.editingItem.isActive
      form.accountId = props.editingItem.accountId
    } else {
      resetForm()
    }
  }
})

function resetForm() {
  form.amount = null
  form.type = TransactionType.Expense
  form.categoryId = null
  form.description = ''
  form.frequency = RecurrenceFrequency.Monthly
  form.startDate = ''
  form.endDate = ''
  form.isActive = true
  form.accountId = null
}

function handleClose() {
  emit('update:visible', false)
  formRef.value?.resetFields()
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  const startDateStr = new Date(form.startDate).toISOString()
  const endDateStr = form.endDate ? new Date(form.endDate).toISOString() : null

  if (isEditing.value && props.editingItem) {
    const success = await store.update(props.editingItem.id, {
      amount: form.amount!,
      description: form.description || undefined,
      endDate: endDateStr,
      isActive: form.isActive,
      accountId: form.accountId!,
    })
    if (success) {
      ElMessage.success('定期交易已更新')
      emit('saved')
      handleClose()
    }
  } else {
    const success = await store.create({
      amount: form.amount!,
      type: form.type,
      categoryId: form.categoryId!,
      description: form.description || undefined,
      frequency: form.frequency,
      startDate: startDateStr,
      endDate: endDateStr,
      accountId: form.accountId!,
    })
    if (success) {
      ElMessage.success('定期交易已新增')
      emit('saved')
      handleClose()
    }
  }
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="dialogTitle"
    width="500px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-position="top"
      @submit.prevent="handleSubmit"
    >
      <el-form-item label="類型" prop="type">
        <el-radio-group v-model="form.type" :disabled="isEditing">
          <el-radio :value="TransactionType.Expense">支出</el-radio>
          <el-radio :value="TransactionType.Income">收入</el-radio>
        </el-radio-group>
      </el-form-item>

      <el-form-item label="金額" prop="amount">
        <el-input-number
          v-model="form.amount"
          :min="0.01"
          :precision="2"
          :controls="false"
          placeholder="請輸入金額"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item label="分類" prop="categoryId">
        <el-select
          v-model="form.categoryId"
          placeholder="請選擇分類"
          style="width: 100%"
          :loading="categoryStore.loading"
          :disabled="isEditing"
        >
          <el-option
            v-for="cat in filteredCategories"
            :key="cat.id"
            :label="cat.name"
            :value="cat.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="帳戶" prop="accountId">
        <el-select
          v-model="form.accountId"
          placeholder="請選擇帳戶"
          style="width: 100%"
          :loading="accountStore.loading"
          :disabled="isEditing"
        >
          <el-option
            v-for="acc in accountStore.accounts"
            :key="acc.id"
            :label="acc.name"
            :value="acc.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="頻率" prop="frequency">
        <el-select
          v-model="form.frequency"
          style="width: 100%"
          :disabled="isEditing"
        >
          <el-option
            v-for="opt in frequencyOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="起始日期" prop="startDate">
        <el-date-picker
          v-model="form.startDate"
          type="date"
          placeholder="請選擇起始日期"
          style="width: 100%"
          value-format="YYYY-MM-DD"
          :disabled="isEditing"
        />
      </el-form-item>

      <el-form-item label="結束日期（選填）" prop="endDate">
        <el-date-picker
          v-model="form.endDate"
          type="date"
          placeholder="不填則永久執行"
          style="width: 100%"
          value-format="YYYY-MM-DD"
        />
      </el-form-item>

      <el-form-item label="說明" prop="description">
        <el-input
          v-model="form.description"
          type="textarea"
          :rows="2"
          placeholder="選填"
          maxlength="500"
          show-word-limit
        />
      </el-form-item>

      <el-form-item v-if="isEditing" label="狀態">
        <el-switch
          v-model="form.isActive"
          active-text="啟用"
          inactive-text="停用"
        />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="store.loading" @click="handleSubmit">
        {{ isEditing ? '更新' : '新增' }}
      </el-button>
    </template>
  </el-dialog>
</template>
