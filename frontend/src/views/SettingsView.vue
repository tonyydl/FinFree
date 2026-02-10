<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import api from '@/api'
import type { ProfileResponse } from '@/types'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'

const loading = ref(false)
const profile = ref<ProfileResponse | null>(null)

// 個人資料表單
const profileFormRef = ref<FormInstance>()
const profileForm = reactive({
  username: '',
})
const profileRules: FormRules = {
  username: [
    { required: true, message: '請輸入使用者名稱', trigger: 'blur' },
    { min: 2, max: 50, message: '使用者名稱長度須在 2-50 字元之間', trigger: 'blur' },
  ],
}
const profileSaving = ref(false)

// 修改密碼表單
const passwordFormRef = ref<FormInstance>()
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})
const passwordRules: FormRules = {
  currentPassword: [
    { required: true, message: '請輸入目前密碼', trigger: 'blur' },
  ],
  newPassword: [
    { required: true, message: '請輸入新密碼', trigger: 'blur' },
    { min: 6, message: '新密碼至少需要 6 個字元', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '請再次輸入新密碼', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== passwordForm.newPassword) {
          callback(new Error('兩次密碼輸入不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur',
    },
  ],
}
const passwordSaving = ref(false)

onMounted(async () => {
  await fetchProfile()
})

async function fetchProfile() {
  loading.value = true
  try {
    const response = await api.get<ProfileResponse>('/user/profile')
    profile.value = response.data
    profileForm.username = response.data.username
  } catch {
    ElMessage.error('取得個人資料失敗')
  } finally {
    loading.value = false
  }
}

async function handleUpdateProfile() {
  const valid = await profileFormRef.value?.validate().catch(() => false)
  if (!valid) return

  profileSaving.value = true
  try {
    const response = await api.put<ProfileResponse>('/user/profile', {
      username: profileForm.username,
    })
    profile.value = response.data
    // 更新 localStorage 中的 username
    localStorage.setItem('username', response.data.username)
    ElMessage.success('個人資料已更新')
  } catch (err: unknown) {
    const axiosErr = err as { response?: { data?: { message?: string } } }
    ElMessage.error(axiosErr.response?.data?.message ?? '更新失敗')
  } finally {
    profileSaving.value = false
  }
}

async function handleChangePassword() {
  const valid = await passwordFormRef.value?.validate().catch(() => false)
  if (!valid) return

  passwordSaving.value = true
  try {
    await api.put('/user/password', {
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
    })
    ElMessage.success('密碼已更新')
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmPassword = ''
    passwordFormRef.value?.resetFields()
  } catch (err: unknown) {
    const axiosErr = err as { response?: { data?: { message?: string } } }
    ElMessage.error(axiosErr.response?.data?.message ?? '修改密碼失敗')
  } finally {
    passwordSaving.value = false
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('zh-TW', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}
</script>

<template>
  <div v-loading="loading">
    <h1 style="margin-top: 0; margin-bottom: 24px">個人設定</h1>

    <el-row :gutter="24">
      <!-- 個人資料 -->
      <el-col :xs="24" :sm="12">
        <el-card shadow="never">
          <template #header>
            <span style="font-weight: bold">個人資料</span>
          </template>

          <el-form
            ref="profileFormRef"
            :model="profileForm"
            :rules="profileRules"
            label-position="top"
            @submit.prevent="handleUpdateProfile"
          >
            <el-form-item label="Email">
              <el-input :model-value="profile?.email" disabled />
            </el-form-item>

            <el-form-item label="使用者名稱" prop="username">
              <el-input v-model="profileForm.username" />
            </el-form-item>

            <el-form-item v-if="profile" label="註冊日期">
              <el-input :model-value="formatDate(profile.createdAt)" disabled />
            </el-form-item>

            <el-form-item>
              <el-button type="primary" :loading="profileSaving" @click="handleUpdateProfile">
                儲存變更
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>

      <!-- 修改密碼 -->
      <el-col :xs="24" :sm="12">
        <el-card shadow="never">
          <template #header>
            <span style="font-weight: bold">修改密碼</span>
          </template>

          <el-form
            ref="passwordFormRef"
            :model="passwordForm"
            :rules="passwordRules"
            label-position="top"
            @submit.prevent="handleChangePassword"
          >
            <el-form-item label="目前密碼" prop="currentPassword">
              <el-input v-model="passwordForm.currentPassword" type="password" show-password />
            </el-form-item>

            <el-form-item label="新密碼" prop="newPassword">
              <el-input v-model="passwordForm.newPassword" type="password" show-password />
            </el-form-item>

            <el-form-item label="確認新密碼" prop="confirmPassword">
              <el-input v-model="passwordForm.confirmPassword" type="password" show-password />
            </el-form-item>

            <el-form-item>
              <el-button type="primary" :loading="passwordSaving" @click="handleChangePassword">
                更新密碼
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>
