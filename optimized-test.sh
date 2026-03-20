#!/bin/bash

echo "🚀 开始 NestGuard 套料算法优化测试"
echo "========================================"

# 获取认证 token
TOKEN=$(curl -s -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.data.token')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "❌ 认证失败"
    exit 1
fi

echo "✅ 认证成功"

# 获取可套料的订单
ORDER_IDS=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/nesting/available-orders" | jq -r '[.data[].id]')

if [ "$ORDER_IDS" = "[]" ]; then
    echo "❌ 没有可套料的订单"
    exit 1
fi

echo "📋 找到可套料订单: $ORDER_IDS"

# 测试不同的策略组合
STRATEGIES=("minMatch" "remnantFirst" "largeFirst")
DIRECTIONS=("horizontal" "vertical" "auto")

TOTAL_TESTS=0
SUCCESS_TESTS=0
BEST_UTILIZATION=0
BEST_STRATEGY=""
BEST_DIRECTION=""

RESULTS_FILE="nesting-optimization-results.json"
echo "[" > $RESULTS_FILE

for strategy in "${STRATEGIES[@]}"; do
    for direction in "${DIRECTIONS[@]}"; do
        echo "⚙️  测试策略: $strategy, 方向: $direction"
        
        # 执行套料计算
        RESPONSE=$(curl -s -X POST "http://localhost:5000/api/nesting/calculate" \
          -H "Authorization: Bearer $TOKEN" \
          -H "Content-Type: application/json" \
          -d "{\"orderIds\":$ORDER_IDS,\"selectStrategy\":\"$strategy\",\"cutDirection\":\"$direction\"}")
        
        SUCCESS=$(echo "$RESPONSE" | jq -r '.success')
        if [ "$SUCCESS" = "true" ]; then
            UTILIZATION=$(echo "$RESPONSE" | jq -r '.data.totalUtilization')
            WASTE_RATE=$(echo "$RESPONSE" | jq -r '.data.wasteRate')
            CALCULATE_TIME=$(echo "$RESPONSE" | jq -r '.data.calculateTime')
            
            echo "   ✅ 利用率: ${UTILIZATION}%, 废料率: ${WASTE_RATE}%, 耗时: $CALCULATE_TIME"
            
            # 记录结果
            RESULT="{\"strategy\":\"$strategy\",\"direction\":\"$direction\",\"utilization\":$UTILIZATION,\"wasteRate\":$WASTE_RATE,\"time\":\"$CALCULATE_TIME\",\"success\":true}"
            if [ $TOTAL_TESTS -eq 0 ]; then
                echo "  $RESULT" >> $RESULTS_FILE
            else
                echo "  ,$RESULT" >> $RESULTS_FILE
            fi
            
            TOTAL_TESTS=$((TOTAL_TESTS + 1))
            SUCCESS_TESTS=$((SUCCESS_TESTS + 1))
            
            # 检查是否为最佳结果
            if (( $(echo "$UTILIZATION > $BEST_UTILIZATION" | bc -l) )); then
                BEST_UTILIZATION=$UTILIZATION
                BEST_STRATEGY=$strategy
                BEST_DIRECTION=$direction
            fi
        else
            ERROR_MSG=$(echo "$RESPONSE" | jq -r '.message')
            echo "   ❌ 错误: $ERROR_MSG"
            
            RESULT="{\"strategy\":\"$strategy\",\"direction\":\"$direction\",\"utilization\":0,\"wasteRate\":100,\"time\":\"0ms\",\"success\":false,\"error\":\"$ERROR_MSG\"}"
            if [ $TOTAL_TESTS -eq 0 ]; then
                echo "  $RESULT" >> $RESULTS_FILE
            else
                echo "  ,$RESULT" >> $RESULTS_FILE
            fi
            
            TOTAL_TESTS=$((TOTAL_TESTS + 1))
        fi
        
        # 短暂延迟避免过载
        sleep 0.1
    done
done

echo "]" >> $RESULTS_FILE

# 输出总结
echo "========================================"
echo "📈 测试结果汇总:"
echo "   总测试次数: $TOTAL_TESTS"
echo "   成功次数: $SUCCESS_TESTS"
echo "   最佳利用率: ${BEST_UTILIZATION}%"
echo "   最佳策略: $BEST_STRATEGY"
echo "   最佳方向: $BEST_DIRECTION"

if [ $TOTAL_TESTS -gt 0 ]; then
    SUCCESS_RATE=$(echo "scale=2; $SUCCESS_TESTS * 100 / $TOTAL_TESTS" | bc)
    echo "   成功率: ${SUCCESS_RATE}%"
fi

echo "📋 详细结果已保存到 $RESULTS_FILE"