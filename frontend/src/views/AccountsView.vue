<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAccountStore } from '@/stores/account'
import type { AccountResponse } from '@/types'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'

const store = useAccountStore()

const dialogVisible = ref(false)
const editingAccount = ref<AccountResponse | null>(null)
const formRef = ref<FormInstance>()

const form = ref({
  name: '',
})

const rules: FormRules = {
  name: [
    { required: true, message: '請輸入帳戶名稱', trigger: 'blur' },
    { min: 1, max: 50, message: '帳戶名稱長度須為 1-50 字', trigger: 'blur' },
  ],
}

// 轉帳
const transferVisible = ref(false)
const transferFormRef = ref<FormInstance>()

const transferForm = ref({
  fromAccountId: null as number | null,
  toAccountId: null as number | null,
  amount: null as number | null,
  date: new Date().toISOString().slice(0, 10),
  description: '',
})

const transferRules: FormRules = {
  fromAccountId: [{ required: true, message: '請選擇來源帳戶', trigger: 'change' }],
  toAccountId: [{ required: true, message: '請選擇目標帳戶', trigger: 'change' }],
  amount: [
    { required: true, message: '請輸入金額', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '金額必須大於 0', trigger: 'blur' },
  ],
  date: [{ required: true, message: '請選擇日期', trigger: 'change' }],
}

const toAccountOptions = computed(() =>
  store.accounts.filter((a) => a.id !== transferForm.value.fromAccountId),
)

onMounted(() => {
  store.fetchAccounts()
})

function handleAdd() {
  editingAccount.value = null
  form.value.name = ''
  dialogVisible.value = true
}

function handleEdit(row: AccountResponse) {
  editingAccount.value = row
  form.value.name = row.name
  dialogVisible.value = true
}

function handleClose() {
  dialogVisible.value = false
  formRef.value?.resetFields()
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  if (editingAccount.value) {
    const success = await store.updateAccount(editingAccount.value.id, { name: form.value.name })
    if (success) {
      ElMessage.success('帳戶已更新')
      handleClose()
    }
  } else {
    const success = await store.createAccount({ name: form.value.name })
    if (success) {
      ElMessage.success('帳戶已新增')
      handleClose()
    }
  }
}

async function handleDelete(row: AccountResponse) {
  try {
    await ElMessageBox.confirm(
      `確定要刪除「${row.name}」帳戶嗎？`,
      '確認刪除',
      {
        confirmButtonText: '刪除',
        cancelButtonText: '取消',
        type: 'warning',
      },
    )
    const success = await store.deleteAccount(row.id)
    if (success) {
      ElMessage.success('帳戶已刪除')
    }
  } catch {
    // 使用者取消
  }
}

function handleTransfer() {
  transferForm.value = {
    fromAccountId: null,
    toAccountId: null,
    amount: null,
    date: new Date().toISOString().slice(0, 10),
    description: '',
  }
  transferVisible.value = true
}

function handleTransferClose() {
  transferVisible.value = false
  transferFormRef.value?.resetFields()
}

async function handleTransferSubmit() {
  const valid = await transferFormRef.value?.validate().catch(() => false)
  if (!valid) return

  if (transferForm.value.fromAccountId === transferForm.value.toAccountId) {
    ElMessage.warning('來源帳戶與目標帳戶不能相同')
    return
  }

  const success = await store.transfer({
    fromAccountId: transferForm.value.fromAccountId!,
    toAccountId: transferForm.value.toAccountId!,
    amount: transferForm.value.amount!,
    date: new Date(transferForm.value.date).toISOString(),
    description: transferForm.value.description || undefined,
  })

  if (success) {
    ElMessage.success('轉帳成功')
    handleTransferClose()
  } else if (store.error) {
    ElMessage.error(store.error)
  }
}

function formatCurrency(value: number): string {
  return `$${value.toLocaleString()}`
}

function getBalanceColor(balance: number): string {
  if (balance > 0) return '#67c23a'
  if (balance < 0) return '#f56c6c'
  return '#909399'
}

