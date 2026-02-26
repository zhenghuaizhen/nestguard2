<template>
  <div class="page-container">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-info">
        <h1>套料历史存档</h1>
        <p>追溯往期生产任务、NC代码及材料报告</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" plain @click="showFilterDialog = true">
          <el-icon><Filter /></el-icon>
          高级筛选
        </el-button>
        <el-button type="primary" plain>
          <el-icon><Delete /></el-icon>
          归档清理
        </el-button>
      </div>
    </div>

    <!-- Search -->
    <el-card class="search-card" shadow="never">
      <el-input v-model="keyword" placeholder="搜索品种/订单号/客户" clearable @keyup.enter="loadData" style="width: 300px;">
        <template #prefix>
          <el-icon><Search /></el-icon>
        </template>
      </el-input>
      <el-button type="primary" @click="loadData" style="margin-left: 12px;">搜索</el-button>
    </el-card>

    <!-- History Cards -->
    <div class="history-list" v-loading="loading">
      <div v-for="item in tableData" :key="item.id" class="history-card">
        <div class="card-header">
          <div class="card-id">#{{ item.orderId || `USE-${item.id}` }}</div>
          <div class="card-time">{{ formatTime(item.useTime) }}</div>
        </div>
        <div class="card-body">
          <div class="info-row">
            <span class="info-label">材质规格</span>
            <span class="info-value">{{ item.materialName }} / {{ item.thickness }}mm</span>
          </div>
          <div class="info-row">
            <span class="info-label">套料结果</span>
            <div class="info-tags">
              <el-tag type="success" size="small">{{ item.width }}x{{ item.length }}mm</el-tag>
              <el-tag size="small">{{ item.weight || '-' }} KG</el-tag>
            </div>
          </div>
        </div>
        <div class="card-footer">
          <el-button type="primary" text size="small" @click="showDetail(item)">
            <el-icon><View /></el-icon>
            查看详情
          </el-button>
        </div>
      </div>

      <div v-if="!loading && tableData.length === 0" class="empty-state">
        <el-empty description="暂无套料历史记录" />
      </div>
    </div>

    <!-- Load More -->
    <div class="load-more" v-if="tableData.length > 0 && tableData.length < total">
      <el-button @click="loadMore" :loading="loadingMore">加载更多历史记录</el-button>
    </div>

    <!-- Detail Dialog -->
    <el-dialog v-model="showDetailDialog" title="套料详情" width="600px">
      <el-descriptions :column="2" border v-if="currentDetail">
        <el-descriptions-item label="订单号">{{ currentDetail.orderId }}</el-descriptions-item>
        <el-descriptions-item label="品种">{{ currentDetail.materialName }}</el-descriptions-item>
        <el-descriptions-item label="厚度">{{ currentDetail.thickness }}mm</el-descriptions-item>
        <el-descriptions-item label="尺寸">{{ currentDetail.width }}x{{ currentDetail.length }}mm</el-descriptions-item>
        <el-descriptions-item label="重量">{{ currentDetail.weight || '-' }} KG</el-descriptions-item>
        <el-descriptions-item label="配料时间">{{ formatTime(currentDetail.useTime) }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Filter, Delete, Search, View } from '@element-plus/icons-vue'
import { getUsedList } from '../api'

const loading = ref(false)
const loadingMore = ref(false)
const tableData = ref<any[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const keyword = ref('')
const showFilterDialog = ref(false)
const showDetailDialog = ref(false)
const currentDetail = ref<any>(null)

const formatTime = (time: string) => {
  if (!time) return ''
  return new Date(time).toLocaleString('zh-CN')
}

const loadData = async () => {
  loading.value = true
  page.value = 1
  try {
    const res: any = await getUsedList({ page: page.value, pageSize: pageSize.value, keyword: keyword.value })
    if (res.success) {
      tableData.value = res.data.items
      total.value = res.data.total
    }
  } finally {
    loading.value = false
  }
}

const loadMore = async () => {
  loadingMore.value = true
  page.value++
  try {
    const res: any = await getUsedList({ page: page.value, pageSize: pageSize.value, keyword: keyword.value })
    if (res.success) {
      tableData.value = [...tableData.value, ...res.data.items]
    }
  } finally {
    loadingMore.value = false
  }
}

const showDetail = (item: any) => {
  currentDetail.value = item
  showDetailDialog.value = true
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

.search-card {
  margin-bottom: 20px;
}

.search-card :deep(.el-card__body) {
  padding: 16px 20px;
}

.history-list {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.history-card {
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
  overflow: hidden;
  transition: all 0.2s;
}

.history-card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #f0f2f5;
}

.card-id {
  font-weight: 600;
  color: #1a1f36;
}

.card-time {
  font-size: 13px;
  color: #909399;
}

.card-body {
  padding: 16px 20px;
}

.info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.info-row:last-child {
  margin-bottom: 0;
}

.info-label {
  font-size: 13px;
  color: #909399;
}

.info-value {
  font-size: 14px;
  color: #1a1f36;
}

.info-tags {
  display: flex;
  gap: 8px;
}

.card-footer {
  padding: 12px 20px;
  background: #f8fafc;
  text-align: right;
}

.empty-state {
  grid-column: span 3;
  padding: 60px 0;
}

.load-more {
  text-align: center;
  margin-top: 24px;
}

@media (max-width: 1200px) {
  .history-list {
    grid-template-columns: repeat(2, 1fr);
  }
  .empty-state {
    grid-column: span 2;
  }
}

@media (max-width: 800px) {
  .history-list {
    grid-template-columns: 1fr;
  }
  .empty-state {
    grid-column: span 1;
  }
}
</style>
