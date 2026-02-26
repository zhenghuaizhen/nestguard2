<template>
  <div class="dashboard">
    <!-- Header -->
    <div class="dashboard-header">
      <div class="header-info">
        <h1>主控制台概况</h1>
        <p>欢迎回来，以下是今天的实时生产数据分析。日期：{{ currentDate }}</p>
      </div>
      <div class="header-actions">
        <el-input v-model="searchKeyword" placeholder="搜索订单、板材..." prefix-icon="Search" clearable class="search-input" />
        <el-button type="primary" @click="handleNewTask">
          <el-icon><Plus /></el-icon>
          新增任务
        </el-button>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon stat-icon-blue">
          <el-icon :size="24"><Document /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-trend trend-up">
            <el-icon><Top /></el-icon>
            <span>+12.5%</span>
          </div>
          <div class="stat-label">今日处理订单</div>
          <div class="stat-value">{{ stats.todayOrders }}</div>
          <div class="stat-footer">
            <el-icon><Clock /></el-icon>
            <span>最近更新于 {{ stats.lastUpdate }}</span>
          </div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon stat-icon-green">
          <el-icon :size="24"><TrendCharts /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-trend trend-up">
            <el-icon><Top /></el-icon>
            <span>+2.4%</span>
          </div>
          <div class="stat-label">平均利用率</div>
          <div class="stat-value">{{ stats.avgUtilization }}%</div>
          <div class="stat-footer">
            <el-icon><TrendCharts /></el-icon>
            <span>高于行业基准 18%</span>
          </div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon stat-icon-orange">
          <el-icon :size="24"><Warning /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-trend trend-down">
            <el-icon><Bottom /></el-icon>
            <span>-32张</span>
          </div>
          <div class="stat-label">库存预警材质</div>
          <div class="stat-value">{{ stats.lowStockCount }}</div>
          <div class="stat-footer">
            <el-icon><Warning /></el-icon>
            <span>{{ stats.lowStockMaterial }} 库位紧张</span>
          </div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon stat-icon-purple">
          <el-icon :size="24"><Timer /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-status">
            <el-tag type="success" size="small">运行中</el-tag>
          </div>
          <div class="stat-label">今日切削时长</div>
          <div class="stat-value">{{ stats.cuttingHours }}h</div>
          <div class="stat-footer">
            <el-icon><Monitor /></el-icon>
            <span>三台设备满轴承运行</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Charts & Activity -->
    <div class="dashboard-row">
      <el-card class="chart-card" shadow="hover">
        <template #header>
          <div class="card-header">
            <span>板材利用率 & 生产成本趋势</span>
            <div class="chart-tabs">
              <el-radio-group v-model="chartPeriod" size="small">
                <el-radio-button value="week">周</el-radio-button>
                <el-radio-button value="month">月</el-radio-button>
              </el-radio-group>
            </div>
          </div>
        </template>
        <div class="chart-container" ref="chartRef"></div>
      </el-card>

      <el-card class="activity-card" shadow="hover">
        <template #header>
          <span>实时系统记录</span>
        </template>
        <div class="activity-list">
          <div class="activity-item" v-for="(item, index) in activities" :key="index">
            <div class="activity-icon" :class="item.type">
              <el-icon>
                <component :is="item.icon" />
              </el-icon>
            </div>
            <div class="activity-content">
              <div class="activity-title">{{ item.title }}</div>
              <div class="activity-desc">{{ item.description }}</div>
            </div>
            <div class="activity-time">{{ item.time }}</div>
          </div>
        </div>
        <div class="activity-footer">
          <el-button text type="primary">查看完整日志</el-button>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { Document, TrendCharts, Warning, Timer, Clock, Monitor, Top, Bottom, Plus, Search, CircleCheck, Loading, Bell, User } from '@element-plus/icons-vue'
import * as echarts from 'echarts'

const router = useRouter()
const searchKeyword = ref('')
const showNewTaskDialog = ref(false)
const chartPeriod = ref('week')
const chartRef = ref<HTMLElement | null>(null)

const currentDate = new Date().toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric' })

const stats = ref({
  todayOrders: 48,
  avgUtilization: 94.8,
  lowStockCount: 5,
  lowStockMaterial: '不锈钢316L',
  cuttingHours: 16.4,
  lastUpdate: new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' }) + ' AM'
})

