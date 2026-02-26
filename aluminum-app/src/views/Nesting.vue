<template>
  <div class="page-container">
    <!-- Page Header -->
    <div class="page-header">
      <div class="header-info">
        <h1>新建套料任务</h1>
        <p>日期: {{ currentDate }}</p>
      </div>
    </div>

    <div class="nesting-layout">
      <!-- Left Panel - Task Setup -->
      <div class="task-panel">
        <!-- Step 1: Select Orders -->
        <div class="step-section">
          <div class="step-header">
            <div class="step-number">1</div>
            <span class="step-title">待排订单 ({{ selectedIds.length }})</span>
          </div>
          <div class="step-content">
            <!-- Search Filter -->
            <div class="order-filter">
              <el-input v-model="orderSearchKeyword" placeholder="搜索订单/客户" clearable size="small" style="width: 200px;" @keyup.enter="filterOrders" />
              <el-button type="primary" size="small" @click="filterOrders">查询</el-button>
            </div>
            <el-table 
              ref="orderTableRef"
              :data="filteredOrders" 
              border 
              size="small" 
              @select="onOrderSelect"
              @select-all="onSelectAll"
              v-loading="loading" 
              max-height="250"
            >
              <el-table-column type="selection" width="40" />
              <el-table-column prop="orderId" label="订单号" min-width="100" />
              <el-table-column prop="customer" label="客户" min-width="80" />
              <el-table-column prop="materialName" label="品种" min-width="80" />
              <el-table-column label="规格 (mm)" min-width="120">
                <template #default="{ row }">
                  {{ row.width }}x{{ row.length }}x{{ row.thickness }}
                </template>
              </el-table-column>
              <el-table-column prop="quantity" label="件数" width="60" />
              <el-table-column prop="weight" label="重量(kg)" width="80" />
            </el-table>
          </div>
        </div>

        <!-- Step 2: Select Board -->
        <div class="step-section">
          <div class="step-header">
            <div class="step-number">2</div>
            <span class="step-title">库存关联</span>
          </div>
          <div class="step-content">
            <el-table ref="inventoryTableRef" :data="sortedInventory" border size="small" @selection-change="onInventorySelectChange" max-height="200" :row-class-name="inventoryRowClass" :key="inventoryTableKey">
              <el-table-column type="selection" width="40" />
              <el-table-column prop="barcode" label="条码号" min-width="100" />
              <el-table-column prop="materialName" label="品种" min-width="80" />
              <el-table-column label="规格 (mm)" min-width="120">
                <template #default="{ row }">
                  {{ row.width }}x{{ row.length }}x{{ row.thickness }}
                </template>
              </el-table-column>
              <el-table-column prop="quantity" label="件数" width="60" />
              <el-table-column prop="weight" label="重量(kg)" width="80" />
            </el-table>
          </div>
        </div>

        <!-- Step 3: Strategy -->
        <div class="step-section">
          <div class="step-header">
            <div class="step-number">3</div>
            <span class="step-title">策略配置</span>
            <el-tooltip placement="right" effect="light" :show-after="300">
              <template #content>
                <div class="strategy-tooltip">
                  <div class="tooltip-section">
                    <div class="tooltip-title">【选料策略】（系统自动）</div>
                    <div class="tooltip-item">系统自动优先选择能满足需求的最小板材，大板保留给更大的订单；同时优先消耗库存余料。</div>
                  </div>
                  <div class="tooltip-section">
                    <div class="tooltip-title">【余料判定规则】</div>
                    <div class="tooltip-item">切割后剩余区域需同时满足以下条件才可入库为余料：</div>
                    <div class="tooltip-item">• 长度 ≥ 最小长度</div>
                    <div class="tooltip-item">• 宽度 ≥ 最小宽度</div>
                    <div class="tooltip-item">• 面积 ≥ 最大废料面积</div>
                    <div class="tooltip-item">任一条件不满足则判定为废料。（阈值可在"废料设置"中配置）</div>
                  </div>
                  <div class="tooltip-section">
                    <div class="tooltip-title">【其他设置】</div>
                    <div class="tooltip-item">• 计算模式：快速/精确，影响计算时间和结果精度</div>
                    <div class="tooltip-item">• 切割方向：根据设备特点选择</div>
                    <div class="tooltip-item">• 固定方向：零件不允许旋转，适合有纹理材料</div>
                  </div>
                </div>
              </template>
              <el-icon class="help-icon"><QuestionFilled /></el-icon>
            </el-tooltip>
          </div>
          <div class="step-content">
            <!-- 计算模式 -->
            <div class="strategy-group">
              <div class="strategy-label">计算模式</div>
              <el-radio-group v-model="calcMode" size="small">
                <el-radio-button value="fast">快速计算</el-radio-button>
                <el-radio-button value="precise">精确计算</el-radio-button>
              </el-radio-group>
            </div>
            <!-- 切割方向 -->
            <div class="strategy-group">
              <div class="strategy-label">切割方向</div>
              <el-radio-group v-model="cutDirection" size="small">
                <el-radio-button value="auto">自动</el-radio-button>
                <el-radio-button value="horizontal">横向优先</el-radio-button>
                <el-radio-button value="vertical">纵向优先</el-radio-button>
              </el-radio-group>
            </div>
            <!-- 固定方向 -->
            <div class="strategy-group">
              <el-checkbox v-model="fixedDirection">固定方向（零件不旋转，适合有纹理材料）</el-checkbox>
            </div>
          </div>
        </div>

        <!-- Action Button -->
        <el-button type="primary" size="large" class="calculate-btn" :loading="calculating" :disabled="!selectedIds.length" @click="handleCalculate">
          <el-icon><Cpu /></el-icon>
          开始计算智能套料
        </el-button>
      </div>

      <!-- Right Panel - Canvas Workspace -->
      <div class="workspace-panel">
        <div v-if="!nestingResult" class="workspace-placeholder">
          <div class="placeholder-content">
            <el-icon :size="48"><Picture /></el-icon>
            <p>Interactive CAD Nesting Space</p>
            <p class="placeholder-hint">选择订单并开始计算后，套料结果将在此显示</p>
          </div>
        </div>
        
        <div v-else class="workspace-content">
          <div class="workspace-header">
            <div class="workspace-title">
              <el-icon><Picture /></el-icon>
              <span>Interactive CAD Nesting Space</span>
            </div>
            <div class="workspace-info">
              <span>板材: {{ nestingResult.materialName }} / {{ nestingResult.thickness }}mm</span>
            </div>
          </div>
          
          <div class="workspace-stats">
            <div class="stat-item">
              <span class="stat-value">{{ nestingResult.completedOrders }}</span>
              <span class="stat-label">已用个数:</span>
            </div>
            <div class="stat-item">
              <span class="stat-value">{{ nestingResult.productWeight || '-' }} kg</span>
              <span class="stat-label">成品重量:</span>
            </div>
            <div class="stat-item">
              <span class="stat-value">{{ nestingResult.remnantWeight || '-' }} kg</span>
              <span class="stat-label">余料重量:</span>
            </div>
            <div class="stat-item">
              <span class="stat-value">{{ nestingResult.wasteWeight || '-' }} kg</span>
              <span class="stat-label">废料重量:</span>
            </div>
            <div class="stat-divider"></div>
            <div class="stat-item">
              <span class="stat-value">{{ nestingResult.wasteRate || 0 }}%</span>
              <span class="stat-label">损耗率:</span>
            </div>
            <div class="stat-item highlight">
              <span class="stat-value">{{ nestingResult.totalUtilization }}%</span>
              <span class="stat-label">当前利用率:</span>
            </div>
            <div class="stat-item">
              <span class="stat-value">{{ nestingResult.calculateTime || '-' }}</span>
              <span class="stat-label">计算用时:</span>
            </div>
          </div>

          <div class="workspace-status">
            <el-tag type="success">NestGuard Engine v1.0 Active</el-tag>
          </div>

          <!-- Canvas Tabs -->
          <el-tabs v-model="activeTab" class="canvas-tabs">
            <el-tab-pane v-for="(item, index) in nestingResult.items" :key="index" :label="item.barcode || `板材 ${index + 1}`" :name="String(index)">
              <div class="canvas-info">
                条码号: {{ item.barcode || '-' }} | 
                规格: {{ item.sourceWidth }} x {{ item.sourceLength }} mm | 
                利用率: {{ item.utilization }}% | 
                类型: {{ item.materialType === 1 ? '余料' : '母材' }} |
                成品重量: {{ item.productWeight }} kg |
                余料重量: {{ item.remnantWeight }} kg |
                废料重量: {{ item.wasteWeight }} kg
              </div>
              <div class="canvas-container">
                <canvas :ref="el => canvasRefs[index] = el" class="nesting-canvas"></canvas>
              </div>
            </el-tab-pane>
          </el-tabs>

          <!-- Action Buttons -->
          <div class="workspace-actions">
            <el-button type="primary" @click="handleExportPlan">
              <el-icon><Download /></el-icon>
              导出方案
            </el-button>
            <el-button type="success" :loading="confirming" @click="handleConfirm">
              <el-icon><Check /></el-icon>
              确认采用方案
            </el-button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Cpu, Picture, Download, Check, QuestionFilled } from '@element-plus/icons-vue'
