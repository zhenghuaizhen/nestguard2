<template>
  <div class="login-container">
    <!-- Left side - Brand & Features -->
    <div class="login-brand">
      <div class="brand-content">
        <div class="brand-logo">
          <img src="/logo.svg" alt="NestGuard" class="logo-img" />
          <div class="logo-text">
            <h1>NestGuard</h1>
            <span class="logo-subtitle">智能套料专家</span>
          </div>
        </div>
        <p class="brand-slogan">智能金属板材套料优化系统</p>
        <p class="brand-desc">通过智能算法优化，将板材利用率提升至 95% 以上，减少材料浪费，降低生产成本，守护企业资源。</p>
        
        <div class="brand-features">
          <div class="feature-item">
            <el-icon :size="24"><Cpu /></el-icon>
            <span>实时动态套料算法</span>
          </div>
          <div class="feature-item">
            <el-icon :size="24"><Connection /></el-icon>
            <span>多终端数据同步备份</span>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Right side - Login Form -->
    <div class="login-form-side">
      <div class="login-form-container">
        <div class="language-selector">
          <el-icon><Promotion /></el-icon>
          <span>简体中文</span>
        </div>
        
        <div class="login-form-header">
          <h2>欢迎回来</h2>
          <p>请输入您的账号和密码访问管理后台</p>
        </div>
        
        <el-form :model="form" :rules="rules" ref="formRef" label-position="top" class="login-form">
          <el-form-item label="账号" prop="username">
            <el-input v-model="form.username" placeholder="请输入账号" size="large" clearable>
              <template #prefix>
                <el-icon><User /></el-icon>
              </template>
            </el-input>
          </el-form-item>
          
          <el-form-item label="密码" prop="password">
            <el-input v-model="form.password" placeholder="请输入密码" size="large" type="password" show-password @keyup.enter="handleLogin">
              <template #prefix>
                <el-icon><Lock /></el-icon>
              </template>
            </el-input>
            <div class="forgot-password">忘记密码？</div>
          </el-form-item>
          
          <el-form-item>
            <el-checkbox v-model="rememberMe">记住登录状态</el-checkbox>
          </el-form-item>
          
          <el-form-item>
            <el-button type="primary" class="login-btn" size="large" :loading="loading" @click="handleLogin">
              立即登录
            </el-button>
          </el-form-item>
        </el-form>
        
        <div class="login-footer">
          <span>当前版本: v1.0.0 (Build {{ buildDate }})</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock, Cpu, Connection, Promotion } from '@element-plus/icons-vue'
import type { FormInstance } from 'element-plus'
import { login } from '../api'

const router = useRouter()
const formRef = ref<FormInstance>()
const loading = ref(false)
const rememberMe = ref(false)

const buildDate = new Date().toISOString().split('T')[0].replace(/-/g, '.')

const form = reactive({ username: '', password: '' })
const rules = {
  username: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const handleLogin = async () => {
  await formRef.value?.validate()
  loading.value = true
  try {
    const res: any = await login(form)
    if (res.success) {
      localStorage.setItem('token', res.data.token)
      localStorage.setItem('displayName', res.data.displayName)
      ElMessage.success('登录成功')
      router.push('/')
    } else {
      ElMessage.error(res.message || '登录失败')
    }
  } catch {
    // handled by interceptor
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  height: 100vh;
  display: flex;
}

.login-brand {
  flex: 1;
  background: linear-gradient(135deg, #1a1f36 0%, #2d3a5c 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60px;
}

.brand-content {
  color: #fff;
  max-width: 480px;
}

.brand-logo {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 24px;
}

.logo-img {
  width: 56px;
  height: 56px;
}

.logo-text {
  display: flex;
  flex-direction: column;
}

.logo-text h1 {
  font-size: 28px;
  font-weight: 600;
  margin: 0;
  letter-spacing: 1px;
}

.logo-subtitle {
  font-size: 14px;
  color: #a0aec0;
  margin-top: 2px;
}

.brand-slogan {
  font-size: 20px;
  color: #a0aec0;
  margin-bottom: 16px;
}

.brand-desc {
  font-size: 14px;
  color: #718096;
  line-height: 1.8;
  margin-bottom: 48px;
}

.brand-features {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 12px;
  color: #a0aec0;
  font-size: 15px;
}

.feature-item .el-icon {
  color: #409eff;
}

.login-form-side {
  width: 480px;
  background: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px;
}

.login-form-container {
  width: 100%;
  max-width: 360px;
}

.language-selector {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #909399;
  font-size: 14px;
  margin-bottom: 48px;
  cursor: pointer;
}

.login-form-header {
  margin-bottom: 32px;
}

.login-form-header h2 {
  font-size: 28px;
  font-weight: 600;
  color: #1a1f36;
  margin: 0 0 8px 0;
}

.login-form-header p {
  font-size: 14px;
  color: #909399;
  margin: 0;
}

.login-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: #1a1f36;
}

.login-form :deep(.el-input__wrapper) {
  border-radius: 8px;
}

.forgot-password {
  text-align: right;
  font-size: 13px;
  color: #409eff;
  cursor: pointer;
  margin-top: 8px;
}

.login-btn {
  width: 100%;
  height: 48px;
  font-size: 16px;
  border-radius: 8px;
}

.login-footer {
  text-align: center;
  color: #c0c4cc;
  font-size: 12px;
  margin-top: 32px;
}

@media (max-width: 900px) {
  .login-brand {
    display: none;
  }
  
  .login-form-side {
    width: 100%;
  }
}
</style>
