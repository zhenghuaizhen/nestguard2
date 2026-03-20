const axios = require('axios');
const fs = require('fs');

// 从环境变量或文件获取 token
let token;
try {
    token = fs.readFileSync('/tmp/token.txt', 'utf8').trim();
} catch (e) {
    console.error('❌ 未找到认证 token，请先登录');
    process.exit(1);
}

const API_BASE = 'http://localhost:5000/api';

// 获取所有订单
async function getOrders() {
    try {
        const response = await axios.get(`${API_BASE}/order?page=1&pageSize=1000`, {
            headers: { Authorization: `Bearer ${token}` }
        });
        return response.data.data.items;
    } catch (error) {
        console.error('❌ 获取订单失败:', error.response?.data || error.message);
        return [];
    }
}

// 获取所有库存
async function getInventory() {
    try {
        const response = await axios.get(`${API_BASE}/inventory?page=1&pageSize=1000`, {
            headers: { Authorization: `Bearer ${token}` }
        });
        return response.data.data.items;
    } catch (error) {
        console.error('❌ 获取库存失败:', error.response?.data || error.message);
        return [];
    }
}

// 执行套料计算
async function calculateNesting(orderIds) {
    try {
        const response = await axios.post(`${API_BASE}/nesting/calculate`, {
            orderIds: orderIds,
            calcMode: 'auto',
            cutDirection: 'horizontal',
            fixedDirection: false
        }, {
            headers: { Authorization: `Bearer ${token}` }
        });
        
        if (response.data.success) {
            const result = response.data.data;
            console.log(`✅ 套料计算成功 - 使用板材: ${result.totalPlates}, 利用率: ${result.totalUtilization}%, 损耗率: ${result.wasteRate}%`);
            return result;
        } else {
            console.log(`⚠️  套料计算失败: ${response.data.message}`);
            return null;
        }
    } catch (error) {
        console.error('❌ 套料计算异常:', error.response?.data || error.message);
        return null;
    }
}

// 分析测试结果
function analyzeResults(results) {
    if (results.length === 0) {
        console.log('📊 无有效测试结果');
        return;
    }
    
    const totalTests = results.length;
    const successfulTests = results.filter(r => r !== null).length;
    const avgUtilization = results
        .filter(r => r !== null)
        .reduce((sum, r) => sum + r.totalUtilization, 0) / successfulTests;
    const avgWasteRate = results
        .filter(r => r !== null)
        .reduce((sum, r) => sum + r.wasteRate, 0) / successfulTests;
    const maxUtilization = Math.max(...results.filter(r => r !== null).map(r => r.totalUtilization));
    const minWasteRate = Math.min(...results.filter(r => r !== null).map(r => r.wasteRate));
    
    console.log('\n📈 测试结果分析:');
    console.log(`   总测试次数: ${totalTests}`);
    console.log(`   成功次数: ${successfulTests}`);
    console.log(`   平均利用率: ${avgUtilization.toFixed(2)}%`);
    console.log(`   平均损耗率: ${avgWasteRate.toFixed(2)}%`);
    console.log(`   最高利用率: ${maxUtilization.toFixed(2)}%`);
    console.log(`   最低损耗率: ${minWasteRate.toFixed(2)}%`);
    
    // 保存详细结果
    const testResults = {
        summary: {
            totalTests,
            successfulTests,
            avgUtilization: parseFloat(avgUtilization.toFixed(2)),
            avgWasteRate: parseFloat(avgWasteRate.toFixed(2)),
            maxUtilization: parseFloat(maxUtilization.toFixed(2)),
            minWasteRate: parseFloat(minWasteRate.toFixed(2))
        },
        detailedResults: results.filter(r => r !== null)
    };
    
    fs.writeFileSync('nesting-test-results.json', JSON.stringify(testResults, null, 2));
    console.log('💾 测试结果已保存到 nesting-test-results.json');
}

async function main() {
    console.log('⚙️  开始套料算法测试...');
    
    // 获取订单和库存
    const orders = await getOrders();
    const inventory = await getInventory();
    
    if (orders.length === 0 || inventory.length === 0) {
        console.log('⚠️  没有找到订单或库存数据，跳过测试');
        return;
    }
    
    console.log(`📋 找到 ${orders.length} 个订单和 ${inventory.length} 个库存记录`);
    
    // 按品种和厚度分组订单
    const orderGroups = {};
    orders.forEach(order => {
        const key = `${order.materialName}|${order.thickness}`;
        if (!orderGroups[key]) {
            orderGroups[key] = [];
        }
        orderGroups[key].push(order);
    });
    
    console.log(`📦 发现 ${Object.keys(orderGroups).length} 个不同的材料规格组合`);
    
    // 对每个组执行套料计算
    const results = [];
    let testCount = 0;
    
    for (const [groupKey, groupOrders] of Object.entries(orderGroups)) {
        if (testCount >= 100) break; // 限制测试次数
        
        const orderIds = groupOrders.map(o => o.id);
        console.log(`\n🧪 测试组 ${testCount + 1}: ${groupKey} (${orderIds.length} 个订单)`);
        
        const result = await calculateNesting(orderIds);
        if (result) {
            results.push(result);
            testCount++;
            
            // 如果达到100次测试，停止
            if (testCount >= 100) {
                console.log('🎯 已完成100次测试，停止执行');
                break;
            }
        }
        
        // 避免请求过于频繁
        await new Promise(resolve => setTimeout(resolve, 100));
    }
    
    // 分析结果
    analyzeResults(results);
}

main().catch(console.error);