import * as XLSX from 'xlsx'
import { getOrders, calculateNesting, confirmNesting, getInventory } from '../api'

const loading = ref(false)
const calculating = ref(false)
const confirming = ref(false)
const allOrders = ref<any[]>([])
const allInventory = ref<any[]>([])
const availableInventory = ref<any[]>([])
const page = ref(1)
const pageSize = ref(50)
const total = ref(0)
const selectedIds = ref<number[]>([])
const selectedInventoryIds = ref<number[]>([])
// Strategy configuration
const calcMode = ref('fast')
const cutDirection = ref('auto')
const fixedDirection = ref(false)
const nestingResult = ref<any>(null)
const activeTab = ref('0')
const canvasRefs = ref<any[]>([])
const orderTableRef = ref<any>(null)
const inventoryTableRef = ref<any>(null)
const inventoryTableKey = ref(0)
const orderSearchKeyword = ref('')
const selectedMaterial = ref<string | null>(null)
const selectedThickness = ref<number | null>(null)

const currentDate = new Date().toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric' })

// Sort inventory: selected items (green) at top
const sortedInventory = computed(() => {
  const selected = availableInventory.value.filter(i => selectedInventoryIds.value.includes(i.id))
  const unselected = availableInventory.value.filter(i => !selectedInventoryIds.value.includes(i.id))
  return [...selected, ...unselected]
})

