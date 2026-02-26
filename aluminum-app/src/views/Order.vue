<template>
  <div class="page-container">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-info">
        <h1>订单管理中心</h1>
        <p>截止至 {{ currentDate }}，共有 {{ total }} 个订单记录</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" plain @click="handleExport">
          <el-icon><Download /></el-icon>
          导出数据
        </el-button>
        <el-button type="primary" @click="showDialog()">
          <el-icon><Plus /></el-icon>
          手动录入订单
        </el-button>
      </div>
    </div>

    <!-- Search Filters -->
    <el-card class="filter-card" shadow="never">
      <div class="filter-row">
        <div class="filter-item">
          <label>订单单号</label>
          <el-input v-model="keyword" placeholder="输入单号模糊查询" clearable @keyup.enter="loadData" />
        </div>
        <div class="filter-item">
          <label>客户</label>
          <el-input v-model="customerFilter" placeholder="输入客户名称" clearable @keyup.enter="loadData" />
        </div>
        <div class="filter-item">
          <label>订单状态</label>
          <el-select v-model="statusFilter" placeholder="全部状态" clearable>
            <el-option label="未配料" :value="0" />
            <el-option label="已配料" :value="1" />
          </el-select>
        </div>
        <div class="filter-actions">
          <el-button type="primary" @click="loadData">查询</el-button>
          <el-button @click="resetFilters">重置</el-button>
        </div>
      </div>
    </el-card>

    <!-- Data Table -->
    <el-card class="table-card" shadow="never">
      <el-table :data="tableData" border @selection-change="onSelectionChange" v-loading="loading">
        <el-table-column type="selection" width="50" />
        <el-table-column label="订单信息" min-width="180">
          <template #default="{ row }">
            <div class="order-info">
              <div class="order-id">#{{ row.orderId }}</div>
              <div class="order-date">{{ formatDate(row.createdAt) }} 下单</div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="customer" label="客户名称" min-width="120" />
        <el-table-column label="规格 (mm)" min-width="120">
          <template #default="{ row }">
            {{ row.width }} x {{ row.length }} x {{ row.thickness }}
          </template>
        </el-table-column>
        <el-table-column prop="materialName" label="品种" min-width="120" />
        <el-table-column prop="quantity" label="件数" width="80" />
        <el-table-column prop="weight" label="重量(kg)" width="100" />
        <el-table-column prop="status" label="订单状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'warning'">
              {{ row.status === 1 ? '已配料' : '未配料' }}
            </el-tag>
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
          <el-button type="danger" size="small" :disabled="!selectedIds.length" @click="handleBatchDelete">批量删除 ({{ selectedIds.length }})</el-button>
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
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑订单' : '新增订单'" width="500px">
      <el-form :model="form" label-width="100px" :rules="formRules" ref="formRef">
        <el-form-item label="订单号"><el-input v-model="form.orderId" placeholder="留空自动生成" /></el-form-item>
        <el-form-item label="客户" prop="customer">
          <el-input v-model="form.customer" placeholder="请输入客户名称" />
        </el-form-item>
        <el-form-item label="品种" prop="materialName">
          <el-select v-model="form.materialName" placeholder="请选择品种" filterable style="width: 100%;" @change="onMaterialChange">
            <el-option v-for="v in varieties" :key="v.id" :label="v.materialName" :value="v.materialName" />
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Download, Plus, Edit, Delete } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { getOrders, createOrder, updateOrder, deleteOrder, batchDeleteOrders, importOrders, exportOrders, downloadOrderTemplate, getAllVarieties } from '../api'

const route = useRoute()
const loading = ref(false)
const tableData = ref<any[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const keyword = ref('')
const customerFilter = ref('')
const statusFilter = ref<number | null>(0)
const selectedIds = ref<number[]>([])
const dialogVisible = ref(false)
const editingId = ref(0)
const varieties = ref<{id: number, materialName: string, density: number}[]>([])
const formRef = ref<FormInstance>()

const currentDate = new Date().toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric' })

const defaultForm = { orderId: '', customer: '', materialName: '', thickness: null, width: null, length: null, quantity: null, weight: null }
const form = reactive<any>({ ...defaultForm })

const formRules: FormRules = {
  customer: [{ required: true, message: '请输入客户名称', trigger: 'blur' }],
  materialName: [{ required: true, message: '请选择品种', trigger: 'change' }]
}

const formatDate = (date: string) => {
  if (!date) return '-'
  return new Date(date).toLocaleDateString('zh-CN')
}

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getOrders({ 
      page: page.value, 
      pageSize: pageSize.value, 
      keyword: keyword.value,
      customer: customerFilter.value,
      status: statusFilter.value
    })
    if (res.success) {
      tableData.value = res.data.items
      total.value = res.data.total
    }
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  keyword.value = ''
  customerFilter.value = ''
  statusFilter.value = null
  loadData()
}

const loadVarieties = async () => {
  const res: any = await getAllVarieties()
  if (res.success) {
    varieties.value = res.data
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

const handleSave = async () => {
  await formRef.value?.validate()
  const res: any = editingId.value
    ? await updateOrder({ ...form, id: editingId.value })
    : await createOrder(form)
  if (res.success) {
    ElMessage.success(res.message)
    dialogVisible.value = false
    loadData()
  }
}

const handleDelete = async (id: number) => {
  await ElMessageBox.confirm('确认删除?', '提示', { type: 'warning' })
  const res: any = await deleteOrder(id)
  if (res.success) { ElMessage.success('已删除'); loadData() }
}

const handleBatchDelete = async () => {
  await ElMessageBox.confirm(`确认删除选中的${selectedIds.value.length}条?`, '提示', { type: 'warning' })
  const res: any = await batchDeleteOrders(selectedIds.value)
  if (res.success) { ElMessage.success('已删除'); loadData() }
}

const onSelectionChange = (rows: any[]) => {
  selectedIds.value = rows.map(r => r.id)
}

const handleImport = async (options: any) => {
  const res: any = await importOrders(options.file)
  if (res.success) { ElMessage.success(res.message); loadData() }
}

const handleExport = async () => {
  const res: any = await exportOrders()
  const url = URL.createObjectURL(new Blob([res]))
  const a = document.createElement('a')
  a.href = url; a.download = 'orders.xlsx'; a.click()
  URL.revokeObjectURL(url)
}

const handleDownloadTemplate = async () => {
  const res: any = await downloadOrderTemplate()
  const url = URL.createObjectURL(new Blob([res]))
  const a = document.createElement('a')
  a.href = url; a.download = 'order_template.xlsx'; a.click()
  URL.revokeObjectURL(url)
}

onMounted(() => {
  loadData()
  loadVarieties()
  // Check if we should open dialog from dashboard
  if (route.query.action === 'new') {
    showDialog()
  }
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
.filter-item :deep(.el-select),
.filter-item :deep(.el-date-editor) {
  width: 200px;
}

.filter-actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.table-card :deep(.el-card__body) {
  padding: 0;
}

.order-info {
  display: flex;
  flex-direction: column;
}

.order-id {
  font-weight: 600;
  color: #1a1f36;
}

.order-date {
  font-size: 12px;
  color: #909399;
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

.form-hint {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}
</style>
