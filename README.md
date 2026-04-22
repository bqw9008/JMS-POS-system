# 小型商超 POS 系统（C#）

一个面向小型商超内部使用的 Windows 桌面 POS 系统。当前目标是先完成单机版最小可用流程，再逐步扩展前台收银、销售记录、统计报表、扫码枪和小票打印能力。

## 当前状态

项目目前处于基础框架与后台基础资料管理阶段，已完成：

- WinForms 桌面应用框架
- SQLite 本地数据库初始化
- 商品分类管理
- 商品基础资料管理
- 库存查看与调整
- 低库存标识
- 基础分层目录结构
- 数据库建表脚本

暂未完成：

- 前台收银完整流程
- 订单生成与库存扣减联动
- 销售记录查询
- 统计报表
- 登录与权限
- 扫码枪和小票打印接入

## 技术栈

- C#
- .NET 9
- WinForms
- SQLite
- Microsoft.Data.Sqlite

## 运行环境

- Windows
- .NET 9 SDK

## 快速开始

```powershell
git clone <your-repo-url>
cd POS-system-cs
dotnet restore
dotnet build
dotnet run
```

程序首次启动时会自动在输出目录创建本地 SQLite 数据库，并执行 `Data/schema.sql` 中的建表脚本。

## 项目结构

```text
POS-system-cs/
├── Application/          # 应用层：服务接口、导航定义、应用模型
├── Configuration/        # 配置模型
├── Data/                 # 数据库脚本
├── Domain/               # 领域实体与枚举
├── Infrastructure/       # 基础设施：SQLite、服务实现、组合根
├── UI/                   # WinForms 界面
├── Program.cs            # 应用入口
├── TODO.md               # 开发计划
└── POS-system-cs.csproj  # 项目文件
```

## 已有模块

### 分类管理

- 分类列表展示
- 新增分类
- 编辑分类
- 删除分类
- 分类关联商品时禁止删除

### 商品管理

- 商品列表展示
- 商品搜索
- 新增商品
- 编辑商品
- 商品停用
- 商品条码、分类、进价、售价、库存预警值维护

### 库存管理

- 库存列表查看
- 直接设置库存
- 按增减数量调整库存
- 低库存提示

## 数据库

当前使用 SQLite，本地数据库默认文件名为 `pos.db`。核心表包括：

- `categories`
- `products`
- `stock`
- `users`
- `orders`
- `order_items`

建表脚本位于 `Data/schema.sql`。

## 开发路线

### Phase 1：最小可用版本

- 完成商品、分类、库存基础管理
- 完成前台收银主流程
- 完成订单保存与库存扣减
- 完成销售记录查询

### Phase 2：后台完善

- 基础统计报表
- 登录与角色区分
- 日志记录
- 配置管理

### Phase 3：设备与部署

- 扫码枪适配
- 小票打印
- 安装部署
- 界面优化

## 贡献

当前项目仍处于早期阶段，建议先从 `TODO.md` 中的 P0 任务开始推进。提交代码前请确保：

```powershell
dotnet build
```

可以通过。

## License

This project is licensed under the MIT License. See `LICENSE` for details.
