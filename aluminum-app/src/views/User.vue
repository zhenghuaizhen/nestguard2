<template>
  <div class="page-container">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-info">
        <h1>系统权限与用户</h1>
        <p>管理系统操作员、访问角色及安全审计日志</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" @click="showDialog()">
          <el-icon><Plus /></el-icon>
          邀请新同事
        </el-button>
      </div>
    </div>

    <!-- Stats -->
    <div class="stats-row">
      <div class="stat-item">
        <div class="stat-icon stat-icon-blue">
          <el-icon><User /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.admins }}</div>
          <div class="stat-label">超级管理员</div>
          <div class="stat-count">人</div>
        </div>
      </div>
      <div class="stat-item">
        <div class="stat-icon stat-icon-green">
          <el-icon><UserFilled /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.users }}</div>
          <div class="stat-label">普通用户</div>
          <div class="stat-count">人</div>
        </div>
      </div>
    </div>

    <!-- User List -->
    <el-card class="table-card" shadow="never">
      <template #header>
        <div class="table-header">
          <span>活跃用户列表</span>
          <el-input v-model="keyword" placeholder="搜索姓名..." clearable @keyup.enter="loadData" style="width: 240px;">
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>
        </div>
      </template>

      <el-table :data="tableData" border v-loading="loading">
        <el-table-column label="成员信息" min-width="200">
          <template #default="{ row }">
            <div class="user-info">
              <el-avatar :size="36" class="user-avatar">{{ row.userName?.charAt(0) || 'U' }}</el-avatar>
              <div class="user-detail">
                <div class="user-name">{{ row.userName }}</div>
                <div class="user-id">{{ row.userId }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="role" label="角色权限" width="150">
          <template #default="{ row }">
            <el-tag :type="row.role === 1 ? 'danger' : 'primary'">
              {{ row.role === 1 ? '管理员' : '普通用户' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="账号状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="lastLogin" label="最后在线日期" width="160">
          <template #default="{ row }">
            {{ formatTime(row.lastLogin) }}
          </template>
        </el-table-column>
        <el-table-column label="管理操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button-group>
              <el-button size="small" @click="showDialog(row)">
                <el-icon><Edit /></el-icon>
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row.id)" :disabled="row.role === 1">
                <el-icon><Delete /></el-icon>
              </el-button>
            </el-button-group>
          </template>
        </el-table-column>
      </el-table>

      <div class="table-footer">
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
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑用户' : '新增用户'" width="450px">
      <el-form :model="form" label-width="80px">
        <el-form-item label="用户ID" required>
          <el-input v-model="form.userId" placeholder="请输入用户ID" :disabled="!!editingId" />
        </el-form-item>
        <el-form-item label="用户名" required>
          <el-input v-model="form.userName" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="form.tel" placeholder="请输入电话号码" />
        </el-form-item>
        <el-form-item label="用户角色">
          <el-select v-model="form.role" placeholder="请选择角色" style="width: 100%;">
            <el-option label="普通用户" :value="0" />
            <el-option label="管理员" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item label="账户状态">
          <el-select v-model="form.status" placeholder="请选择状态" style="width: 100%;">
            <el-option label="禁用" :value="0" />
            <el-option label="启用" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item label="密码" v-if="!editingId">
          <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
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
import { Plus, User, UserFilled, Search, Edit, Delete } from '@element-plus/icons-vue'
import { getUsers, createUser, updateUser, deleteUser } from '../api'

const loading = ref(false)
const tableData = ref<any[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const keyword = ref('')
const dialogVisible = ref(false)
const editingId = ref(0)

const stats = ref({
  admins: 1,
  users: 0
})

const defaultForm = { userId: '', userName: '', tel: '', password: '', role: 0, status: 1 }
const form = reactive({ ...defaultForm })

const formatTime = (time: string) => {
  if (!time) return '-'
  return new Date(time).toLocaleDateString('zh-CN')
}

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getUsers({ page: page.value, pageSize: pageSize.value, keyword: keyword.value })
    if (res.success) {
      tableData.value = res.data.items
      total.value = res.data.total
      // Calculate stats based on role field from backend
      stats.value.admins = tableData.value.filter((u: any) => u.role === 1).length
      stats.value.users = tableData.value.filter((u: any) => u.role === 0).length
    }
  } finally {
    loading.value = false
  }
}

const showDialog = (row?: any) => {
  if (row) {
    editingId.value = row.id
    Object.assign(form, { 
      userId: row.userId, 
      userName: row.userName, 
      tel: row.tel || '', 
      password: '',
      role: row.role ?? 0,
      status: row.status ?? 1
    })
  } else {
    editingId.value = 0
    Object.assign(form, defaultForm)
  }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.userId || !form.userName) {
    ElMessage.error('请填写用户ID和用户名')
    return
  }
  
  const res: any = editingId.value
    ? await updateUser({ ...form, id: editingId.value })
    : await createUser(form)
  
  if (res.success) {
    ElMessage.success(res.message)
    dialogVisible.value = false
    loadData()
  }
}

const handleDelete = async (id: number) => {
  await ElMessageBox.confirm('确认删除该用户?', '提示', { type: 'warning' })
  const res: any = await deleteUser(id)
  if (res.success) {
    ElMessage.success('已删除')
    loadData()
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

.header-actions {
  display: flex;
  gap: 12px;
}

.stats-row {
  display: flex;
  gap: 20px;
  margin-bottom: 20px;
}

.stat-item {
  background: #fff;
  border-radius: 12px;
  padding: 20px;
  display: flex;
  gap: 16px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
  min-width: 200px;
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}

.stat-icon-blue {
  background: rgba(64, 158, 255, 0.1);
  color: #409eff;
}

.stat-icon-green {
  background: rgba(103, 194, 58, 0.1);
  color: #67c23a;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #1a1f36;
}

.stat-label {
  font-size: 13px;
  color: #909399;
}

.stat-count {
  font-size: 12px;
  color: #c0c4cc;
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

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-avatar {
  background: linear-gradient(135deg, #409eff 0%, #66b1ff 100%);
}

.user-name {
  font-weight: 500;
  color: #1a1f36;
}

.user-id {
  font-size: 12px;
  color: #909399;
}

.table-footer {
  display: flex;
  justify-content: flex-end;
  padding: 16px 20px;
  border-top: 1px solid #ebeef5;
}
</style>