const inventoryRowClass = ({ row }: { row: any }) => {
  return selectedInventoryIds.value.includes(row.id) ? 'selected-row' : ''
}

// Filter orders based on search keyword
const filteredOrders = computed(() => {
  if (!orderSearchKeyword.value.trim()) {
    return allOrders.value
  }
  
  const keyword = orderSearchKeyword.value.toLowerCase().trim()
  return allOrders.value.filter((order: any) =>
    order.orderId?.toLowerCase().includes(keyword) ||
    order.customer?.toLowerCase().includes(keyword) ||
    order.materialName?.toLowerCase().includes(keyword)
  )
})

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getOrders({ page: page.value, pageSize: pageSize.value })
    if (res.success) {
      allOrders.value = res.data.items.filter((o: any) => o.status === 0)
    }
  } finally {
    loading.value = false
  }
}

const loadInventory = async () => {
  try {
    const res: any = await getInventory({ page: 1, pageSize: 100 })
    if (res.success) {
      allInventory.value = res.data.items
    }
  } catch {
    // ignore
  }
}

// Handle single row select
const onOrderSelect = (selection: any[], row: any) => {
  const isSelected = selection.some(r => r.id === row.id)
  
  if (isSelected) {
    // User is trying to select this row
    if (selectedMaterial.value === null) {
      // First selection - allow and set material/thickness
      selectedMaterial.value = row.materialName
      selectedThickness.value = row.thickness
      selectedIds.value = [row.id]
      loadMatchingInventory(row.materialName, row.thickness)
    } else {
      // Check if this row matches current selection criteria
      if (row.materialName !== selectedMaterial.value || row.thickness !== selectedThickness.value) {
        ElMessage.warning('只能勾选同一品种和厚度的订单')
        // Deselect this row
        nextTick(() => {
          if (orderTableRef.value) {
            orderTableRef.value.toggleRowSelection(row, false)
          }
        })
        return
      }
      // Valid selection - add to selectedIds
      selectedIds.value = selection.map(r => r.id)
    }
  } else {
    // User is deselecting this row
    selectedIds.value = selection.map(r => r.id)
    if (selection.length === 0) {
      selectedMaterial.value = null
      selectedThickness.value = null
      availableInventory.value = []
    }
  }
}

