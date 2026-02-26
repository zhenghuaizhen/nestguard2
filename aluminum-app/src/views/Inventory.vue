<template>
  <div class="page-container">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-info">
        <h1>原材料库存看板</h1>
        <p>实时板材数据同步 (最后更新: {{ lastUpdate }})</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" plain>
          <el-icon><Camera /></el-icon>
          扫描入库
        </el-button>
        <el-button type="primary" @click="showDialog()">
          <el-icon><Plus /></el-icon>
          新增库存记录
        </el-button>
      </div>
    </div>

    <!-- Alert Banner -->
    <el-alert v-if="lowStockItems.length > 0" type="warning" :closable="false" class="alert-banner">
      <template #title>
        <div class="alert-content">
          <div class="alert-left">
            <el-icon :size="18"><Warning /></el-icon>
            <span><strong>库存预警！</strong> 共有 {{ lowStockItems.length }} 个品种库存低于最低库存阈值，请及时补充。</span>
          </div>
          <el-button type="warning" size="small" @click="showLowStockDialog = true">查看详情</el-button>
        </div>
      </template>
    </el-alert>

    <!-- Stats Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-label">总板材库存量 (张)</div>
        <div class="stat-value">{{ stats.totalPlates }}</div>
        <div class="stat-footer">仓容 {{ stats.capacity }}%</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">今日消耗量 (平米)</div>
        <div class="stat-value">{{ stats.todayConsumption }}</div>
        <div class="stat-footer trend-up">
          <el-icon><Top /></el-icon>
          比昨日上升 14%
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-label">原材料总估值 (万元)</div>
        <div class="stat-value">¥{{ calculatedTotalValue }}</div>
        <div class="stat-footer">按品种成本单价计算</div>
      </div>
    </div>

    <!-- Search Filters -->
    <el-card class="filter-card" shadow="never">
      <div class="filter-row">
        <div class="filter-item">
          <label>品种</label>
          <el-select v-model="materialNameFilter" placeholder="全部品种" clearable filterable>
            <el-option v-for="v in varieties" :key="v.id" :label="v.materialName" :value="v.materialName" />
          </el-select>
        </div>
        <div class="filter-item">
          <label>状态</label>
          <el-select v-model="statusFilter" placeholder="全部状态" clearable>
            <el-option label="可用" :value="0" />
            <el-option label="已用" :value="1" />
          </el-select>
        </div>
        <div class="filter-item">
          <label>规格</label>
          <el-input v-model="specFilter" placeholder="规格模糊查找" clearable @keyup.enter="loadData" />
        </div>
        <div class="filter-actions">
          <el-button type="primary" @click="loadData">查询</el-button>
          <el-button @click="resetFilters">重置</el-button>
        </div>
      </div>
    </el-card>

    <!-- Table Section -->
    <el-card class="table-card" shadow="never">
      <template #header>
        <div class="table-header">
          <span>实时库位列表</span>
          <div class="table-tabs">
            <el-radio-group v-model="materialFilter" size="small" @change="loadData">
              <el-radio-button :value="null">所有</el-radio-button>
              <el-radio-button :value="0">母材</el-radio-button>
              <el-radio-button :value="1">余料</el-radio-button>
            </el-radio-group>
          </div>
        </div>
      </template>

      <el-table :data="tableData" border v-loading="loading">
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="barcode" label="条码号" min-width="120" />
        <el-table-column prop="materialName" label="品种" min-width="150" />
        <el-table-column prop="materialType" label="物料类型" width="100">
          <template #default="{ row }">
            <el-tag :type="materialTypeTag(row.materialType)">{{ materialTypeLabel(row.materialType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="规格 (mm)" min-width="150">
          <template #default="{ row }">
            {{ row.width }} x {{ row.length }} x {{ row.thickness }}
          </template>
        </el-table-column>
        <el-table-column prop="quantity" label="件数" width="80" />
        <el-table-column prop="weight" label="重量(kg)" width="100" />
        <el-table-column label="库存金额(元)" width="120">
          <template #default="{ row }">
            {{ row.weight && getCostPerKg(row.materialName) ? (row.weight * (getCostPerKg(row.materialName) || 0)).toFixed(2) : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 0 ? 'success' : 'info'">{{ row.status === 0 ? '可用' : '已用' }}</el-tag>
          </template>
        </el-table-column>
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
        <div class="footer-left">
          <el-upload :show-file-list="false" accept=".xlsx,.xls" :http-request="handleImport" style="display: inline-block;">
            <el-button type="warning" size="small">导入Excel</el-button>
          </el-upload>
          <el-button size="small" @click="handleDownloadTemplate">下载模板</el-button>
          <el-button size="small" @click="handleExport">导出Excel</el-button>
        </div>
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          @change="loadData"
        />
      </div>
    </el-card>

    <!-- Dialog -->
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑库存' : '新增库存'" width="500px">
      <el-form :model="form" label-width="100px" :rules="formRules" ref="formRef">
        <el-form-item label="条码号">
          <el-input v-model="form.barcode" placeholder="请输入条码号" clearable />
        </el-form-item>
        <el-form-item label="品种" prop="materialName">
          <el-select v-model="form.materialName" placeholder="请选择品种" filterable style="width: 100%;" @change="onMaterialChange">
            <el-option v-for="v in varieties" :key="v.id" :label="v.materialName" :value="v.materialName" />
          </el-select>
        </el-form-item>
        <el-form-item label="物料类型">
          <el-select v-model="form.materialType" placeholder="请选择" style="width: 100%;">
            <el-option label="母材" :value="0" />
            <el-option label="余料" :value="1" />
            <el-option label="次品" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="厚度(mm)"><el-input-number v-model="form.thickness" :min="0.1" :step="0.5" :precision="2" placeholder="请输入" style="width: 100%;" @change="calculateWeight" /></el-form-item>
        <el-form-item label="宽度(mm)"><el-input-number v-model="form.width" :min="1" :precision="0" placeholder="请输入" style="width: 100%;" @change="calculateWeight" /></el-form-item>
        <el-form-item label="长度(mm)"><el-input-number v-model="form.length" :min="1" :precision="0" placeholder="请输入" style="width: 100%;" @change="calculateWeight" /></el-form-item>
        <el-form-item label="件数">
          <el-input-number v-model="form.quantity" :min="1" style="width: 100%;" @change="calculateWeight" />
          <div class="form-hint">填写件数后自动计算重量</div>
        </el-form-item>
        <el-form-item label="重量(kg)">
          <el-input-number v-model="form.weight" :min="0" :step="0.1" :precision="2" style="width: 100%;" />
          <div class="form-hint">可手动修改</div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- Low Stock Dialog -->
    <el-dialog v-model="showLowStockDialog" title="库存预警详情" width="700px">
      <el-table :data="lowStockItems" border>
        <el-table-column prop="materialName" label="品种" width="150" />
        <el-table-column label="当前库存(kg)" width="120">
          <template #default="{ row }">
            {{ row.currentStock }} kg
          </template>
        </el-table-column>
        <el-table-column label="最低库存(kg)" width="120">
          <template #default="{ row }">
            {{ row.minStock }} kg
          </template>
        </el-table-column>
        <el-table-column label="最高库存(kg)" width="120">
          <template #default="{ row }">
            {{ row.maxStock || '-' }} kg
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === '极低' ? 'danger' : 'warning'">{{ row.status }}</el-tag>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Camera, Warning, Top, Edit, Delete } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { getInventory, createInventory, updateInventory, deleteInventory, importInventory, exportInventory, downloadInventoryTemplate, getAllVarieties } from '../api'

const loading = ref(false)
const tableData = ref<any[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const keyword = ref('')
const materialFilter = ref<number | null>(null)
const materialNameFilter = ref('')
const statusFilter = ref<number | null>(0)
const specFilter = ref('')
const dialogVisible = ref(false)
const showLowStockDialog = ref(false)
const editingId = ref(0)
const varieties = ref<{id: number, materialName: string, density: number, costPerKg: number | null, minStock: number | null, maxStock: number | null}[]>([])
const formRef = ref<FormInstance>()

const lastUpdate = new Date().toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }).replace(/\//g, '-')

const stats = ref({
  totalPlates: 0,
  capacity: 64.2,
  todayConsumption: 1210.5,
  totalValue: 0
})

// Get cost per kg from variety data
const getCostPerKg = (materialName: string): number | null => {
  const variety = varieties.value.find(v => v.materialName === materialName)
  return variety?.costPerKg || null
}

// 计算库存金额
const getInventoryCost = (row: any) => {
  if (!row.weight) return 0
  const costPerKg = getCostPerKg(row.materialName)
  return costPerKg ? row.weight * costPerKg : 0
}

// 计算总估值 (万元)
const calculatedTotalValue = computed(() => {
  const total = tableData.value
    .filter(item => item.status === 0)
    .reduce((sum, item) => sum + getInventoryCost(item), 0)
  return (total / 10000).toFixed(2)
})

// 计算库存预警列表 - 按品种统计库存重量，与最低库存阈值对比
const lowStockItems = computed(() => {
  // 按品种分组统计可用库存重量
  const stockByMaterial: Record<string, number> = {}
  tableData.value
    .filter(item => item.status === 0)
    .forEach(item => {
      const name = item.materialName
      const weight = item.weight || 0
      stockByMaterial[name] = (stockByMaterial[name] || 0) + weight
    })
  
  // 检查每个品种是否低于最低库存
  const warnings: any[] = []
  varieties.value.forEach(variety => {
    const currentStock = stockByMaterial[variety.materialName] || 0
    if (variety.minStock && currentStock < variety.minStock) {
      warnings.push({
        materialName: variety.materialName,
        currentStock: currentStock.toFixed(2),
        minStock: variety.minStock,
        maxStock: variety.maxStock,
        status: currentStock < (variety.minStock * 0.5) ? '极低' : '偏低'
      })
    }
  })
  
  return warnings
})

const defaultForm = { barcode: '', materialName: '', materialType: 0, thickness: null, width: null, length: null, quantity: null, weight: null, status: 0 }
const form = reactive<any>({ ...defaultForm })

const formRules: FormRules = {
  materialName: [{ required: true, message: '请选择品种', trigger: 'change' }]
}

const materialTypeLabel = (type: number) => {
  const labels: Record<number, string> = { 0: '母材', 1: '余料', 2: '次品' }
  return labels[type] || '未知'
}

const materialTypeTag = (type: number) => {
  const tags: Record<number, string> = { 0: 'primary', 1: 'success', 2: 'danger' }
  return tags[type] || 'info'
}

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getInventory({ 
      page: page.value, 
      pageSize: pageSize.value, 
      keyword: keyword.value,
      materialName: materialNameFilter.value,
      status: statusFilter.value,
      spec: specFilter.value,
      materialType: materialFilter.value
    })
    if (res.success) {
      tableData.value = res.data.items
      total.value = res.data.total
      stats.value.totalPlates = res.data.total
    }
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  materialNameFilter.value = ''
  statusFilter.value = null
  specFilter.value = ''
  materialFilter.value = null
  loadData()
}

const loadVarieties = async () => {
  const res: any = await getAllVarieties()
  if (res.success) {
    varieties.value = res.data
  }
}

const onMaterialChange = () => {
  calculateWeight()
}

const calculateWeight = () => {
  if (form.quantity && form.quantity > 0 && form.materialName) {
    const variety = varieties.value.find(v => v.materialName === form.materialName)
    if (variety && variety.density) {
      // Weight (kg) = Volume (mm³) * Density (g/cm³) / 1,000,000
      const volume = form.width * form.length * form.thickness * form.quantity
      form.weight = Math.round(volume * variety.density / 1_000_000 * 100) / 100
    }
  }
}

const showDialog = (row?: any) => {
  if (row) {
    editingId.value = row.id
    Object.assign(form, row)
  } else {
    editingId.value = 0
    Object.assign(form, defaultForm)
  }
  dialogVisible.value = true
}

const handleSave = async () => {
  await formRef.value?.validate()
  const res: any = editingId.value
    ? await updateInventory({ ...form, id: editingId.value })
    : await createInventory(form)
  if (res.success) {
    ElMessage.success(res.message)
    dialogVisible.value = false
    loadData()
  }
}

const handleDelete = async (id: number) => {
  await ElMessageBox.confirm('确认删除?', '提示', { type: 'warning' })
  const res: any = await deleteInventory(id)
  if (res.success) { ElMessage.success('已删除'); loadData() }
}

const handleImport = async (options: any) => {
  const res: any = await importInventory(options.file)
  if (res.success) { ElMessage.success(res.message); loadData() }
}

const handleExport = async () => {
  const res: any = await exportInventory()
  const url = URL.createObjectURL(new Blob([res]))
  const a = document.createElement('a')
  a.href = url; a.download = 'inventory.xlsx'; a.click()
  URL.revokeObjectURL(url)
}

const handleDownloadTemplate = async () => {
  const res: any = await downloadInventoryTemplate()
  const url = URL.createObjectURL(new Blob([res]))
  const a = document.createElement('a')
  a.href = url; a.download = 'inventory_template.xlsx'; a.click()
  URL.revokeObjectURL(url)
}

onMounted(() => {
  loadData()
  loadVarieties()
})
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

.alert-banner {
  margin-bottom: 20px;
}

.alert-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.alert-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
  margin-bottom: 20px;
}

.stat-card {
  background: #fff;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
}

.stat-label {
  font-size: 13px;
  color: #909399;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 32px;
  font-weight: 600;
  color: #1a1f36;
  margin-bottom: 8px;
}

.stat-footer {
  font-size: 12px;
  color: #909399;
}

.trend-up {
  color: #67c23a;
  display: flex;
  align-items: center;
  gap: 4px;
}

.table-card :deep(.el-card__header) {
  padding: 16px 20px;
  border-bottom: 1px solid #ebeef5;
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.table-header span {
  font-weight: 600;
  color: #1a1f36;
}

.table-card :deep(.el-card__body) {
  padding: 0;
}

.table-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-top: 1px solid #ebeef5;
}

.footer-left {
  display: flex;
  gap: 8px;
}

.filter-card {
  margin-bottom: 16px;
}

.filter-card :deep(.el-card__body) {
  padding: 16px 20px;
}

.filter-row {
  display: flex;
  align-items: flex-end;
  gap: 20px;
  flex-wrap: wrap;
}

.filter-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.filter-item label {
  font-size: 13px;
  color: #606266;
  font-weight: 500;
}

.filter-item :deep(.el-input),
.filter-item :deep(.el-select) {
  width: 200px;
}

.filter-actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.form-hint {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}
</style>
