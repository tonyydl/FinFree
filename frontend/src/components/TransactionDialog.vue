<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { useTransactionStore } from '@/stores/transaction'
import { useCategoryStore } from '@/stores/category'
import { useAccountStore } from '@/stores/account'
import { TransactionType } from '@/types'
import type { TransactionResponse } from '@/types'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  visible: boolean
  editingTransaction: TransactionResponse | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  'saved': []
}>()

const transactionStore = useTransactionStore()
const categoryStore = useCategoryStore()
const accountStore = useAccountStore()
const formRef = ref<FormInstance>()

const isEditing = computed(() => !!props.editingTransaction)
const dialogTitle = computed(() => isEditing.value ? '編輯交易' : '新增交易')

const form = reactive({
  amount: null as number | null,
  type: TransactionType.Expense,
  categoryId: null as number | null,
  description: '',
  date: '',
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
  date: [
    { required: true, message: '請選擇日期', trigger: 'change' },
  ],
  accountId: [
    { required: true, message: '請選擇帳戶', trigger: 'change' },
  ],
}

// 依據交易類型篩選分類
const filteredCategories = computed(() =>
  categoryStore.categories.filter(c => c.type === form.type),
)

// 切換類型時清除已選分類（因為分類選項會改變）
watch(() => form.type, () => {
  form.categoryId = null
})

// 開啟對話框時初始化表單
watch(() => props.visible, (val) => {
  if (val) {
    categoryStore.fetchCategories()
    accountStore.fetchAccounts()
    if (props.editingTransaction) {
      form.amount = props.editingTransaction.amount
      form.type = props.editingTransaction.type
      form.categoryId = props.editingTransaction.categoryId
      form.description = props.editingTransaction.description ?? ''
      form.date = props.editingTransaction.date
      form.accountId = props.editingTransaction.accountId
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
  form.date = ''
  form.accountId = null
}

function handleClose() {
  emit('update:visible', false)
  formRef.value?.resetFields()
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  const dateStr = new Date(form.date).toISOString()

  if (isEditing.value && props.editingTransaction) {
    const success = await transactionStore.updateTransaction(props.editingTransaction.id, {
      amount: form.amount!,
      type: form.type,
      categoryId: form.categoryId!,
      description: form.description || undefined,
      date: dateStr,
      accountId: form.accountId!,
    })
    if (success) {
      ElMessage.success('交易記錄已更新')
      emit('saved')
      handleClose()
    }
  } else {
    const success = await transactionStore.createTransaction({
      amount: form.amount!,
      type: form.type,
      categoryId: form.categoryId!,
      description: form.description || undefined,
      date: dateStr,
      accountId: form.accountId!,
    })
    if (success) {
      ElMessage.success('交易記錄已新增')
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
        <el-radio-group v-model="form.type">
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
        >
          <el-option
            v-for="acc in accountStore.accounts"
            :key="acc.id"
            :label="acc.name"
            :value="acc.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="日期" prop="date">
        <el-date-picker
          v-model="form.date"
          type="date"
          placeholder="請選擇日期"
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
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="transactionStore.loading" @click="handleSubmit">
        {{ isEditing ? '更新' : '新增' }}
      </el-button>
    </template>
  </el-dialog>
</template>
