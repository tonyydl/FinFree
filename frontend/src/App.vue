<script setup lang="ts">
</script>

<template>
  <router-view />
</template>

<style>
html, body, #app {
  margin: 0;
  padding: 0;
  height: 100%;
  font-family: 'Helvetica Neue', Helvetica, 'PingFang SC',
    'Hiragino Sans GB', 'Microsoft YaHei', Arial, sans-serif;
}

@media (max-width: 767px) {
  .date-range-popper .el-picker-panel__body {
    flex-direction: column;
    min-width: 0;
  }

  .date-range-popper .el-picker-panel__content {
    width: auto;
  }

  .date-range-popper {
    max-width: 90vw;
  }
}
</style>