// Handle select all
const onSelectAll = (selection: any[]) => {
  if (selection.length === 0) {
    // User deselected all
    selectedIds.value = []
    selectedMaterial.value = null
    selectedThickness.value = null
    availableInventory.value = []
    return
  }
  
  if (selectedMaterial.value === null) {
    // No existing selection - check if all selected rows have same material/thickness
    const firstItem = selection[0]
    const allSame = selection.every(item => 
      item.materialName === firstItem.materialName && item.thickness === firstItem.thickness
    )
    
    if (!allSame) {
      ElMessage.warning('只能勾选同一品种和厚度的订单')
      // Deselect all
      nextTick(() => {
        if (orderTableRef.value) {
          orderTableRef.value.clearSelection()
        }
      })
      return
    }
    
    selectedMaterial.value = firstItem.materialName
    selectedThickness.value = firstItem.thickness
    selectedIds.value = selection.map(r => r.id)
    loadMatchingInventory(firstItem.materialName, firstItem.thickness)
  } else {
    // Already have selection - filter out invalid rows
    const validSelection = selection.filter(item => 
      item.materialName === selectedMaterial.value && item.thickness === selectedThickness.value
    )
    
    if (validSelection.length !== selection.length) {
      ElMessage.warning('只能勾选同一品种和厚度的订单')
      // Restore to valid selection
      nextTick(() => {
        if (orderTableRef.value) {
          orderTableRef.value.clearSelection()
          validSelection.forEach(row => {
            orderTableRef.value.toggleRowSelection(row, true)
          })
        }
      })
      return
    }
    
    selectedIds.value = validSelection.map(r => r.id)
  }
}

// Load inventory filtered by material and thickness
const loadMatchingInventory = async (materialName: string, thickness: number) => {
  try {
    const res: any = await getInventory({ 
      page: 1, 
      pageSize: 100
    })
    
    if (res.success) {
      // Filter inventory by material name and thickness
      availableInventory.value = res.data.items.filter((item: any) => 
        item.status === 0 && 
        item.materialName === materialName && 
        item.thickness === thickness
      )
    }
  } catch {
    // ignore
  }
}

const onInventorySelectChange = (rows: any[]) => {
  selectedInventoryIds.value = rows.map(r => r.id)
  // Force table to re-render to update row class
  inventoryTableKey.value++
}

const filterOrders = () => {
  // Trigger computed property re-evaluation - filteredOrders is computed automatically
}

