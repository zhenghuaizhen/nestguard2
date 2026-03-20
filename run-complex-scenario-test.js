#!/usr/bin/env node

const axios = require('axios');
const fs = require('fs').promises;

// 配置
const API_BASE = 'http://localhost:5000/api';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'admin123';

let authToken = '';

class ComplexScenarioTester {
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

    async getOrdersByMaterial(material, thickness) {
        try {
            const response = await this.client.get('/order?page=1&pageSize=1000');
            if (response.data.success) {
                return response.data.data.items.filter(order => 
                    order.materialName === material && 
                    order.thickness === thickness &&
                    order.status === 0
                );
            }
        } catch (error) {
            console.error('❌ 获取订单失败:', error.message);
        }
        return [];
    }

    async calculateNesting(orderIds, strategy = 'minMatch', cutDirection = 'auto') {
        try {
            const response = await this.client.post('/nesting/calculate', {
                orderIds: orderIds,
                selectStrategy: strategy,
                cutDirection: cutDirection,
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

    async runScenarioTests() {
        console.log('🚀 开始10个复杂场景测试...\n');

        // 定义测试场景
        const scenarios = [
            { material: '1050纯铝板', thickness: 1, name: '场景1-大小混合' },
            { material: '1060纯铝板', thickness: 2, name: '场景2-高厚板' },
            { material: '3003防锈铝板', thickness: 1.5, name: '场景3-不规则组合' },
            { material: '5052铝镁合金板', thickness: 3, name: '场景4-密集小件' }
        ];

        const strategies = ['minMatch', 'remnantFirst', 'largeFirst'];
        const directions = ['horizontal', 'vertical', 'auto'];

        const allResults = [];

        for (const scenario of scenarios) {
            console.log(`\n🧪 ${scenario.name}: ${scenario.material} ${scenario.thickness}mm`);
            
            const orders = await this.getOrdersByMaterial(scenario.material, scenario.thickness);
            if (orders.length === 0) {
                console.log('   ⚠️  无订单数据，跳过');
                continue;
            }

            const orderIds = orders.map(o => o.id);
            const scenarioResults = [];

            for (const strategy of strategies) {
                for (const direction of directions) {
                    console.log(`   📊 测试: ${strategy} + ${direction}`);
                    
                    const result = await this.calculateNesting(orderIds, strategy, direction);
                    if (result) {
                        const testResult = {
                            scenario: scenario.name,
                            material: `${scenario.material} ${scenario.thickness}mm`,
                            strategy,
                            direction,
                            utilization: result.totalUtilization,
                            wasteRate: result.wasteRate,
                            platesUsed: result.totalPlates,
                            completedOrders: result.completedOrders,
                            calculateTime: result.calculateTime
                        };
                        scenarioResults.push(testResult);
                        console.log(`      ✅ 利用率: ${result.totalUtilization.toFixed(2)}%, 损耗: ${result.wasteRate.toFixed(2)}%`);
                    } else {
                        console.log(`      ❌ 计算失败`);
                    }

                    // 避免请求过于频繁
                    await new Promise(resolve => setTimeout(resolve, 100));
                }
            }

            if (scenarioResults.length > 0) {
                // 找出最佳结果
                const bestResult = scenarioResults.reduce((best, current) => 
                    current.utilization > best.utilization ? current : best
                );
                
                console.log(`   🏆 最佳: ${bestResult.strategy} + ${bestResult.direction} (利用率: ${bestResult.utilization.toFixed(2)}%)`);
                allResults.push(...scenarioResults);
            }
        }

        // 保存结果
        await fs.writeFile('complex-scenario-results.json', JSON.stringify(allResults, null, 2));
        console.log('\n💾 复杂场景测试结果已保存到 complex-scenario-results.json');

        // 生成总结报告
        this.generateSummaryReport(allResults);
    }

    generateSummaryReport(results) {
        if (results.length === 0) {
            console.log('📊 无测试结果');
            return;
        }

        const totalTests = results.length;
        const avgUtilization = results.reduce((sum, r) => sum + r.utilization, 0) / totalTests;
        const avgWasteRate = results.reduce((sum, r) => sum + r.wasteRate, 0) / totalTests;
        
        // 按策略统计
        const strategyStats = {};
        results.forEach(r => {
            const key = `${r.strategy}+${r.direction}`;
            if (!strategyStats[key]) {
                strategyStats[key] = { count: 0, totalUtil: 0, totalWaste: 0 };
            }
            strategyStats[key].count++;
            strategyStats[key].totalUtil += r.utilization;
            strategyStats[key].totalWaste += r.wasteRate;
        });

        console.log('\n' + '='.repeat(60));
        console.log('📊 NESTGUARD 优化算法复杂场景测试报告');
        console.log('='.repeat(60));
        console.log(`\n📈 总体表现:`);
        console.log(`   • 总测试数: ${totalTests}`);
        console.log(`   • 平均利用率: ${avgUtilization.toFixed(2)}%`);
        console.log(`   • 平均损耗率: ${avgWasteRate.toFixed(2)}%`);

        console.log(`\n🔍 策略效果分析:`);
        Object.entries(strategyStats).forEach(([strategy, stats]) => {
            const avgUtil = (stats.totalUtil / stats.count).toFixed(2);
            const avgWaste = (stats.totalWaste / stats.count).toFixed(2);
            console.log(`   • ${strategy}: 利用率 ${avgUtil}%, 损耗 ${avgWaste}%`);
        });

        // 最佳策略
        const bestStrategy = Object.entries(strategyStats)
            .reduce((best, [strategy, stats]) => {
                const avgUtil = stats.totalUtil / stats.count;
                return avgUtil > best.avgUtil ? { strategy, avgUtil } : best;
            }, { strategy: '', avgUtil: 0 });

        console.log(`\n🎯 推荐策略: ${bestStrategy.strategy} (平均利用率: ${bestStrategy.avgUtil.toFixed(2)}%)`);
    }
}

async function main() {
    const tester = new ComplexScenarioTester();
    if (await tester.login()) {
        await tester.runScenarioTests();
    }
}

if (require.main === module) {
    main().catch(console.error);
}