<template>
  <div class="page-container">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-info">
        <h1>废料回收规则设置</h1>
        <p>定义余料回收标准，当余料尺寸或面积小于设定值时判定为废料</p>
      </div>
    </div>

    <!-- Settings Content -->
    <el-card class="settings-card" shadow="never">
      <div class="settings-layout">
        <!-- Tabs -->
        <div class="settings-tabs">
          <div class="tab-item">
            <el-icon><Grid /></el-icon>
            <span>材质库管理</span>
          </div>
          <div class="tab-item disabled">
            <el-icon><Setting /></el-icon>
            <span>厚度规范设置</span>
          </div>
          <div class="tab-item disabled">
            <el-icon><Aim /></el-icon>
            <span>零件间距标准</span>
          </div>
          <div class="tab-item disabled">
            <el-icon><Scissor /></el-icon>
            <span>切割工艺配套</span>
          </div>
          <div class="tab-item active">
            <el-icon><RefreshRight /></el-icon>
            <span>余料回收规则</span>
          </div>
        </div>

        <!-- Content -->
        <div class="settings-content">
          <div class="content-header">
            <div class="content-title">
              <el-icon><RefreshRight /></el-icon>
              <span>废料判定参数</span>
            </div>
          </div>

          <el-form :model="form" label-width="160px" v-loading="loading" class="settings-form">
            <el-form-item label="最小长度(mm)">
              <el-input-number v-model="form.minLength" :min="1" :step="10" />
              <div class="form-hint">小于此值视为废料</div>
            </el-form-item>
            <el-form-item label="最小宽度(mm)">
              <el-input-number v-model="form.minWidth" :min="1" :step="10" />
              <div class="form-hint">小于此值视为废料</div>
            </el-form-item>
            <el-form-item label="最大废料面积(㎡)">
              <el-input-number v-model="form.maxWasteArea" :min="0.01" :step="0.01" :precision="4" />
              <div class="form-hint">小于此值视为废料</div>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleSave" :loading="saving">
                <el-icon><Check /></el-icon>
                保存设置
              </el-button>
            </el-form-item>
          </el-form>

          <div class="settings-description">
            <el-alert type="info" :closable="false">
              <template #title>
                <strong>规则说明</strong>
              </template>
              <p>当套料完成后，系统会自动计算剩余的余料面积。如果余料的长或宽小于设定的最小值，或者面积小于最大废料面积，则该余料将被标记为废料，不再计入可用库存。</p>
            </el-alert>
          </div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Grid, Setting, Aim, Scissor, RefreshRight, Check } from '@element-plus/icons-vue'
import { getWasteSettings, saveWasteSetting } from '../api'

const loading = ref(false)
const saving = ref(false)

const form = reactive({
  id: 0,
  minLength: 100,
  minWidth: 100,
  maxWasteArea: 0.1
})

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getWasteSettings()
    if (res.success && res.data) {
      const data = Array.isArray(res.data) ? res.data[0] : res.data
      if (data) {
        form.id = data.id || 0
        form.minLength = data.minLength || 100
        form.minWidth = data.minWidth || 100
        form.maxWasteArea = data.maxWasteArea || 0.1
      }
    }
  } finally {
    loading.value = false
  }
}

const handleSave = async () => {
  saving.value = true
  try {
    const res: any = await saveWasteSetting(form)
    if (res.success) {
      ElMessage.success('保存成功')
    }
  } finally {
    saving.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.page-container {
  max-width: 1400px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 20px;
}

.header-info h1 {
  font-size: 24px;
  font-weight: 600;
  color: #1a1f36;
  margin: 0 0 8px 0;
}

.header-info p {
  font-size: 14px;
  color: #909399;
  margin: 0;
}

.settings-card :deep(.el-card__body) {
  padding: 0;
}

.settings-layout {
  display: flex;
  min-height: 500px;
}

.settings-tabs {
  width: 220px;
  background: #f8fafc;
  border-right: 1px solid #ebeef5;
  padding: 16px 0;
}

.tab-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 20px;
  cursor: pointer;
  color: #606266;
  transition: all 0.2s;
}

.tab-item:hover {
  background: rgba(64, 158, 255, 0.05);
  color: #409eff;
}

.tab-item.active {
  background: rgba(64, 158, 255, 0.1);
  color: #409eff;
  font-weight: 500;
  border-left: 3px solid #409eff;
}

.tab-item.disabled {
  cursor: not-allowed;
  color: #c0c4cc;
}

.settings-content {
  flex: 1;
  padding: 20px;
}

.content-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid #ebeef5;
}

.content-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: #1a1f36;
}

.settings-form {
  max-width: 500px;
}

.form-hint {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}

.settings-description {
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid #ebeef5;
}

.settings-description p {
  margin: 8px 0 0 0;
  font-size: 13px;
  color: #606266;
  line-height: 1.6;
}
</style>
