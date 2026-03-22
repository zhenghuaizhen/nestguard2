#!/bin/bash

echo "🚀 开始 NestGuard 套料算法综合测试"
echo "========================================"

# 获取 token
TOKEN=$(curl -s -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.data.token')

if [ "$TOKEN" == "null" ]; then
    echo "❌ 认证失败"
    exit 1
fi

echo "✅ 认证成功"

# 测试不同的策略组合
strategies=("minMatch" "remnantFirst" "largeFirst")
directions=("horizontal" "vertical" "auto")

TOTAL_TESTS=0
SUCCESS_TESTS=0
TOTAL_UTILIZATION=0
TOTAL_WASTE_RATE=0

# 获取所有可套料订单并按品种和厚度分组，取第一组
ORDERS_JSON=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/nesting/available-orders")
ORDER_IDS=$(echo "$ORDERS_JSON" | jq -r '.data | group_by(.materialName, .thickness) | .[0] | [.[].id] | join(",")')

if [ -z "$ORDER_IDS" ] || [ "$ORDER_IDS" == "[]" ] || [ "$ORDER_IDS" == "null" ]; then
    echo "⚠️  没有找到可套料的订单"
    exit 0
fi

echo "📋 找到匹配的套料订单 ID: $ORDER_IDS"

# 对每种策略和方向进行测试
for strategy in "${strategies[@]}"; do
    for direction in "${directions[@]}"; do
        echo "⚙️  测试策略: $strategy, 方向: $direction"
        
        # 构建请求数据
        REQUEST_DATA="{\"orderIds\":[$ORDER_IDS],\"selectStrategy\":\"$strategy\",\"cutDirection\":\"$direction\",\"fixedDirection\":false}"
        
        # 执行套料计算
        RESULT=$(curl -s -X POST "http://localhost:5000/api/nesting/calculate" \
          -H "Authorization: Bearer $TOKEN" \
          -H "Content-Type: application/json" \
          -d "$REQUEST_DATA")
        
        SUCCESS=$(echo "$RESULT" | jq -r '.success')
        if [ "$SUCCESS" == "true" ]; then
            UTILIZATION=$(echo "$RESULT" | jq -r '.data.totalUtilization')
            WASTE_RATE=$(echo "$RESULT" | jq -r '.data.wasteRate')
            
            echo "   ✅ 成功 - 利用率: ${UTILIZATION}%, 废料率: ${WASTE_RATE}%"
            
            TOTAL_TESTS=$((TOTAL_TESTS + 1))
            SUCCESS_TESTS=$((SUCCESS_TESTS + 1))
            TOTAL_UTILIZATION=$(echo "$TOTAL_UTILIZATION $UTILIZATION" | awk '{print $1 + $2}')
            TOTAL_WASTE_RATE=$(echo "$TOTAL_WASTE_RATE $WASTE_RATE" | awk '{print $1 + $2}')
            
            # 保存详细结果
            echo "$RESULT" >> nesting-detailed-results.json
            
        else
            MESSAGE=$(echo "$RESULT" | jq -r '.message')
            echo "   ❌ 失败 - $MESSAGE"
        fi
        
        # 短暂延迟避免过载
        sleep 0.5
    done
done

# 输出汇总结果
echo "========================================"
echo "📈 测试结果汇总:"
echo "   总测试次数: $TOTAL_TESTS"
echo "   成功次数: $SUCCESS_TESTS"

if [ $TOTAL_TESTS -gt 0 ]; then
    AVG_UTILIZATION=$(echo "$TOTAL_UTILIZATION $TOTAL_TESTS" | awk '{printf "%.2f", $1 / $2}')
    AVG_WASTE_RATE=$(echo "$TOTAL_WASTE_RATE $TOTAL_TESTS" | awk '{printf "%.2f", $1 / $2}')
    SUCCESS_RATE=$(echo "$SUCCESS_TESTS $TOTAL_TESTS" | awk '{printf "%.2f", $1 * 100 / $2}')
    
    echo "   平均利用率: ${AVG_UTILIZATION}%"
    echo "   平均废料率: ${AVG_WASTE_RATE}%"
    echo "   成功率: ${SUCCESS_RATE}%"
    
    # 评估算法性能
    if [ $(echo "$AVG_UTILIZATION" | awk '{if ($1 > 80) print 1; else print 0}') -eq 1 ]; then
        echo "🎯 算法评估: 优秀 (高利用率)"
    elif [ $(echo "$AVG_UTILIZATION" | awk '{if ($1 > 60) print 1; else print 0}') -eq 1 ]; then
        echo "👍 算法评估: 良好"
    else
        echo "⚠️  算法评估: 需要优化"
    fi
else
    echo "⚠️  没有成功测试，无法评估算法性能"
fi

echo "📋 详细结果已保存到 nesting-detailed-results.json"