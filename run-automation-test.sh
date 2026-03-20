#!/bin/bash

echo "🚀 开始 NestGuard 套料算法自动化测试"
echo "========================================"

# 获取认证 token
echo "🔐 获取认证 token..."
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.data.token')

if [ "$TOKEN" == "null" ]; then
  echo "❌ 认证失败"
  exit 1
fi

echo "✅ 认证成功"

# 清理现有测试数据
echo "🧹 清理现有测试数据..."
curl -s -X DELETE http://localhost:5000/api/order/batch-delete \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '[2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101]' > /dev/null

curl -s -X DELETE http://localhost:5000/api/inventory/batch-delete \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '[2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101]' > /dev/null

echo "✅ 测试数据清理完成"

# 生成测试数据
echo "📊 生成测试数据..."
node test-automation.js

echo "✅ 测试数据生成完成"

# 运行套料测试
echo "⚙️  运行套料算法测试..."
node run-nesting-test.js "$TOKEN"

echo "✅ 自动化测试完成！"
echo "========================================"
echo "📋 测试结果已保存到 nesting-test-results.json"