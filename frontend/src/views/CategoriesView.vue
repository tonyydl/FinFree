<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useCategoryStore } from '@/stores/category'
import { TransactionType } from '@/types'
import type { CategoryResponse } from '@/types'
import CategoryDialog from '@/components/CategoryDialog.vue'
import { ElMessage, ElMessageBox } from 'element-plus'

const categoryStore = useCategoryStore()

const dialogVisible = ref(false)
const editingCategory = ref<CategoryResponse | null>(null)

onMounted(() => {
  categoryStore.fetchCategories()
})

function handleAdd() {
  editingCategory.value = null
  dialogVisible.value = true
}

function handleEdit(row: CategoryResponse) {
  editingCategory.value = row
  dialogVisible.value = true
}

async function handleDelete(row: CategoryResponse) {
  try {
    await ElMessageBox.confirm(
      `確定要刪除分類「${row.name}」嗎？`,
      '確認刪除',
      {
        confirmButtonText: '刪除',
        cancelButtonText: '取消',
        type: 'warning',
      },
    )
    const success = await categoryStore.deleteCategory(row.id)
    if (success) {
      ElMessage.success('分類已刪除')
    } else {
      ElMessage.error(categoryStore.error ?? '刪除失敗')
    }
  } catch {
    // 使用者取消刪除
  }
}
</script>

<template>
  <div>
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px">
      <h1 style="margin: 0">分類管理</h1>
      <el-button type="primary" @click="handleAdd">新增分類</el-button>
    </div>

    <el-table
      v-loading="categoryStore.loading"
      :data="categoryStore.categories"
      stripe
      style="width: 100%"
    >
      <el-table-column prop="name" label="名稱" />

      <el-table-column label="類型" width="120">
        <template #default="{ row }">
          <el-tag :type="row.type === TransactionType.Income ? 'success' : 'danger'" size="small">
            {{ row.type === TransactionType.Income ? '收入' : '支出' }}
          </el-tag>
        </template>
      </el-table-column>

      <el-table-column label="來源" width="120">
        <template #default="{ row }">
          <el-tag :type="row.isSystemDefault ? 'info' : undefined" size="small">
            {{ row.isSystemDefault ? '系統預設' : '自訂' }}
          </el-tag>
        </template>
      </el-table-column>

      <el-table-column label="操作" width="150" fixed="right">
        <template #default="{ row }">
          <template v-if="!row.isSystemDefault">
            <el-button type="primary" text size="small" @click="handleEdit(row)">編輯</el-button>
            <el-button type="danger" text size="small" @click="handleDelete(row)">刪除</el-button>
          </template>
          <span v-else style="color: #909399; font-size: 12px">—</span>
        </template>
      </el-table-column>

      <template #empty>
        <el-empty description="尚無分類">
          <el-button type="primary" @click="handleAdd">新增第一個分類</el-button>
        </el-empty>
      </template>
    </el-table>

    <CategoryDialog
      v-model:visible="dialogVisible"
      :editing-category="editingCategory"
    />
  </div>
</template>