const handleCalculate = async () => {
  calculating.value = true
  try {
    const res: any = await calculateNesting({ 
      orderIds: selectedIds.value,
      calcMode: calcMode.value,
      cutDirection: cutDirection.value,
      fixedDirection: fixedDirection.value
    })
    if (res.success) {
      nestingResult.value = res.data
      activeTab.value = '0'
      
      // Auto-select inventory based on calculation result
      let usedInventoryIds: number[] = []
      if (res.data.usedInventoryIds && Array.isArray(res.data.usedInventoryIds)) {
        usedInventoryIds = res.data.usedInventoryIds
      } else if (res.data.items && res.data.items.length > 0) {
        // Extract inventory IDs from items
        const usedIds = new Set<number>()
        res.data.items.forEach((item: any) => {
          if (item.inventoryId) usedIds.add(item.inventoryId)
        })
        usedInventoryIds = Array.from(usedIds)
      }
      
      if (usedInventoryIds.length > 0) {
        selectedInventoryIds.value = usedInventoryIds
        // Force table to re-render to update row class and sorting
        inventoryTableKey.value++
        // Programmatically select rows in the inventory table
        // Wait for sortedInventory to recompute and table to re-render
        nextTick(() => {
          nextTick(() => {
            if (inventoryTableRef.value) {
              inventoryTableRef.value.clearSelection()
              // sortedInventory has already been recomputed with selected items at top
              sortedInventory.value.forEach((row: any) => {
                if (usedInventoryIds.includes(row.id)) {
                  inventoryTableRef.value.toggleRowSelection(row, true)
                }
              })
            }
          })
        })
      }
      
      ElMessage.success(res.message)
      nextTick(() => {
        drawAllCanvases()
      })
    } else {
      ElMessage.error(res.message || '计算失败')
    }
  } finally {
    calculating.value = false
  }
}

const handleExportPlan = () => {
  if (!nestingResult.value || !nestingResult.value.items || nestingResult.value.items.length === 0) {
    ElMessage.warning('没有可导出的套料方案')
    return
  }

  const wb = XLSX.utils.book_new()
  
  // 汇总信息表
  const summaryData = [
    ['NestGuard 智能套料专家 - 套料方案报告'],
    [''],
    ['生成时间', new Date().toLocaleString('zh-CN')],
    [''],
    ['品种', nestingResult.value.materialName],
    ['厚度(mm)', nestingResult.value.thickness],
    ['使用板材数量', nestingResult.value.totalPlates],
    ['完成订单数', nestingResult.value.completedOrders],
    ['成品重量(kg)', nestingResult.value.productWeight],
    ['余料重量(kg)', nestingResult.value.remnantWeight],
    ['废料重量(kg)', nestingResult.value.wasteWeight],
    ['损耗率(%)', nestingResult.value.wasteRate],
    ['平均利用率(%)', nestingResult.value.totalUtilization],
    ['计算用时', nestingResult.value.calculateTime],
  ]
  const summarySheet = XLSX.utils.aoa_to_sheet(summaryData)
  XLSX.utils.book_append_sheet(wb, summarySheet, '汇总信息')

  // 详情表 - 按母板分组
  const detailData: any[][] = [
    ['母板条码号', '母板规格(mm)', '母板类型', '利用率(%)', '订单号', '订单规格(mm)', '数量', '成品重量(kg)', '余料尺寸(mm)', '余料重量(kg)', '废料重量(kg)'],
  ]

  nestingResult.value.items.forEach((item: any) => {
    const materialTypeStr = item.materialType === 1 ? '余料' : '母材'
    const remnantSize = item.hasRemnant ? `${item.remnantWidth}x${item.remnantLength}` : '-'
    
    if (item.placements && item.placements.length > 0) {
      // 按订单号分组统计数量
      const orderMap = new Map<string, { count: number, specs: string[] }>()
      item.placements.forEach((p: any) => {
        if (p.status === '使用') {
          const spec = `${p.width}x${p.length}`
          if (!orderMap.has(p.orderIdStr)) {
            orderMap.set(p.orderIdStr, { count: 0, specs: [] })
          }
          const order = orderMap.get(p.orderIdStr)!
          order.count++
          if (!order.specs.includes(spec)) {
            order.specs.push(spec)
          }
        }
      })

      const orderEntries = Array.from(orderMap.entries())
      
      orderEntries.forEach(([orderId, data], idx: number) => {
        const specsStr = data.specs.join('; ')
        detailData.push([
          idx === 0 ? item.barcode : '',
          idx === 0 ? `${item.sourceWidth}x${item.sourceLength}` : '',
          idx === 0 ? materialTypeStr : '',
          idx === 0 ? item.utilization : '',
          orderId,
          specsStr,
          data.count,
          idx === 0 ? item.productWeight : '',
          idx === 0 ? remnantSize : '',
          idx === 0 ? item.remnantWeight : '',
          idx === 0 ? item.wasteWeight : '',
        ])
      })
    } else {
      // 无订单的母板
      detailData.push([
        item.barcode,
        `${item.sourceWidth}x${item.sourceLength}`,
        materialTypeStr,
        item.utilization,
        '-',
        '-',
        '-',
        item.productWeight,
        remnantSize,
        item.remnantWeight,
        item.wasteWeight,
      ])
    }
  })

  const detailSheet = XLSX.utils.aoa_to_sheet(detailData)
  
  // 设置列宽
  detailSheet['!cols'] = [
    { wch: 15 }, // 母板条码号
    { wch: 18 }, // 母板规格
    { wch: 10 }, // 母板类型
    { wch: 12 }, // 利用率
    { wch: 18 }, // 订单号
    { wch: 20 }, // 订单规格
    { wch: 8 },  // 数量
    { wch: 14 }, // 成品重量
    { wch: 16 }, // 余料尺寸
    { wch: 14 }, // 余料重量
    { wch: 14 }, // 废料重量
  ]
  
  XLSX.utils.book_append_sheet(wb, detailSheet, '套料明细')

  // 导出文件
  const fileName = `套料方案_${nestingResult.value.materialName}_${new Date().toISOString().slice(0, 10)}.xlsx`
  XLSX.writeFile(wb, fileName)
  
  ElMessage.success('方案已导出')
}



