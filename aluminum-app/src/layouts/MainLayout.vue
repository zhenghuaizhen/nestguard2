<template>
  <el-container style="height: 100vh">
    <!-- Sidebar -->
    <el-aside width="240px" class="sidebar">
      <div class="sidebar-header">
        <img src="/logo.svg" alt="NestGuard" class="logo-img" />
        <div class="logo-text-wrapper">
          <span class="logo-text">NestGuard</span>
          <span class="logo-subtitle">智能套料专家</span>
        </div>
      </div>
      
      <el-menu
        :default-active="route.path"
        :default-openeds="['nesting-group', 'system-group']"
        background-color="#1a1f36"
        text-color="#a0aec0"
        active-text-color="#409eff"
        router
        class="sidebar-menu"
      >
        <el-menu-item index="/dashboard">
          <el-icon><Monitor /></el-icon>
          <span>主控制台</span>
        </el-menu-item>
        
        <el-menu-item index="/order">
          <el-icon><Document /></el-icon>
          <span>订单管理</span>
        </el-menu-item>
        
        <el-menu-item index="/inventory">
          <el-icon><Box /></el-icon>
          <span>库存管理</span>
        </el-menu-item>
        
        <el-menu-item index="/variety">
          <el-icon><Grid /></el-icon>
          <span>品种设置</span>
        </el-menu-item>
        
        <el-sub-menu index="nesting-group">
          <template #title>
            <el-icon><MagicStick /></el-icon>
            <span>智能算法</span>
          </template>
          <el-menu-item index="/nesting">智能套料任务</el-menu-item>
          <el-menu-item index="/result">历史记录</el-menu-item>
        </el-sub-menu>
        
        <el-sub-menu index="system-group">
          <template #title>
            <el-icon><Setting /></el-icon>
            <span>系统管理</span>
          </template>
          <el-menu-item index="/user">用户管理</el-menu-item>
          <el-menu-item index="/waste">废料设置</el-menu-item>
        </el-sub-menu>
      </el-menu>
    </el-aside>
    
    <el-container>
      <!-- Header -->
      <el-header class="main-header">
        <div class="header-left">
          <h1 class="page-title">{{ currentTitle }}</h1>
        </div>
        <div class="header-right">
          <div class="user-info">
            <el-avatar :size="36" class="user-avatar">{{ displayName?.charAt(0) || 'U' }}</el-avatar>
            <div class="user-detail">
              <span class="user-name">{{ displayName }}</span>
              <span class="user-role">管理员</span>
            </div>
          </div>
          <el-button type="danger" text @click="logout">
            <el-icon><SwitchButton /></el-icon>
            退出系统
          </el-button>
        </div>
      </el-header>
      
      <!-- Main Content -->
      <el-main class="main-content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Monitor, Document, Box, Grid, MagicStick, Setting, SwitchButton } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()

const pageTitles: Record<string, string> = {
  '/dashboard': '主控制台',
  '/order': '订单管理',
  '/inventory': '库存管理',
  '/variety': '品种设置',
  '/nesting': '智能套料任务',
  '/result': '历史记录',
  '/user': '用户管理',
  '/waste': '废料设置'
}

const currentTitle = computed(() => pageTitles[route.path] || 'NestGuard 智能套料专家')

const displayName = computed(() => localStorage.getItem('displayName') || '用户')

const logout = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('displayName')
  router.push('/login')
}
</script>

<style scoped>
.sidebar {
  background: #1a1f36;
  display: flex;
  flex-direction: column;
}

.sidebar-header {
  height: 64px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.logo-img {
  width: 36px;
  height: 36px;
}

.logo-text-wrapper {
  display: flex;
  flex-direction: column;
  margin-left: 10px;
}

.logo-text {
  font-size: 16px;
  font-weight: 600;
  color: #fff;
  line-height: 1.2;
}

.logo-subtitle {
  font-size: 11px;
  color: #a0aec0;
  line-height: 1.2;
}

.sidebar-menu {
  border-right: none;
  flex: 1;
}

.sidebar-menu :deep(.el-sub-menu__title) {
  height: 48px;
  line-height: 48px;
}

.sidebar-menu :deep(.el-menu-item) {
  height: 48px;
  line-height: 48px;
}

.sidebar-menu :deep(.el-menu-item:hover) {
  background-color: rgba(255, 255, 255, 0.05) !important;
}

.sidebar-menu :deep(.el-menu-item.is-active) {
  background-color: rgba(64, 158, 255, 0.1) !important;
}

.main-header {
  background: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  height: 64px;
}

.header-left {
  display: flex;
  align-items: center;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: #1a1f36;
  margin: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 24px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-avatar {
  background: linear-gradient(135deg, #409eff 0%, #66b1ff 100%);
}

.user-detail {
  display: flex;
  flex-direction: column;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: #1a1f36;
}

.user-role {
  font-size: 12px;
  color: #909399;
}

.main-content {
  background: #f5f7fa;
  padding: 24px;
  overflow-y: auto;
}
</style>
