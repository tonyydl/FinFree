<script setup lang="ts">
import { ref } from 'vue'
import AppSidebar from './AppSidebar.vue'
import AppNavbar from './AppNavbar.vue'

const isCollapsed = ref(false)

function toggleSidebar() {
  isCollapsed.value = !isCollapsed.value
}
</script>

<template>
  <el-container style="height: 100vh">
    <AppSidebar :collapsed="isCollapsed" />
    <el-container direction="vertical">
      <AppNavbar :collapsed="isCollapsed" @toggle-sidebar="toggleSidebar" />
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>