const handleConfirm = async () => {
  if (!nestingResult.value || !nestingResult.value.items || nestingResult.value.items.length === 0) {
    ElMessage.warning('没有可确认的套料方案')
    return
  }
  
  try {
    await ElMessageBox.confirm(
      '确认采用此套料方案？确认后将更新订单状态、标记库存为已使用、生成余料记录。',
      '确认方案',
      { confirmButtonText: '确认', cancelButtonText: '取消', type: 'warning' }
    )
  } catch {
    return
  }
  
  confirming.value = true
  try {
    const res: any = await confirmNesting(nestingResult.value)
    if (res.success) {
      ElMessage.success(res.message || '方案已确认')
      nestingResult.value = null
      selectedIds.value = []
      selectedMaterial.value = null
      selectedThickness.value = null
      availableInventory.value = []
      loadData()
    } else {
      ElMessage.error(res.message || '确认失败')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '确认失败，请检查网络连接')
  } finally {
    confirming.value = false
  }
}

const drawAllCanvases = () => {
  if (!nestingResult.value?.items) return
  nestingResult.value.items.forEach((item: any, index: number) => {
    drawCanvas(index, item)
  })
}

const drawCanvas = (index: number, item: any) => {
  const canvas = canvasRefs.value[index]
  if (!canvas) return

  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const maxWidth = 600
  const maxHeight = 400
  const scale = Math.min(maxWidth / item.sourceWidth, maxHeight / item.sourceLength)
  
  canvas.width = item.sourceWidth * scale
  canvas.height = item.sourceLength * scale

  ctx.fillStyle = '#f5f7fa'
  ctx.fillRect(0, 0, canvas.width, canvas.height)

  ctx.strokeStyle = '#dcdfe6'
  ctx.lineWidth = 2
  ctx.strokeRect(0, 0, canvas.width, canvas.height)

  item.placements.forEach((p: any) => {
    const x = p.x * scale
    const y = p.y * scale
    const w = p.width * scale
    const h = p.length * scale

    ctx.fillStyle = p.color || '#67C23A'
    ctx.fillRect(x, y, w, h)

    ctx.strokeStyle = '#fff'
    ctx.lineWidth = 1
    ctx.strokeRect(x, y, w, h)

    if (p.status === '使用' && w > 30 && h > 20) {
      ctx.fillStyle = '#fff'
      ctx.font = '12px Arial'
      ctx.textAlign = 'center'
      ctx.textBaseline = 'middle'
      ctx.fillText(p.orderIdStr || '', x + w/2, y + h/2 - 8)
      ctx.font = '10px Arial'
      ctx.fillText(`${p.width}x${p.length}`, x + w/2, y + h/2 + 8)
    }
  })
}

