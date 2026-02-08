<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { useCategoryStore } from '@/stores/category'
import { TransactionType } from '@/types'
import type { CategoryResponse } from '@/types'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  visible: boolean
  editingCategory: CategoryResponse | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
}>()

const categoryStore = useCategoryStore()
const formRef = ref<FormInstance>()

const isEditing = computed(() => !!props.editingCategory)
const dialogTitle = computed(() => isEditing.value ? '編輯分類' : '新增分類')

const form = reactive({
  name: '',
  type: TransactionType.Expense,
})

const rules: FormRules = {
  name: [
    { required: true, message: '請輸入分類名稱', trigger: 'blur' },
    { min: 1, max: 50, message: '分類名稱長度須在 1-50 字元之間', trigger: 'blur' },
  ],
}

watch(() => props.visible, (val) => {
  if (val) {
    if (props.editingCategory) {
      form.name = props.editingCategory.name
      form.type = props.editingCategory.type
    } else {
      resetForm()
    }
  }
})

function resetForm() {
  form.name = ''
  form.type = TransactionType.Expense
}

function handleClose() {
  emit('update:visible', false)
  formRef.value?.resetFields()
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  if (isEditing.value && props.editingCategory) {
    const success = await categoryStore.updateCategory(props.editingCategory.id, {
      name: form.name,
    })
    if (success) {
      ElMessage.success('分類已更新')
      handleClose()
    } else {
      ElMessage.error(categoryStore.error ?? '更新失敗')
    }
  } else {
    const success = await categoryStore.createCategory({
      name: form.name,
      type: form.type,
    })
    if (success) {
      ElMessage.success('分類已新增')
      handleClose()
    } else {
      ElMessage.error(categoryStore.error ?? '新增失敗')
    }
  }
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="dialogTitle"
    width="400px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-position="top"
      @submit.prevent="handleSubmit"
    >
      <el-form-item label="類型" prop="type">
        <el-radio-group v-model="form.type" :disabled="isEditing">
          <el-radio :value="TransactionType.Expense">支出</el-radio>
          <el-radio :value="TransactionType.Income">收入</el-radio>
        </el-radio-group>
      </el-form-item>

      <el-form-item label="名稱" prop="name">
        <el-input
          v-model="form.name"
          placeholder="請輸入分類名稱"
          maxlength="50"
          show-word-limit
        />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="categoryStore.loading" @click="handleSubmit">
        {{ isEditing ? '更新' : '新增' }}
      </el-button>
    </template>
  </el-dialog>
</template>