const totalBalance = () => store.accounts.reduce((sum, a) => sum + a.balance, 0)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 style="margin: 0">帳戶管理</h1>
      <div style="display: flex; gap: 8px">
        <el-button :disabled="store.accounts.length < 2" @click="handleTransfer">轉帳</el-button>
        <el-button type="primary" @click="handleAdd">新增帳戶</el-button>
      </div>
    </div>

    <!-- 帳戶總覽 -->
    <el-card v-if="store.accounts.length > 0" shadow="never" style="margin-bottom: 20px">
      <div style="display: flex; justify-content: space-between; align-items: center">
        <span style="font-size: 16px; font-weight: bold">總資產</span>
        <span :style="{ fontSize: '24px', fontWeight: 'bold', color: getBalanceColor(totalBalance()) }">
          {{ formatCurrency(totalBalance()) }}
        </span>
      </div>
    </el-card>

    <el-table
      v-loading="store.loading"
      :data="store.accounts"
      stripe
      style="width: 100%"
    >
      <el-table-column prop="name" label="帳戶名稱" />

      <el-table-column label="餘額" width="200">
        <template #default="{ row }">
          <span :style="{ color: getBalanceColor(row.balance), fontWeight: 'bold', fontSize: '16px' }">
            {{ formatCurrency(row.balance) }}
          </span>
        </template>
      </el-table-column>

      <el-table-column label="操作" width="150" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" text size="small" @click="handleEdit(row)">編輯</el-button>
          <el-button type="danger" text size="small" @click="handleDelete(row)">刪除</el-button>
        </template>
      </el-table-column>

      <template #empty>
        <el-empty description="尚無帳戶">
          <el-button type="primary" @click="handleAdd">新增第一個帳戶</el-button>
        </el-empty>
      </template>
    </el-table>

    <!-- 新增/編輯對話框 -->
    <el-dialog
      :model-value="dialogVisible"
      :title="editingAccount ? '編輯帳戶' : '新增帳戶'"
      width="400px"
      @close="handleClose"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        @submit.prevent="handleSubmit"
      >
        <el-form-item label="帳戶名稱" prop="name">
          <el-input
            v-model="form.name"
            placeholder="例如：現金、中信銀行、玉山信用卡"
            maxlength="50"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" :loading="store.loading" @click="handleSubmit">
          {{ editingAccount ? '更新' : '新增' }}
        </el-button>
      </template>
    </el-dialog>

    <!-- 轉帳對話框 -->
    <el-dialog
      :model-value="transferVisible"
      title="帳戶轉帳"
      width="450px"
      @close="handleTransferClose"
    >
      <el-form
        ref="transferFormRef"
        :model="transferForm"
        :rules="transferRules"
        label-position="top"
        @submit.prevent="handleTransferSubmit"
      >
        <el-form-item label="來源帳戶" prop="fromAccountId">
          <el-select
            v-model="transferForm.fromAccountId"
            placeholder="選擇來源帳戶"
            style="width: 100%"
          >
            <el-option
              v-for="account in store.accounts"
              :key="account.id"
              :label="`${account.name}（${formatCurrency(account.balance)}）`"
              :value="account.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="目標帳戶" prop="toAccountId">
          <el-select
            v-model="transferForm.toAccountId"
            placeholder="選擇目標帳戶"
            style="width: 100%"
          >
            <el-option
              v-for="account in toAccountOptions"
              :key="account.id"
              :label="`${account.name}（${formatCurrency(account.balance)}）`"
              :value="account.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="金額" prop="amount">
          <el-input-number
            v-model="transferForm.amount"
            :min="0.01"
            :precision="2"
            :controls="false"
            placeholder="輸入轉帳金額"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="日期" prop="date">
          <el-date-picker
            v-model="transferForm.date"
            type="date"
            placeholder="選擇日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="描述">
          <el-input
            v-model="transferForm.description"
            placeholder="選填，例如：繳信用卡"
            maxlength="500"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="handleTransferClose">取消</el-button>
        <el-button type="primary" :loading="store.loading" @click="handleTransferSubmit">
          確認轉帳
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
</style>
