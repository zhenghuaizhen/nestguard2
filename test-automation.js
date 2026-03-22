#!/usr/bin/env node

// NestGuard 自动化测试脚本
// 生成100+笔测试数据，验证套料算法最优性

const axios = require('axios');
const fs = require('fs').promises;

// 配置
const API_BASE = 'http://localhost:5000/api';
const TEST_COUNT = 100; // 计划执行100笔测试
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'admin123';

// 全局变量
let authToken = '';
let testResults = [];

// 常用材料规格
const MATERIALS = [
    { name: '1050纯铝板', thickness: 1 },
    { name: '1050纯铝板', thickness: 2 },
    { name: '1060纯铝板', thickness: 1 },
    { name: '3003防锈铝板', thickness: 1.5 }
];

// 库存板材规格（标准尺寸）
const STOCK_SIZES = [
    { width: 2000, length: 4000 },
    { width: 1500, length: 3000 },
    { width: 1200, length: 2400 },
    { width: 1000, length: 2000 }
];

// 订单零件规格范围
const PART_MIN_SIZE = 50;
const PART_MAX_SIZE = 800;
const PART_MIN_QTY = 1;
const PART_MAX_QTY = 20;

class NestGuardTester {
    constructor() {
        this.client = axios.create({
            baseURL: API_BASE,
            timeout: 30000
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

    async clearTestData() {
        console.log('🧹 清理测试数据...');
        
        // 获取所有订单
        try {
            const ordersResponse = await this.client.get('/order?page=1&pageSize=1000');
            if (ordersResponse.data.success) {
                const orderIds = ordersResponse.data.data.items.map(item => item.id);
                if (orderIds.length > 0) {
                    await this.client.post('/order/batch-delete', orderIds);
                    console.log(`🗑️ 删除了 ${orderIds.length} 个订单`);
                }
            }
        } catch (error) {
            console.warn('⚠️ 清理订单时出错:', error.message);
        }

        // 获取所有库存
        try {
            const inventoryResponse = await this.client.get('/inventory?page=1&pageSize=1000');
            if (inventoryResponse.data.success) {
                const inventoryIds = inventoryResponse.data.data.items.map(item => item.id);
                if (inventoryIds.length > 0) {
                    for (const id of inventoryIds) {
                        try {
                            await this.client.delete(`/inventory/${id}`);
                        } catch (error) {
                            // 忽略删除错误（可能已被使用）
                        }
                    }
                    console.log(`🗑️ 删除了 ${inventoryIds.length} 个库存记录`);
                }
            }
        } catch (error) {
            console.warn('⚠️ 清理库存时出错:', error.message);
        }
    }

    generateRandomPart(material) {
        const width = Math.floor(Math.random() * (PART_MAX_SIZE - PART_MIN_SIZE + 1)) + PART_MIN_SIZE;
        const length = Math.floor(Math.random() * (PART_MAX_SIZE - PART_MIN_SIZE + 1)) + PART_MIN_SIZE;
        const quantity = Math.floor(Math.random() * (PART_MAX_QTY - PART_MIN_QTY + 1)) + PART_MIN_QTY;
        
        return {
            customer: `客户${Math.floor(Math.random() * 1000)}`,
            materialName: material.name,
            thickness: material.thickness,
            width: width,
            length: length,
            quantity: quantity
        };
    }

    generateStockInventory(material, count = 3) {
        const inventories = [];
        for (let i = 0; i < count; i++) {
            const stockSize = STOCK_SIZES[Math.floor(Math.random() * STOCK_SIZES.length)];
            inventories.push({
                barcode: `STOCK-${material.name.replace(/\s+/g, '')}-${material.thickness}mm-${i + 1}`,
                materialName: material.name,
                thickness: material.thickness,
                width: stockSize.width,
                length: stockSize.length,
                quantity: 1,
                materialType: 0,
                status: 0
            });
        }
        return inventories;
    }

    async createOrder(orderData) {
        try {
            const response = await this.client.post('/order', orderData);
            if (response.data.success) {
                // 后端返回 true 表示成功，需要通过查询获取 ID
                const listResponse = await this.client.get(`/order?page=1&pageSize=100&materialName=${encodeURIComponent(orderData.materialName)}`);
                const item = listResponse.data.data.items.find(i => i.orderId === orderData.orderId);
                return item ? item.id : listResponse.data.data.items[0].id;
            } else {
                console.error('❌ 创建订单失败:', response.data.message);
                return null;
            }
        } catch (error) {
            console.error('❌ 创建订单异常:', error.message);
            return null;
        }
    }

    async createInventory(inventoryData) {
        try {
            const response = await this.client.post('/inventory', inventoryData);
            if (response.data.success) {
                const listResponse = await this.client.get(`/inventory?page=1&pageSize=100&materialName=${encodeURIComponent(inventoryData.materialName)}`);
                const item = listResponse.data.data.items.find(i => i.barcode === inventoryData.barcode);
                return item ? item.id : listResponse.data.data.items[0].id;
            } else {
                console.error('❌ 创建库存失败:', response.data.message);
                return null;
            }
        } catch (error) {
            console.error('❌ 创建库存异常:', error.message);
            return null;
        }
    }

    async calculateNesting(orderIds) {
        const response = await this.client.post('/nesting/calculate', {
            orderIds: orderIds,
            calcMode: 'fast',
            selectStrategy: 'minMatch',
            cutDirection: 'horizontal',
            fixedDirection: false
        });

        if (response.data.success) {
            return response.data.data;
        } else {
            console.error('❌ 套料计算失败:', response.data.message);
            return null;
        }
    }

    evaluateOptimality(result) {
        if (!result || !result.items || result.items.length === 0) {
            return { isOptimal: false, reason: '无计算结果' };
        }

        const totalUtilization = result.totalUtilization;
        const wasteRate = result.wasteRate;
        const platesUsed = result.totalPlates;
        const completedOrders = result.completedOrders;

        // 评估标准：
        // 1. 利用率 > 70% 为良好
        // 2. 损耗率 < 30% 为良好  
        // 3. 完成订单比例高
        // 4. 使用板材数量合理

        let score = 0;
        let reasons = [];

        if (totalUtilization >= 80) {
            score += 30;
            reasons.push('利用率优秀(≥80%)');
        } else if (totalUtilization >= 70) {
            score += 20;
            reasons.push('利用率良好(≥70%)');
        } else if (totalUtilization >= 50) {
            score += 10;
            reasons.push('利用率一般(≥50%)');
        } else {
            reasons.push('利用率较低(<50%)');
        }

        if (wasteRate <= 20) {
            score += 30;
            reasons.push('损耗率优秀(≤20%)');
        } else if (wasteRate <= 30) {
            score += 20;
            reasons.push('损耗率良好(≤30%)');
        } else if (wasteRate <= 50) {
            score += 10;
            reasons.push('损耗率一般(≤50%)');
        } else {
            reasons.push('损耗率较高(>50%)');
        }

        // 板材使用效率
        if (platesUsed <= Math.ceil(completedOrders / 2)) {
            score += 20;
            reasons.push('板材使用高效');
        } else if (platesUsed <= completedOrders) {
            score += 10;
            reasons.push('板材使用合理');
        } else {
            reasons.push('板材使用较多');
        }

        const isOptimal = score >= 60; // 总分60分以上认为是最优
        return {
            isOptimal,
            score,
            totalUtilization,
            wasteRate,
            platesUsed,
            completedOrders,
            reasons: reasons.join(', ')
        };
    }

    async logDetailedError(error, payload) {
        if (error.response) {
            console.error(`   ❌ 状态码: ${error.response.status}`);
            console.error(`   ❌ 响应数据: ${JSON.stringify(error.response.data)}`);
            console.error(`   ❌ 请求负载: ${JSON.stringify(payload)}`);
        } else {
            console.error(`   ❌ 错误消息: ${error.message}`);
        }
    }

    async runSingleTest(testIndex, material) {
        
        // 创建库存
        const inventories = this.generateStockInventory(material, 2);
        const createdInventories = [];
        
        for (const inv of inventories) {
            const id = await this.createInventory(inv);
            if (id) {
                createdInventories.push(id);
            }
        }
        
        if (createdInventories.length === 0) {
            console.error('❌ 无法创建库存，跳过测试');
            return null;
        }

        // 创建订单（同品种同厚度）
        const orderIds = [];
        const orderCount = Math.floor(Math.random() * 5) + 2; // 2-6个订单
        
        for (let i = 0; i < orderCount; i++) {
            const part = this.generateRandomPart(material);
            const id = await this.createOrder(part);
            if (id) {
                orderIds.push(id);
            }
        }
        
        if (orderIds.length === 0) {
            console.error('❌ 无法创建订单，跳过测试');
            return null;
        }

        // 执行套料计算
        let result;
        const payload = {
            orderIds: orderIds,
            calcMode: 'fast',
            selectStrategy: 'minMatch',
            cutDirection: 'horizontal',
            fixedDirection: false
        };
        try {
            result = await this.calculateNesting(orderIds);
        } catch (error) {
            console.error('❌ 套料计算异常:');
            await this.logDetailedError(error, payload);
            return null;
        }
        
        if (!result) {
            console.error('❌ 套料计算失败，跳过测试');
            return null;
        }

        // 评估最优性
        const evaluation = this.evaluateOptimality(result);
        
        const testResult = {
            testIndex: testIndex + 1,
            material: `${material.name} ${material.thickness}mm`,
            orderCount: orderIds.length,
            plateCount: result.totalPlates,
            utilization: result.totalUtilization,
            wasteRate: result.wasteRate,
            completedOrders: result.completedOrders,
            isOptimal: evaluation.isOptimal,
            score: evaluation.score,
            reasons: evaluation.reasons
        };

        console.log(`📊 结果: 利用率=${result.totalUtilization.toFixed(2)}%, 损耗=${result.wasteRate.toFixed(2)}%, 板材=${result.totalPlates}, 完成订单=${result.completedOrders}`);
        console.log(`🎯 最优性: ${evaluation.isOptimal ? '✅ 是' : '❌ 否'} (评分: ${evaluation.score}/80)`);
        
        return testResult;
    }

    async runAllTests() {
        console.log('🚀 开始 NestGuard 套料算法自动化测试');
        console.log(`📋 计划执行 ${TEST_COUNT} 笔测试`);
        
        // 登录
        if (!(await this.login())) {
            console.error('❌ 登录失败，无法继续测试');
            return;
        }

        // 清理测试数据
        await this.clearTestData();

        // 执行测试
        for (let i = 0; i < TEST_COUNT; i++) {
            const material = MATERIALS[Math.floor(Math.random() * MATERIALS.length)];

            // 每次测试前清理，确保环境干净
            await this.clearTestData();

            console.log(`\n🧪 测试 ${i + 1}/${TEST_COUNT}: ${material.name} ${material.thickness}mm`);
            const result = await this.runSingleTest(i, material);
            
            if (result) {
                testResults.push(result);
            }
            
            // 避免请求过于频繁
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        // 生成测试报告
        this.generateReport();
    }

    generateReport() {
        console.log('\n' + '='.repeat(60));
        console.log('📊 NESTGUARD 套料算法自动化测试报告');
        console.log('='.repeat(60));
        
        const totalTests = testResults.length;
        const optimalTests = testResults.filter(r => r.isOptimal).length;
        const avgUtilization = testResults.reduce((sum, r) => sum + r.utilization, 0) / totalTests;
        const avgWasteRate = testResults.reduce((sum, r) => sum + r.wasteRate, 0) / totalTests;
        const avgScore = testResults.reduce((sum, r) => sum + r.score, 0) / totalTests;
        
        console.log(`\n📈 测试概览:`);
        console.log(`   • 总测试数: ${totalTests}`);
        console.log(`   • 最优结果: ${optimalTests} (${((optimalTests/totalTests)*100).toFixed(1)}%)`);
        console.log(`   • 平均利用率: ${avgUtilization.toFixed(2)}%`);
        console.log(`   • 平均损耗率: ${avgWasteRate.toFixed(2)}%`);
        console.log(`   • 平均评分: ${avgScore.toFixed(1)}/80`);
        
        // 按材料分类统计
        const materialStats = {};
        testResults.forEach(r => {
            if (!materialStats[r.material]) {
                materialStats[r.material] = { count: 0, optimal: 0, totalUtil: 0, totalWaste: 0 };
            }
            materialStats[r.material].count++;
            if (r.isOptimal) materialStats[r.material].optimal++;
            materialStats[r.material].totalUtil += r.utilization;
            materialStats[r.material].totalWaste += r.wasteRate;
        });
        
        console.log(`\n🔍 按材料分析:`);
        Object.entries(materialStats).forEach(([material, stats]) => {
            const optRate = (stats.optimal / stats.count * 100).toFixed(1);
            const avgUtil = (stats.totalUtil / stats.count).toFixed(2);
            const avgWaste = (stats.totalWaste / stats.count).toFixed(2);
            console.log(`   • ${material}: 最优率 ${optRate}%, 利用率 ${avgUtil}%, 损耗 ${avgWaste}%`);
        });
        
        // 最优性结论
        const optimalRate = (optimalTests / totalTests) * 100;
        console.log(`\n🎯 算法最优性结论:`);
        if (optimalRate >= 80) {
            console.log('   ✅ 算法表现优秀！在大多数情况下都能实现最优套料');
            console.log('   ✅ 损耗最小化和加工效率兼顾良好');
        } else if (optimalRate >= 60) {
            console.log('   ⚠️ 算法表现良好，但仍有优化空间');
            console.log('   ⚠️ 建议针对特定材料规格进行微调');
        } else {
            console.log('   ❌ 算法需要进一步优化');
            console.log('   ❌ 损耗控制和效率平衡需要改进');
        }
        
        // 保存详细结果
        const reportData = {
            summary: {
                totalTests,
                optimalTests,
                optimalRate: optimalRate.toFixed(2),
                avgUtilization: avgUtilization.toFixed(2),
                avgWasteRate: avgWasteRate.toFixed(2),
                avgScore: avgScore.toFixed(1)
            },
            detailedResults: testResults
        };
        
        fs.writeFile('test-results.json', JSON.stringify(reportData, null, 2))
            .then(() => console.log('\n💾 详细测试结果已保存到 test-results.json'))
            .catch(err => console.error('❌ 保存结果失败:', err.message));
    }
}

// 运行测试
async function main() {
    const tester = new NestGuardTester();
    await tester.runAllTests();
}

if (require.main === module) {
    main().catch(console.error);
}