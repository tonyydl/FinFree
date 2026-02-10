<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import AppSidebar from './AppSidebar.vue'
import AppNavbar from './AppNavbar.vue'

const isCollapsed = ref(false)
const isMobile = ref(false)
const drawerVisible = ref(false)

function checkMobile() {
  isMobile.value = window.innerWidth < 768
  if (isMobile.value) {
    isCollapsed.value = true
  }
}

function toggleSidebar() {
  if (isMobile.value) {
    drawerVisible.value = !drawerVisible.value
  } else {
    isCollapsed.value = !isCollapsed.value
  }
}

function handleMenuSelect() {
  if (isMobile.value) {
    drawerVisible.value = false
  }
}

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', checkMobile)
})

onUnmounted(() => {
  window.removeEventListener('resize', checkMobile)
})
</script>

<template>
  <el-container style="height: 100vh">
    <!-- 桌面版 sidebar -->
    <AppSidebar v-if="!isMobile" :collapsed="isCollapsed" @menu-select="handleMenuSelect" />

    <!-- 手機版 drawer -->
    <el-drawer
      v-if="isMobile"
      v-model="drawerVisible"
      direction="ltr"
      :size="200"
      :show-close="false"
      :with-header="false"
    >
      <AppSidebar :collapsed="false" @menu-select="handleMenuSelect" />
    </el-drawer>

    <el-container direction="vertical">
      <AppNavbar :collapsed="isCollapsed" @toggle-sidebar="toggleSidebar" />
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>
