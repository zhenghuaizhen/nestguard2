#!/usr/bin/env node

// NestGuard 优化对比测试脚本
// 比较原版算法和优化算法的性能差异

const axios = require('axios');
const fs = require('fs').promises;

// 配置
const API_BASE = 'http://localhost:5000/api';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'admin123';

// 全局变量
let authToken = '';

class OptimizationTester {
    constructor() {
        this.client = axios.create({
            baseURL: API_BASE,
            timeout: 60000 // 增加超时时间
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

    // 创建测试数据
    async createTestData() {
        console.log('📊 创建测试数据...');
        
        // 创建库存
        const stockInventories = [
            { barcode: "STOCK-1", materialName: "1050纯铝板", thickness: 1, width: 2000, length: 4000, quantity: 3 },
            { barcode: "STOCK-2", materialName: "1050纯铝板", thickness: 1, width: 1500, length: 3000, quantity: 2 },
            { barcode: "STOCK-3", materialName: "1050纯铝板", thickness: 1, width: 1200, length: 2400, quantity: 2 },
            { barcode: "REMNANT-1", materialName: "1050纯铝板", thickness: 1, width: 800, length: 1200, quantity: 1, materialType: 1 }
        ];
        
        for (const inv of stockInventories) {
            await this.client.post('/inventory', inv);
        }
        
        // 创建订单（混合大小零件）
        const orders = [
            // 大零件
            { customer: "客户A", materialName: "1050纯铝板", thickness: 1, width: 900, length: 1800, quantity: 2 },
            { customer: "客户B", materialName: "1050纯铝板", thickness: 1, width: 800, length: 1600, quantity: 3 },
            // 中等零件
            { customer: "客户C", materialName: "1050纯铝板", thickness: 1, width: 600, length: 1200, quantity: 4 },
            { customer: "客户D", materialName: "1050纯铝板", thickness: 1, width: 500, length: 1000, quantity: 5 },
            // 小零件
            { customer: "客户E", materialName: "1050纯铝板", thickness: 1, width: 300, length: 600, quantity: 8 },
            { customer: "客户F", materialName: "1050纯铝板", thickness: 1, width: 250, length: 500, quantity: 10 },
            { customer: "客户G", materialName: "1050纯铝板", thickness: 1, width: 200, length: 400, quantity: 12 }
        ];
        
        for (const order of orders) {
            await this.client.post('/order', order);
        }
        
        console.log('✅ 测试数据创建完成');
    }

    async testOriginalAlgorithm() {
        console.log('\n🧪 测试原版算法...');
        
        // 获取订单ID
        const ordersResponse = await this.client.get('/order?page=1&pageSize=100');
        const orderIds = ordersResponse.data.data.items.map(item => item.id);
        
        const startTime = Date.now();
        const response = await this.client.post('/nesting/calculate', {
            orderIds: orderIds,
            calcMode: 'fast',
            cutDirection: 'auto',
            fixedDirection: false,
            selectStrategy: 'minMatch'
        });
        const endTime = Date.now();
        
        if (response.data.success) {
            const result = response.data.data;
            const calculationTime = endTime - startTime;
            
            console.log(`✅ 原版算法结果:`);
            console.log(`   • 利用率: ${result.totalUtilization.toFixed(2)}%`);
            console.log(`   • 损耗率: ${result.wasteRate.toFixed(2)}%`);
            console.log(`   • 使用板材: ${result.totalPlates}`);
            console.log(`   • 计算时间: ${calculationTime}ms`);
            
            return {
                algorithm: 'original',
                utilization: result.totalUtilization,
                wasteRate: result.wasteRate,
                platesUsed: result.totalPlates,
                calculationTime: calculationTime,
                completedOrders: result.completedOrders
            };
        } else {
            console.error('❌ 原版算法测试失败:', response.data.message);
            return null;
        }
    }

    async testOptimizedAlgorithm() {
        console.log('\n🚀 测试优化算法...');
        
        // 获取订单ID
        const ordersResponse = await this.client.get('/order?page=1&pageSize=100');
        const orderIds = ordersResponse.data.data.items.map(item => item.id);
        
        const startTime = Date.now();
        const response = await this.client.post('/nesting/calculate', {
            orderIds: orderIds,
            calcMode: 'precise',
            cutDirection: 'auto',
            fixedDirection: false,
            selectStrategy: 'smartMatch'
        });
        const endTime = Date.now();
        
        if (response.data.success) {
            const result = response.data.data;
            const calculationTime = endTime - startTime;
            
            console.log(`✅ 优化算法结果:`);
            console.log(`   • 利用率: ${result.totalUtilization.toFixed(2)}%`);
            console.log(`   • 损耗率: ${result.wasteRate.toFixed(2)}%`);
            console.log(`   • 使用板材: ${result.totalPlates}`);
            console.log(`   • 计算时间: ${calculationTime}ms`);
            
            return {
                algorithm: 'optimized',
                utilization: result.totalUtilization,
                wasteRate: result.wasteRate,
                platesUsed: result.totalPlates,
                calculationTime: calculationTime,
                completedOrders: result.completedOrders
            };
        } else {
            console.error('❌ 优化算法测试失败:', response.data.message);
            return null;
        }
    }

    generateComparisonReport(originalResult, optimizedResult) {
        console.log('\n' + '='.repeat(60));
        console.log('📊 NESTGUARD 算法优化对比报告');
        console.log('='.repeat(60));
        
        if (!originalResult || !optimizedResult) {
            console.log('❌ 测试数据不完整，无法生成对比报告');
            return;
        }
        
        const utilizationImprovement = optimizedResult.utilization - originalResult.utilization;
        const wasteReduction = originalResult.wasteRate - optimizedResult.wasteRate;
        const platesSaved = originalResult.platesUsed - optimizedResult.platesUsed;
        const timeDifference = optimizedResult.calculationTime - originalResult.calculationTime;
        
        console.log('\n📈 性能对比:');
        console.log(`   • 利用率提升: ${utilizationImprovement > 0 ? '+' : ''}${utilizationImprovement.toFixed(2)}% (${originalResult.utilization.toFixed(2)}% → ${optimizedResult.utilization.toFixed(2)}%)`);
        console.log(`   • 损耗率降低: ${wasteReduction > 0 ? '-' : ''}${wasteReduction.toFixed(2)}% (${originalResult.wasteRate.toFixed(2)}% → ${optimizedResult.wasteRate.toFixed(2)}%)`);
        console.log(`   • 节省板材: ${platesSaved} 张 (${originalResult.platesUsed} → ${optimizedResult.platesUsed})`);
        console.log(`   • 计算时间差异: ${timeDifference > 0 ? '+' : ''}${timeDifference}ms`);
        
        console.log('\n🎯 优化效果评估:');
        if (utilizationImprovement >= 5) {
            console.log('   ✅ 利用率显著提升！优化效果优秀');
        } else if (utilizationImprovement >= 2) {
            console.log('   ⚠️ 利用率有所提升，优化效果良好');
        } else {
            console.log('   ❌ 利用率提升不明显，需要进一步优化');
        }
        
        if (platesSaved > 0) {
            console.log(`   💰 节省 ${platesSaved} 张板材，直接降低成本`);
        }
        
        // 保存详细报告
        const reportData = {
            original: originalResult,
            optimized: optimizedResult,
            improvements: {
                utilization: utilizationImprovement,
                wasteReduction: wasteReduction,
                platesSaved: platesSaved,
                timeDifference: timeDifference
            }
        };
        
        fs.writeFile('optimization-comparison-report.json', JSON.stringify(reportData, null, 2))
            .then(() => console.log('\n💾 详细对比报告已保存到 optimization-comparison-report.json'))
            .catch(err => console.error('❌ 保存报告失败:', err.message));
    }

    async runComparisonTest() {
        console.log('🚀 开始 NestGuard 算法优化对比测试');
        
        // 登录
        if (!(await this.login())) {
            console.error('❌ 登录失败，无法继续测试');
            return;
        }

        // 清理并创建测试数据
        await this.clearTestData();
        await this.createTestData();

        // 测试原版算法
        const originalResult = await this.testOriginalAlgorithm();
        
        // 重新创建测试数据（确保相同条件）
        await this.clearTestData();
        await this.createTestData();
        
        // 测试优化算法
        const optimizedResult = await this.testOptimizedAlgorithm();
        
        // 生成对比报告
        this.generateComparisonReport(originalResult, optimizedResult);
    }
}

// 运行测试
async function main() {
    const tester = new OptimizationTester();
    await tester.runComparisonTest();
}

if (require.main === module) {
    main().catch(console.error);
}