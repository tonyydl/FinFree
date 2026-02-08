<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div>
    <h1>歡迎回來，{{ authStore.username }}</h1>
    <p>這是 FinFree 記帳應用的總覽頁面。</p>

    <el-row :gutter="20" style="margin-top: 20px">
      <el-col :span="8">
        <el-card shadow="hover">
          <template #header>總收入</template>
          <p style="font-size: 24px; color: #67c23a">--</p>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover">
          <template #header>總支出</template>
          <p style="font-size: 24px; color: #f56c6c">--</p>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover">
          <template #header>餘額</template>
          <p style="font-size: 24px; color: #409eff">--</p>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>
