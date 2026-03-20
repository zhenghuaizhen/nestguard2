#!/usr/bin/env node

// NestGuard 优化算法对比测试
const axios = require('axios');
const fs = require('fs').promises;

const API_BASE = 'http://localhost:5000/api';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'admin123';

let authToken = '';

class OptimizedTester {
    constructor() {
        this.client = axios.create({
            baseURL: API_BASE,
            timeout: 60000
        });
    }

    async login() {
        try {
            const response = await this.client.post('/auth/login', {
                username: ADMIN_USER,
                password: ADMIN_PASS
            });
            
            if (response.data.success) {
                authToken = response.data.data.token;
                this.client.defaults.headers.common['Authorization'] = `Bearer ${authToken}`;
                console.log('✅ 登录成功');
                return true;
            } else {
                console.error('❌ 登录失败:', response.data.message);
                return false;
            }
        } catch (error) {
            console.error('❌ 登录异常:', error.message);
            return false;
        }
    }

    async getAvailableOrders() {
        try {
            const response = await this.client.get('/nesting/available-orders');
            if (response.data.success) {
                return response.data.data;
            }
        } catch (error) {
            console.error('❌ 获取订单失败:', error.message);
        }
        return [];
    }

    async calculateNesting(orderIds, strategy = 'minMatch') {
        try {
            const response = await this.client.post('/nesting/calculate', {
                orderIds: orderIds,
                selectStrategy: strategy,
                cutDirection: 'auto',
                fixedDirection: false
            });
            
            if (response.data.success) {
                return response.data.data;
            } else {
                console.error('❌ 套料计算失败:', response.data.message);
                return null;
            }
        } catch (error) {
            console.error('❌ 套料计算异常:', error.message);
            return null;
        }
    }

    async runOptimizationTest() {
        console.log('🚀 开始优化算法测试...');
        
        if (!(await this.login())) {
            console.error('❌ 认证失败');
            return;
        }

        // 获取所有可用订单
        const orders = await this.getAvailableOrders();
        if (orders.length === 0) {
            console.log('⚠️ 没有找到可套料的订单');
            return;
        }

        console.log(`📋 找到 ${orders.length} 个可套料订单`);

        // 按品种和厚度分组
        const groups = {};
        orders.forEach(order => {
            const key = `${order.materialName}|${order.thickness}`;
            if (!groups[key]) {
                groups[key] = [];
            }
            groups[key].push(order);
        });

        console.log(`📦 发现 ${Object.keys(groups).length} 个材料组合`);

        const testResults = [];
        let testIndex = 1;

        // 对每个组进行测试
        for (const [groupKey, groupOrders] of Object.entries(groups)) {
            if (testIndex > 10) break; // 限制测试数量
            
            const orderIds = groupOrders.map(o => o.id);
            console.log(`\n🧪 测试 ${testIndex}: ${groupKey} (${orderIds.length} 个订单)`);
            
            // 测试优化算法
            const result = await this.calculateNesting(orderIds, 'minMatch');
            
            if (result) {
                const testResult = {
                    testIndex,
                    material: groupKey,
                    orderCount: orderIds.length,
                    plateCount: result.totalPlates,
                    utilization: result.totalUtilization,
                    wasteRate: result.wasteRate,
                    completedOrders: result.completedOrders,
                    productWeight: result.productWeight,
                    remnantWeight: result.remnantWeight,
                    wasteWeight: result.wasteWeight,
                    calculateTime: result.calculateTime
                };
                
                testResults.push(testResult);
                
                console.log(`📊 结果: 利用率=${result.totalUtilization.toFixed(2)}%, 损耗=${result.wasteRate.toFixed(2)}%`);
                console.log(`   📦 使用板材: ${result.totalPlates}, 完成订单: ${result.completedOrders}`);
                
                testIndex++;
            }
            
            // 避免请求过于频繁
            await new Promise(resolve => setTimeout(resolve, 500));
        }

        // 保存测试结果
        const report = {
            timestamp: new Date().toISOString(),
            totalTests: testResults.length,
            results: testResults,
            summary: {
                avgUtilization: testResults.reduce((sum, r) => sum + r.utilization, 0) / testResults.length,
                avgWasteRate: testResults.reduce((sum, r) => sum + r.wasteRate, 0) / testResults.length,
                totalPlatesUsed: testResults.reduce((sum, r) => sum + r.plateCount, 0),
                totalCompletedOrders: testResults.reduce((sum, r) => sum + r.completedOrders, 0)
            }
        };

        await fs.writeFile('optimization-test-results.json', JSON.stringify(report, null, 2));
        console.log('\n💾 测试结果已保存到 optimization-test-results.json');

        // 显示总结
        console.log('\n' + '='.repeat(50));
        console.log('📈 优化测试总结:');
        console.log(`   总测试数: ${report.totalTests}`);
        console.log(`   平均利用率: ${report.summary.avgUtilization.toFixed(2)}%`);
        console.log(`   平均损耗率: ${report.summary.avgWasteRate.toFixed(2)}%`);
        console.log(`   总板材使用: ${report.summary.totalPlatesUsed}`);
        console.log(`   总完成订单: ${report.summary.totalCompletedOrders}`);
        console.log('='.repeat(50));
    }
}

async function main() {
    const tester = new OptimizedTester();
    await tester.runOptimizationTest();
}

if (require.main === module) {
    main().catch(console.error);
}