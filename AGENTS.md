# NestGuard 智能套料专家 - 项目知识库

## 项目概述

**名称**: NestGuard 智能套料专家  
**用途**: 铝板加工企业的智能套料优化系统  
**功能**: 订单管理、库存管理、智能套料计算、余料/废料追踪

## 技术栈

| 层级 | 技术 | 端口 |
|------|------|------|
| 后端 | ASP.NET Core 9.0 + SqlSugar + SQLite | 5000 |
| 前端 | Vue 3 + TypeScript + Element Plus + Vite | 3000 |

## 目录结构

```
项目根目录/
├── AluminumApi/              # 后端
│   ├── Controllers/          # API 控制器
│   │   ├── AuthController.cs       # 登录认证
│   │   ├── OrderController.cs      # 订单管理
│   │   ├── InventoryController.cs  # 库存管理
│   │   ├── NestingController.cs    # 套料计算
│   │   ├── VarietyController.cs    # 品种设置
│   │   └── WasteSettingController.cs # 废料设置
│   ├── Services/
│   │   └── NestingService.cs       # 核心套料算法 ⭐
│   ├── Models/               # 数据模型
│   ├── Dtos/                 # DTO
│   ├── Program.cs            # 启动配置
│   ├── appsettings.json      # 配置
│   └── aluminum.db           # SQLite 数据库
│
├── aluminum-app/             # 前端
│   ├── src/
│   │   ├── views/
│   │   │   ├── Dashboard.vue      # 主控制台
│   │   │   ├── Order.vue          # 订单管理
│   │   │   ├── Inventory.vue      # 库存管理
│   │   │   ├── Nesting.vue        # 智能套料任务 ⭐
│   │   │   ├── Result.vue         # 历史记录
│   │   │   └── ...
│   │   ├── api/index.ts      # API 接口
│   │   └── ...
│   ├── package.json
│   └── vite.config.ts
│
└── AGENTS.md                 # 本知识库文件
```

## 启动命令

```bash
# 后端 (在 AluminumApi 目录)
dotnet run --urls="http://localhost:5000"

# 前端 (在 aluminum-app 目录)
npm run dev
```

## 端口配置 (不可修改)

- **后端**: 5000
- **前端**: 3000
- 前端通过 Vite proxy 代理到后端 5000

## API 路由

| 模块 | 路由 | 说明 |
|------|------|------|
| 认证 | POST /api/auth/login | 登录获取 JWT |
| 订单 | GET/POST/PUT/DELETE /api/order | CRUD |
| 库存 | GET/POST/PUT/DELETE /api/inventory | CRUD |
| 套料 | POST /api/nesting/calculate | 计算套料方案 |
| 套料 | POST /api/nesting/confirm | 确认采用方案 |
| 品种 | GET/POST /api/variety | 品种管理 |
| 废料 | GET/PUT /api/wastesetting | 废料设置 |

## 默认账号

- 用户名: `admin`
- 密码: `admin123`

## 套料算法逻辑 (NestingService.cs)

### 核心流程
1. 验证订单（同品种、同厚度）
2. 按策略选择库存排序
3. 展开订单为零件列表（按数量）
4. 按面积降序排列零件
5. 贪心放置 + 智能旋转选择
6. 计算余料/废料

### 选料策略
- `minMatch`: 最小匹配优先
- `remnantFirst`: 余料优先
- `largeFirst`: 大板优先

### 放置算法
- 行优先扫描
- 智能旋转：自动选择能放更多零件的方向
- `fixedDirection=true`: 禁止旋转

### 余料判定条件
- 长度 >= minLength (默认100mm)
- 宽度 >= minWidth (默认100mm)
- 面积 >= maxWasteArea (默认0.1㎡)

## 已知问题与修复记录

| 问题 | 状态 | 解决方案 |
|------|------|----------|
| 后端未运行导致登录失败 | ✅ 已修复 | 启动时自动检查 |
| 订单数量未展开 | ✅ 已修复 | decimal→int 转换 |
| 库存 quantity 字段未处理 | ✅ 已修复 | 循环处理多张同规格板 |
| 余料计算不准确 | ✅ 已修复 | 检查底部+右侧区域 |

## 测试数据 (2026-02-23)

### 库存数据
| 条码 | 品种 | 厚度 | 尺寸 | 数量 |
|------|------|------|------|------|
| A1-001 | 1050纯铝板 | 1mm | 4000×2000 | 2张 |
| A1-002 | 1050纯铝板 | 1mm | 2000×1000 | 1张 |
| A1-R01 | 1050纯铝板 | 1mm | 800×500 | 1张(余料) |
| A2-001 | 1050纯铝板 | 2mm | 4000×2000 | 1张 |
| A2-002 | 1050纯铝板 | 2mm | 1500×1000 | 1张 |
| B1-001 | 1060纯铝板 | 1mm | 3000×2000 | 1张 |

### 订单数据
| 订单号 | 客户 | 品种 | 零件尺寸 | 数量 |
|--------|------|------|----------|------|
| ORD-TEST-001 | 小米 | 1050纯铝板 1mm | 100×150 | 20件 |
| ORD-TEST-002 | 华为 | 1050纯铝板 1mm | 200×300 | 10件 |
| ORD-TEST-003 | OPPO | 1050纯铝板 1mm | 150×200 | 15件 |
| ORD-TEST-004 | 富士康 | 1050纯铝板 2mm | 300×400 | 8件 |
| ORD-TEST-005 | 比亚迪 | 1060纯铝板 1mm | 250×350 | 6件 |
| ORD-TEST-006 | VIVO | 1050纯铝板 1mm | 50×80 | 50件 |

## Linux 部署

### 环境要求
```bash
# .NET 9.0
sudo apt update
sudo apt install -y dotnet-sdk-9.0

# Node.js 18+
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install -y nodejs
```

### 部署步骤
```bash
# 1. 克隆/上传项目到服务器

# 2. 启动后端
cd AluminumApi
dotnet restore
dotnet run --urls="http://0.0.0.0:5000"

# 3. 启动前端
cd ../aluminum-app
npm install
npm run dev -- --host 0.0.0.0
```

## 后续优化方向

1. [ ] 套料算法进一步优化（矩形装箱问题）
2. [ ] 支持不规则形状零件
3. [ ] 批量导入订单/库存
4. [ ] 生产进度跟踪
5. [ ] 多用户权限管理
