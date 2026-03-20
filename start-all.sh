#!/bin/bash
echo "🚀 启动 NestGuard 项目..."
echo "🔧 后端端口: 5000"
echo "🌐 前端端口: 3000"
echo ""

# 启动后端
cd /home/admin/.openclaw/workspace/nestguard2/AluminumApi
echo "正在启动后端服务..."
nohup ./start-backend.sh > backend.log 2>&1 &

# 等待后端启动
sleep 3

# 启动前端
cd /home/admin/.openclaw/workspace/nestguard2/aluminum-app
echo "正在启动前端服务..."
nohup ./start-frontend.sh > frontend.log 2>&1 &

echo ""
echo "✅ 项目启动完成！"
echo "🌐 访问地址: http://你的服务器IP:3000"
echo "👤 登录账号: admin / admin123"
echo "📄 日志文件: backend.log, frontend.log"