watch(activeTab, () => {
  nextTick(() => {
    drawAllCanvases()
  })
})

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.page-container {
  max-width: 1400px;
}

.page-header {
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

.nesting-layout {
  display: grid;
  grid-template-columns: 420px 1fr;
  gap: 20px;
}

.task-panel {
  background: #fff;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
}

.step-section {
  margin-bottom: 20px;
}

.step-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.step-number {
  width: 24px;
  height: 24px;
  background: #409eff;
  color: #fff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 600;
}

.step-title {
  font-weight: 600;
  color: #1a1f36;
}

.step-content {
  padding-left: 36px;
}

.order-filter {
  display: flex;
  gap: 8px;
  margin-bottom: 10px;
}

.calculate-btn {
  width: 100%;
  height: 48px;
  font-size: 16px;
  margin-top: 20px;
}

/* Selected inventory row styling */
:deep(.selected-row) {
  background-color: #e8f5e9 !important;
}

:deep(.selected-row td) {
  background-color: #e8f5e9 !important;
}

.workspace-panel {
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
  min-height: 600px;
}

.workspace-placeholder {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 500px;
}

.placeholder-content {
  text-align: center;
  color: #c0c4cc;
}

.placeholder-content p {
  margin: 16px 0 0 0;
  font-size: 16px;
}

.placeholder-hint {
  font-size: 13px !important;
  color: #c0c4cc;
}

.workspace-content {
  padding: 20px;
}

.workspace-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.workspace-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: #1a1f36;
}

.workspace-info {
  font-size: 13px;
  color: #909399;
}

.workspace-stats {
  display: flex;
  gap: 24px;
  margin-bottom: 16px;
  padding: 12px 16px;
  background: #f8fafc;
  border-radius: 8px;
}

.stat-item {
  display: flex;
  flex-direction: column;
}

.stat-item.highlight .stat-value {
  color: #67c23a;
  font-weight: 600;
}

.stat-value {
  font-size: 18px;
  font-weight: 600;
  color: #1a1f36;
}

.stat-label {
  font-size: 12px;
  color: #909399;
}

.stat-divider {
  width: 1px;
  background: #e4e7ed;
  margin: 0 8px;
}

.workspace-status {
  margin-bottom: 16px;
}

.canvas-tabs {
  margin-bottom: 16px;
}

.canvas-info {
  font-size: 13px;
  color: #606266;
  margin-bottom: 12px;
}

.canvas-container {
  border: 1px solid #ebeef5;
  border-radius: 8px;
  overflow: hidden;
}

.nesting-canvas {
  display: block;
  background: #fafafa;
}

.workspace-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  padding-top: 16px;
  border-top: 1px solid #ebeef5;
}

@media (max-width: 1000px) {
  .nesting-layout {
    grid-template-columns: 1fr;
  }
}

/* Strategy configuration styles */
.help-icon {
  color: #909399;
  cursor: pointer;
  font-size: 16px;
  margin-left: 4px;
}

.help-icon:hover {
  color: #409eff;
}

.strategy-tooltip {
  max-width: 380px;
  font-size: 13px;
  line-height: 1.6;
}

.tooltip-section {
  margin-bottom: 12px;
}

.tooltip-section:last-child {
  margin-bottom: 0;
}

.tooltip-title {
  font-weight: 600;
  color: #1a1f36;
  margin-bottom: 4px;
}

.tooltip-item {
  color: #606266;
  padding-left: 4px;
}

.strategy-group {
  margin-bottom: 12px;
}

.strategy-group:last-child {
  margin-bottom: 0;
}

.strategy-label {
  font-size: 12px;
  color: #909399;
  margin-bottom: 6px;
}
</style>
