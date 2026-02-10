<script setup lang="ts">
import { useRouter } from 'vue-router'
import { HomeFilled, List, Grid, Setting } from '@element-plus/icons-vue'

defineProps<{
  collapsed: boolean
}>()

const router = useRouter()
</script>

<template>
  <el-aside :width="collapsed ? '64px' : '200px'" style="transition: width 0.3s">
    <el-menu
      :collapse="collapsed"
      :default-active="router.currentRoute.value.path"
      router
      style="height: 100%; border-right: none"
    >
      <div style="padding: 16px; text-align: center; font-weight: bold; font-size: 18px">
        <span v-if="!collapsed">FinFree</span>
        <span v-else>FF</span>
      </div>
      <el-menu-item index="/">
        <el-icon><HomeFilled /></el-icon>
        <template #title>總覽</template>
      </el-menu-item>
      <el-menu-item index="/transactions">
        <el-icon><List /></el-icon>
        <template #title>交易記錄</template>
      </el-menu-item>
      <el-menu-item index="/categories">
        <el-icon><Grid /></el-icon>
        <template #title>分類管理</template>
      </el-menu-item>
      <el-menu-item index="/settings">
        <el-icon><Setting /></el-icon>
        <template #title>個人設定</template>
      </el-menu-item>
    </el-menu>
  </el-aside>
</template>
