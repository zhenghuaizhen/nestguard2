<template>
  <div class="page-container">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-info">
        <h1>品种与工艺标准设置</h1>
        <p>在这里定义原材料材质、切割工艺参数和排样规则</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" plain @click="handleExport">
          <el-icon><Download /></el-icon>
          导出配置
        </el-button>
        <el-button type="primary" @click="showDialog()">
          <el-icon><Plus /></el-icon>
          新增品种
        </el-button>
      </div>
    </div>

    <!-- Settings Content -->
    <el-card class="settings-card" shadow="never">
      <div class="settings-layout">
        <!-- Tabs -->
        <div class="settings-tabs">
          <div class="tab-item active">
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
          <div class="tab-item disabled">
            <el-icon><RefreshRight /></el-icon>
            <span>余料回收规则</span>
          </div>
        </div>

        <!-- Content -->
        <div class="settings-content">
          <div class="content-header">
            <div class="content-title">
              <el-icon><Setting /></el-icon>
              <span>基本参数维护</span>
            </div>
            <div class="content-actions">
              <el-input v-model="keyword" placeholder="搜索品种名称" clearable @keyup.enter="loadData" style="width: 200px;" />
              <el-button type="primary" @click="loadData">查询</el-button>
            </div>
          </div>

          <!-- Table -->
          <el-table :data="tableData" border @selection-change="onSelectionChange" v-loading="loading">
            <el-table-column type="selection" width="50" />
            <el-table-column prop="id" label="ID" width="80" />
            <el-table-column prop="materialName" label="品种名称" min-width="180" />
            <el-table-column prop="density" label="密度(g/cm³)" width="110" />
            <el-table-column prop="costPerKg" label="成本单价(元/kg)" width="130" />
            <el-table-column prop="minStock" label="最低库存(kg)" width="110" />
            <el-table-column prop="maxStock" label="最高库存(kg)" width="110" />
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="{ row }">
                <el-button-group>
                  <el-button size="small" @click="showDialog(row)">
                    <el-icon><Edit /></el-icon>
                  </el-button>
                  <el-button size="small" type="danger" @click="handleDelete(row.id)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </el-button-group>
              </template>
            </el-table-column>
          </el-table>

          <div class="table-footer">
            <el-button type="danger" size="small" :disabled="!selectedIds.length" @click="handleBatchDelete">
              批量删除 ({{ selectedIds.length }})
            </el-button>
            <el-pagination
              v-model:current-page="page"
              v-model:page-size="pageSize"
              :total="total"
              :page-sizes="[10, 20, 50]"
              layout="total, sizes, prev, pager, next"
              @change="loadData"
            />
          </div>
        </div>
      </div>
    </el-card>

    <!-- Dialog -->
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑品种' : '新增品种'" width="450px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="品种" required>
          <el-input v-model="form.materialName" placeholder="如: 5052铝镁合金板" />
        </el-form-item>
        <el-form-item label="密度(g/cm³)">
          <el-input-number v-model="form.density" :min="0" :step="0.01" :precision="4" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="成本单价">
          <el-input-number v-model="form.costPerKg" :min="0" :step="1" :precision="2" style="width: 100%;" placeholder="元/kg" />
        </el-form-item>
        <el-form-item label="最低库存">
          <el-input-number v-model="form.minStock" :min="0" :step="10" :precision="2" style="width: 100%;" placeholder="kg" />
        </el-form-item>
        <el-form-item label="最高库存">
          <el-input-number v-model="form.maxStock" :min="0" :step="10" :precision="2" style="width: 100%;" placeholder="kg" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Download, Grid, Setting, Aim, Scissor, RefreshRight, Edit, Delete } from '@element-plus/icons-vue'
import { getVarieties, createVariety, updateVariety, deleteVariety, batchDeleteVarieties } from '../api'

const loading = ref(false)
const tableData = ref<any[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const keyword = ref('')
const selectedIds = ref<number[]>([])
const dialogVisible = ref(false)
const editingId = ref(0)

const defaultForm = { materialName: '', density: 2.7, costPerKg: null, minStock: null, maxStock: null }
const form = reactive({ ...defaultForm })

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getVarieties({ page: page.value, pageSize: pageSize.value, keyword: keyword.value })
    if (res.success) {
      tableData.value = res.data.items
      total.value = res.data.total
    }
  } finally {
    loading.value = false
  }
}

const handleExport = () => {
  ElMessage.info('导出配置功能开发中')
}

const showDialog = (row?: any) => {
  if (row) {
    editingId.value = row.id
    Object.assign(form, { 
      materialName: row.materialName, 
      density: row.density, 
      costPerKg: row.costPerKg,
      minStock: row.minStock,
      maxStock: row.maxStock
    })
  } else {
    editingId.value = 0
    Object.assign(form, defaultForm)
  }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.materialName) {
    ElMessage.error('请输入品种名称')
    return
  }
  const res: any = editingId.value
    ? await updateVariety({ ...form, id: editingId.value })
    : await createVariety(form)
  if (res.success) {
    ElMessage.success(res.message)
    dialogVisible.value = false
    loadData()
  }
}

const handleDelete = async (id: number) => {
  await ElMessageBox.confirm('确认删除?', '提示', { type: 'warning' })
  const res: any = await deleteVariety(id)
  if (res.success) { ElMessage.success('已删除'); loadData() }
}

const handleBatchDelete = async () => {
  await ElMessageBox.confirm(`确认删除选中的${selectedIds.value.length}条?`, '提示', { type: 'warning' })
  const res: any = await batchDeleteVarieties(selectedIds.value)
  if (res.success) { ElMessage.success('已删除'); loadData() }
}

const onSelectionChange = (rows: any[]) => {
  selectedIds.value = rows.map(r => r.id)
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

.header-actions {
  display: flex;
  gap: 12px;
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
  margin-bottom: 16px;
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

.content-actions {
  display: flex;
  gap: 8px;
}

.table-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #ebeef5;
}
</style>
