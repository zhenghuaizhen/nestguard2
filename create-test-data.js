#!/usr/bin/env node

const axios = require('axios');
const fs = require('fs');

// 配置
const API_BASE = 'http://localhost:5000/api';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'admin123';

let authToken = '';

// 测试材料规格
const MATERIALS = [
    { name: '1050纯铝板', thickness: 1 },
    { name: '1060纯铝板', thickness: 2 },
    { name: '3003防锈铝板', thickness: 1.5 },
    { name: '5052铝镁合金板', thickness: 3 }
];

// 库存板材规格
const STOCK_SIZES = [
    { width: 2000, length: 4000 },
    { width: 1500, length: 3000 },
    { width: 1200, length: 2400 },
    { width: 1000, length: 2000 }
];

class TestDataCreator {
    constructor() {
        this.client = axios.create({
            baseURL: API_BASE,
            timeout: 10000
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
        console.log('🧹 清理现有测试数据...');
        
        // 删除所有订单
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

        // 删除所有库存
        try {
            const inventoryResponse = await this.client.get('/inventory?page=1&pageSize=1000');
            if (inventoryResponse.data.success) {
                const inventoryIds = inventoryResponse.data.data.items.map(item => item.id);
                if (inventoryIds.length > 0) {
                    for (const id of inventoryIds) {
                        try {
                            await this.client.delete(`/inventory/${id}`);
                        } catch (error) {
                            // 忽略删除错误
                        }
                    }
                    console.log(`🗑️ 删除了 ${inventoryIds.length} 个库存记录`);
                }
            }
        } catch (error) {
            console.warn('⚠️ 清理库存时出错:', error.message);
        }
    }

    // 创建复杂测试场景
    async createComplexTestScenarios() {
        console.log('\n🚀 开始创建10个复杂测试场景...');
        
        const scenarios = [];
        
        // 场景1: 大小零件混合
        scenarios.push({
            name: '大小零件混合',
            material: MATERIALS[0],
            orders: [
                { width: 800, length: 600, quantity: 2 }, // 大件
                { width: 300, length: 200, quantity: 8 }, // 小件
                { width: 500, length: 400, quantity: 3 }  // 中件
            ],
            inventory: [
                { width: 2000, length: 4000, quantity: 2 }
            ]
        });

        // 场景2: 相同尺寸大量需求
        scenarios.push({
            name: '相同尺寸大量需求',
            material: MATERIALS[1],
            orders: [
                { width: 400, length: 300, quantity: 20 }
            ],
            inventory: [
                { width: 1500, length: 3000, quantity: 3 }
            ]
        });

        // 场景3: 不规则尺寸组合
        scenarios.push({
            name: '不规则尺寸组合',
            material: MATERIALS[2],
            orders: [
                { width: 750, length: 650, quantity: 2 },
                { width: 450, length: 350, quantity: 4 },
                { width: 900, length: 800, quantity: 1 },
                { width: 250, length: 150, quantity: 10 }
            ],
            inventory: [
                { width: 2000, length: 4000, quantity: 1 },
                { width: 1200, length: 2400, quantity: 1 }
            ]
        });

        // 场景4: 高厚板测试
        scenarios.push({
            name: '高厚板测试',
            material: MATERIALS[3],
            orders: [
                { width: 600, length: 500, quantity: 3 },
                { width: 400, length: 300, quantity: 5 },
                { width: 800, length: 700, quantity: 2 }
            ],
            inventory: [
                { width: 1500, length: 3000, quantity: 2 }
            ]
        });

        // 场景5: 余料利用测试
        scenarios.push({
            name: '余料利用测试',
            material: MATERIALS[0],
            orders: [
                { width: 950, length: 850, quantity: 1 }, // 刚好适合2000x4000的一半
                { width: 450, length: 350, quantity: 6 }, // 适合余料
                { width: 200, length: 150, quantity: 12 } // 小件填充
            ],
            inventory: [
                { width: 2000, length: 4000, quantity: 1 }
            ]
        });

        // 场景6: 多品种混合（但每次只处理一个品种）
        scenarios.push({
            name: '多品种混合-1050',
            material: MATERIALS[0],
            orders: [
                { width: 700, length: 600, quantity: 2 },
                { width: 500, length: 400, quantity: 3 },
                { width: 300, length: 250, quantity: 5 }
            ],
            inventory: [
                { width: 1500, length: 3000, quantity: 2 }
            ]
        });

        scenarios.push({
            name: '多品种混合-1060',
            material: MATERIALS[1],
            orders: [
                { width: 800, length: 700, quantity: 1 },
                { width: 600, length: 500, quantity: 2 },
                { width: 400, length: 300, quantity: 4 }
            ],
            inventory: [
                { width: 2000, length: 4000, quantity: 1 }
            ]
        });

        // 场景7: 极端比例零件
        scenarios.push({
            name: '极端比例零件',
            material: MATERIALS[2],
            orders: [
                { width: 1800, length: 200, quantity: 3 }, // 长条形
                { width: 200, length: 1800, quantity: 3 }, // 竖条形
                { width: 100, length: 100, quantity: 20 }  // 小方块
            ],
            inventory: [
                { width: 2000, length: 4000, quantity: 1 }
            ]
        });

        // 场景8: 密集小件
        scenarios.push({
            name: '密集小件',
            material: MATERIALS[0],
            orders: [
                { width: 150, length: 100, quantity: 30 },
                { width: 200, length: 150, quantity: 20 },
                { width: 250, length: 200, quantity: 15 }
            ],
            inventory: [
                { width: 1200, length: 2400, quantity: 3 }
            ]
        });

        // 场景9: 大件优先
        scenarios.push({
            name: '大件优先',
            material: MATERIALS[3],
            orders: [
                { width: 1800, length: 1600, quantity: 1 }, // 超大件
                { width: 1200, length: 1000, quantity: 2 }, // 大件
                { width: 600, length: 500, quantity: 4 }   // 中件
            ],
            inventory: [
                { width: 2000, length: 4000, quantity: 2 }
            ]
        });

        // 场景10: 零碎余料利用
        scenarios.push({
            name: '零碎余料利用',
            material: MATERIALS[1],
            orders: [
                { width: 350, length: 250, quantity: 8 },
                { width: 280, length: 220, quantity: 10 },
                { width: 180, length: 150, quantity: 15 },
                { width: 120, length: 100, quantity: 20 }
            ],
            inventory: [
                { width: 1000, length: 2000, quantity: 4 }
            ]
        });

        let scenarioResults = [];
        
        for (let i = 0; i < scenarios.length; i++) {
            console.log(`\n📋 创建场景 ${i + 1}: ${scenarios[i].name}`);
            
            // 创建库存
            const inventoryIds = [];
            for (const inv of scenarios[i].inventory) {
                const inventoryData = {
                    barcode: `TEST-${scenarios[i].material.name.replace(/\s+/g, '')}-${scenarios[i].material.thickness}mm-${i + 1}-${Math.floor(Math.random() * 1000)}`,
                    materialName: scenarios[i].material.name,
                    thickness: scenarios[i].material.thickness,
                    width: inv.width,
                    length: inv.length,
                    quantity: inv.quantity,
                    materialType: 0
                };
                
                try {
                    const response = await this.client.post('/inventory', inventoryData);
                    if (response.data.success) {
                        inventoryIds.push(response.data.data.id);
                        console.log(`   ✅ 创建库存: ${inv.width}x${inv.length}x${inv.quantity}`);
                    }
                } catch (error) {
                    console.error(`   ❌ 创建库存失败:`, error.message);
                }
            }
            
            // 创建订单
            const orderIds = [];
            for (const order of scenarios[i].orders) {
                const orderData = {
                    customer: `测试客户-${i + 1}`,
                    materialName: scenarios[i].material.name,
                    thickness: scenarios[i].material.thickness,
                    width: order.width,
                    length: order.length,
                    quantity: order.quantity
                };
                
                try {
                    const response = await this.client.post('/order', orderData);
                    if (response.data.success) {
                        orderIds.push(response.data.data.id);
                        console.log(`   ✅ 创建订单: ${order.width}x${order.length}x${order.quantity}`);
                    }
                } catch (error) {
                    console.error(`   ❌ 创建订单失败:`, error.message);
                }
            }
            
            scenarioResults.push({
                scenario: i + 1,
                name: scenarios[i].name,
                material: `${scenarios[i].material.name} ${scenarios[i].material.thickness}mm`,
                orderIds: orderIds,
                inventoryIds: inventoryIds
            });
        }
        
        return scenarioResults;
    }
}

async function main() {
    const creator = new TestDataCreator();
    
    if (!(await creator.login())) {
        console.error('❌ 登录失败');
        process.exit(1);
    }
    
    await creator.clearTestData();
    const results = await creator.createComplexTestScenarios();
    
    // 保存测试场景配置
    fs.writeFileSync('test-scenarios.json', JSON.stringify(results, null, 2));
    console.log('\n💾 测试场景配置已保存到 test-scenarios.json');
    
    console.log('\n✅ 测试数据创建完成！');
}

if (require.main === module) {
    main().catch(console.error);
}