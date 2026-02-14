<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import { useRouter } from 'vue-router'
import { Fold, Expand, Sunny, Moon } from '@element-plus/icons-vue'

defineProps<{
  collapsed: boolean
}>()

const emit = defineEmits<{
  toggleSidebar: []
}>()

const authStore = useAuthStore()
const themeStore = useThemeStore()
const router = useRouter()

function handleLogout() {
  authStore.logout()
  router.push({ name: 'Login' })
}
</script>

<template>
  <el-header style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--el-border-color)">
    <el-button text @click="emit('toggleSidebar')">
      <el-icon :size="20">
        <Fold v-if="!collapsed" />
        <Expand v-else />
      </el-icon>
    </el-button>
    <div style="display: flex; align-items: center; gap: 12px">
      <el-button text @click="themeStore.toggleTheme">
        <el-icon :size="18">
          <Moon v-if="!themeStore.isDark" />
          <Sunny v-else />
        </el-icon>
      </el-button>
      <span>{{ authStore.username }}</span>
      <el-button type="danger" text @click="handleLogout">登出</el-button>
    </div>
  </el-header>
</template>
