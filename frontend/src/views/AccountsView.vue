<script setup lang="ts">
import { ref, onMounted } from 'vue'
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
      <el-button type="primary" @click="handleAdd">新增帳戶</el-button>
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
