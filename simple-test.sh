#!/bin/bash

echo "🚀 开始 NestGuard 套料算法大规模测试"
echo "========================================"

# 获取 token
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.data.token')

if [ "$TOKEN" == "null" ]; then
    echo "❌ 认证失败"
    exit 1
fi

echo "✅ 认证成功"

# 清理现有数据
echo "🧹 清理测试数据..."
curl -s -X DELETE http://localhost:5000/api/order/batch-delete -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "[1,2,3,4,5,6,7,8,9,10]"
curl -s -X DELETE http://localhost:5000/api/inventory/batch-delete -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "[1,2,3,4,5,6,7,8,9,10]"

# 定义测试参数
MATERIALS=("1050纯铝板" "1060纯铝板" "3003防锈铝板" "5052铝镁合金板" "6061铝镁硅合金板")
THICKNESSES=(1 2 3 4 5)
INVENTORY_COUNT=20
ORDER_COUNT=100

echo "📊 生成库存数据 ($INVENTORY_COUNT 条)..."
for i in $(seq 1 $INVENTORY_COUNT); do
    MATERIAL=${MATERIALS[$((RANDOM % ${#MATERIALS[@]}))]}
    THICKNESS=${THICKNESSES[$((RANDOM % ${#THICKNESSES[@]}))]}
    WIDTH=$((2000 + RANDOM % 3000))  # 2000-5000mm
    LENGTH=$((3000 + RANDOM % 4000)) # 3000-7000mm
    QUANTITY=$((1 + RANDOM % 3))     # 1-3张
    
    curl -s -X POST http://localhost:5000/api/inventory \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d "{\"barcode\":\"INV-$i\",\"materialName\":\"$MATERIAL\",\"thickness\":$THICKNESS,\"width\":$WIDTH,\"length\":$LENGTH,\"quantity\":$QUANTITY}" > /dev/null
    
    if [ $((i % 10)) -eq 0 ]; then
        echo "   已生成 $i 条库存数据"
    fi
done

echo "📋 生成订单数据 ($ORDER_COUNT 笔)..."
for i in $(seq 1 $ORDER_COUNT); do
    # 随机选择已存在的库存来匹配品种和厚度
    INV_INDEX=$((1 + RANDOM % INVENTORY_COUNT))
    INVENTORY=$(curl -s -X GET "http://localhost:5000/api/inventory?page=1&pageSize=$INVENTORY_COUNT" -H "Authorization: Bearer $TOKEN")
    MATERIAL=$(echo "$INVENTORY" | jq -r ".data.items[$((INV_INDEX-1))].materialName")
    THICKNESS=$(echo "$INVENTORY" | jq -r ".data.items[$((INV_INDEX-1))].thickness")
    
    WIDTH=$((50 + RANDOM % 400))   # 50-450mm
    LENGTH=$((80 + RANDOM % 600))  # 80-680mm
    QUANTITY=$((1 + RANDOM % 20))  # 1-20件
    
    curl -s -X POST http://localhost:5000/api/order \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d "{\"orderId\":\"TEST-ORDER-$i\",\"customer\":\"客户$i\",\"materialName\":\"$MATERIAL\",\"thickness\":$THICKNESS,\"width\":$WIDTH,\"length\":$LENGTH,\"quantity\":$QUANTITY}" > /dev/null
    
    if [ $((i % 20)) -eq 0 ]; then
        echo "   已生成 $i 笔订单"
    fi
done

echo "⚙️  执行套料计算测试..."

# 测试不同的策略组合
STRATEGIES=("minMatch" "remnantFirst" "largeFirst")
DIRECTIONS=("horizontal" "vertical" "auto")
TOTAL_TESTS=0
SUCCESS_TESTS=0

for STRATEGY in "${STRATEGIES[@]}"; do
    for DIRECTION in "${DIRECTIONS[@]}"; do
        echo "   测试策略: $STRATEGY, 方向: $DIRECTION"
        
        # 获取可套料的订单
        ORDERS=$(curl -s -X GET http://localhost:5000/api/nesting/available-orders -H "Authorization: Bearer $TOKEN")
        ORDER_IDS=$(echo "$ORDERS" | jq -r '[.data[] | .id] | join(",")')
        
        if [ "$ORDER_IDS" != "" ] && [ "$ORDER_IDS" != "[]" ]; then
            # 执行套料计算
            RESULT=$(curl -s -X POST http://localhost:5000/api/nesting/calculate \
              -H "Authorization: Bearer $TOKEN" \
              -H "Content-Type: application/json" \
              -d "{\"orderIds\":[$ORDER_IDS],\"selectStrategy\":\"$STRATEGY\",\"cutDirection\":\"$DIRECTION\",\"fixedDirection\":false}")
            
            UTILIZATION=$(echo "$RESULT" | jq -r '.data.totalUtilization // "0"')
            WASTE_RATE=$(echo "$RESULT" | jq -r '.data.wasteRate // "100"')
            TOTAL_PLATES=$(echo "$RESULT" | jq -r '.data.totalPlates // "0"')
            
            if [ "$UTILIZATION" != "null" ] && [ "$UTILIZATION" != "0" ]; then
                echo "     ✅ 利用率: ${UTILIZATION}%, 废料率: ${WASTE_RATE}%, 使用板材: ${TOTAL_PLATES}张"
                SUCCESS_TESTS=$((SUCCESS_TESTS + 1))
            else
                echo "     ❌ 计算失败"
            fi
            TOTAL_TESTS=$((TOTAL_TESTS + 1))
        else
            echo "     ⚠️  没有可套料的订单"
        fi
        
        sleep 1
    done
done

echo "========================================"
echo "📈 测试结果汇总:"
echo "   总测试次数: $TOTAL_TESTS"
echo "   成功次数: $SUCCESS_TESTS"
echo "   成功率: $((SUCCESS_TESTS * 100 / TOTAL_TESTS))%"
echo "========================================"

# 保存详细结果
echo "$RESULT" > nesting-detailed-results.json
echo "📋 详细结果已保存到 nesting-detailed-results.json"