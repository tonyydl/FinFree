<script setup lang="ts">
import { reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { LoginRequest } from '@/types'

const router = useRouter()
const authStore = useAuthStore()

const form = reactive<LoginRequest>({
  email: '',
  password: '',
})

async function handleLogin() {
  const success = await authStore.login(form)
  if (success) {
    router.push({ name: 'Dashboard' })
  }
}
</script>

<template>
  <div style="display: flex; justify-content: center; align-items: center; height: 100vh; background: #f5f7fa; padding: 16px">
    <el-card style="width: 100%; max-width: 400px">
      <template #header>
        <h2 style="text-align: center; margin: 0">FinFree 登入</h2>
      </template>
      <el-form :model="form" label-position="top" @submit.prevent="handleLogin">
        <el-form-item label="Email">
          <el-input v-model="form.email" type="email" placeholder="請輸入 Email" />
        </el-form-item>
        <el-form-item label="密碼">
          <el-input v-model="form.password" type="password" placeholder="請輸入密碼" show-password />
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
            登入
          </el-button>
        </el-form-item>
        <div style="text-align: center">
          還沒有帳號？
          <router-link to="/register">前往註冊</router-link>
        </div>
      </el-form>
    </el-card>
  </div>
</template>