const activities = ref([
  { type: 'success', icon: CircleCheck, title: '套料任务完成 #N2026-088', description: '板材: 碳钢 Q345B 12mm', time: '09:12 AM' },
  { type: 'loading', icon: Loading, title: '正在生成 NC 代码...', description: '订单: 机床外罩组件 A100', time: '08:45 AM' },
  { type: 'warning', icon: Bell, title: '库存预警: 铝板 5052', description: '当前剩余 8 张, 设定的阈值为 15', time: '08:02 AM' },
  { type: 'info', icon: User, title: '新用户成功入驻系统', description: '技术部 - 李晓明', time: '昨日 17:30 PM' }
])

let chart: echarts.ECharts | null = null

const handleNewTask = () => {
  router.push({ path: '/order', query: { action: 'new' } })
}

const initChart = () => {
  if (!chartRef.value) return
  
  chart = echarts.init(chartRef.value)
  
  const option = {
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'cross' }
    },
    legend: {
      data: ['利用率', '成本'],
      bottom: 0
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '15%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: chartPeriod.value === 'week' 
        ? ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
        : ['第1周', '第2周', '第3周', '第4周']
    },
    yAxis: [
      {
        type: 'value',
        name: '利用率(%)',
        min: 80,
        max: 100
      },
      {
        type: 'value',
        name: '成本(万元)',
        min: 0,
        max: 50
      }
    ],
    series: [
      {
        name: '利用率',
        type: 'line',
        smooth: true,
        data: [92.1, 93.5, 94.2, 93.8, 95.1, 94.8, 94.8],
        itemStyle: { color: '#409eff' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(64, 158, 255, 0.3)' },
            { offset: 1, color: 'rgba(64, 158, 255, 0.05)' }
          ])
        }
      },
      {
        name: '成本',
        type: 'bar',
        yAxisIndex: 1,
        data: [32, 28, 35, 30, 33, 29, 31],
        itemStyle: { color: '#67c23a' }
      }
    ]
  }
  
  chart.setOption(option)
}

onMounted(() => {
  nextTick(() => {
    initChart()
  })
})
</script>

<style scoped>
.dashboard {
  max-width: 1400px;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
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

.search-input {
  width: 280px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  margin-bottom: 24px;
}

.stat-card {
  background: #fff;
  border-radius: 12px;
  padding: 20px;
  display: flex;
  gap: 16px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-icon-blue {
  background: rgba(64, 158, 255, 0.1);
  color: #409eff;
}

.stat-icon-green {
  background: rgba(103, 194, 58, 0.1);
  color: #67c23a;
}

.stat-icon-orange {
  background: rgba(230, 162, 60, 0.1);
  color: #e6a23c;
}

.stat-icon-purple {
  background: rgba(144, 102, 255, 0.1);
  color: #9066ff;
}

.stat-content {
  flex: 1;
}

.stat-trend {
  font-size: 12px;
  margin-bottom: 4px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.trend-up {
  color: #67c23a;
}

.trend-down {
  color: #f56c6c;
}

.stat-label {
  font-size: 13px;
  color: #909399;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #1a1f36;
  margin-bottom: 8px;
}

.stat-footer {
  font-size: 12px;
  color: #909399;
  display: flex;
  align-items: center;
  gap: 4px;
}

.stat-status {
  margin-bottom: 4px;
}

.dashboard-row {
  display: grid;
  grid-template-columns: 1fr 400px;
  gap: 20px;
}

.chart-card {
  min-height: 380px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.chart-container {
  height: 280px;
}

.activity-card {
  min-height: 380px;
}

.activity-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.activity-item {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.activity-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.activity-icon.success {
  background: rgba(103, 194, 58, 0.1);
  color: #67c23a;
}

.activity-icon.loading {
  background: rgba(64, 158, 255, 0.1);
  color: #409eff;
}

.activity-icon.warning {
  background: rgba(230, 162, 60, 0.1);
  color: #e6a23c;
}

.activity-icon.info {
  background: rgba(144, 147, 153, 0.1);
  color: #909399;
}

.activity-content {
  flex: 1;
}

.activity-title {
  font-size: 14px;
  font-weight: 500;
  color: #1a1f36;
  margin-bottom: 4px;
}

.activity-desc {
  font-size: 13px;
  color: #909399;
}

.activity-time {
  font-size: 12px;
  color: #c0c4cc;
}

.activity-footer {
  margin-top: 16px;
  text-align: center;
  border-top: 1px solid #ebeef5;
  padding-top: 12px;
}

@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  
  .dashboard-row {
    grid-template-columns: 1fr;
  }
}
</style>
