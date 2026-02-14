<script setup lang="ts">
import { reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { RegisterRequest } from '@/types'

const router = useRouter()
const authStore = useAuthStore()

const form = reactive<RegisterRequest>({
  username: '',
  email: '',
  password: '',
})

async function handleRegister() {
  const success = await authStore.register(form)
  if (success) {
    router.push({ name: 'Dashboard' })
  }
}
</script>

<template>
  <div style="display: flex; justify-content: center; align-items: center; height: 100vh; background: var(--el-bg-color-page); padding: 16px">
    <el-card style="width: 100%; max-width: 400px">
      <template #header>
        <h2 style="text-align: center; margin: 0">FinFree 註冊</h2>
      </template>
      <el-form :model="form" label-position="top" @submit.prevent="handleRegister">
        <el-form-item label="使用者名稱">
          <el-input v-model="form.username" placeholder="請輸入使用者名稱" />
        </el-form-item>
        <el-form-item label="Email">
          <el-input v-model="form.email" type="email" placeholder="請輸入 Email" />
        </el-form-item>
        <el-form-item label="密碼">
          <el-input v-model="form.password" type="password" placeholder="請輸入密碼（至少 6 字元）" show-password />
        </el-form-item>
        <el-alert
          v-if="authStore.error"
          :title="authStore.error"
          type="error"
          show-icon
          :closable="false"
          style="margin-bottom: 16px"
        />
        <el-form-item>
          <el-button
            type="primary"
            native-type="submit"
            :loading="authStore.loading"
            style="width: 100%"
          >
            註冊
          </el-button>
        </el-form-item>
        <div style="text-align: center">
          已有帳號？
          <router-link to="/login">前往登入</router-link>
        </div>
      </el-form>
    </el-card>
  </div>
</template>
