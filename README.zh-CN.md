# 小型商超 POS 系统（C#）

语言：[English](README.md) | 简体中文

一个面向小商店、便利店、小型商超的 Windows 桌面 POS 系统。目标不是一开始就做得很重，而是先把商品、库存和收银这几条核心流程跑通，再逐步补齐报表、扫码、小票打印等能力。

项目目前还在早期阶段，所以现在更重视结构清晰、功能可持续扩展，而不是一次性堆满功能。

## ✅ 现在能做什么

目前已经完成了一些基础能力：

- WinForms 桌面应用外壳
- 首次启动自动初始化本地 SQLite 数据库
- 分类管理
- 商品管理
- 库存查看与调整
- 低库存提示
- 基础分层目录结构
- 数据库建表脚本

还在路上：

- 前台收银
- 订单保存与库存扣减
- 销售记录查询
- 统计报表
- 登录和角色
- 扫码枪与小票打印支持

## 🧰 技术栈

- C#
- .NET 9
- WinForms
- SQLite
- Microsoft.Data.Sqlite

## 💻 运行环境

- Windows
- .NET 9 SDK

## 🚀 快速开始

```powershell
git clone <your-repo-url>
cd POS-system-cs
dotnet restore
dotnet build
dotnet run
```

程序首次启动时会自动创建本地 SQLite 数据库。建表脚本在 `Data/schema.sql`。

## 🗂️ 项目结构

```text
POS-system-cs/
├── Application/          # 应用层模型、导航、服务接口
├── Configuration/        # 配置模型
├── Data/                 # 数据库脚本
├── Domain/               # 领域实体与枚举
├── Infrastructure/       # SQLite 访问、服务实现、组合根
├── UI/                   # WinForms 界面和控件
├── Program.cs            # 应用入口
├── TODO.md               # 开发清单
└── POS-system-cs.csproj  # 项目文件
```

## 🧩 当前模块

### 分类管理

可以新增、编辑、查看和删除分类。已经关联商品的分类会被保护，避免误删。

### 商品管理

可以维护商品编码、名称、条码、分类、进价、售价、库存预警值和启用状态。

### 库存管理

可以查看库存、直接设置库存、按数量增减调整库存，也可以快速看到低库存商品。

## 🗄️ 数据库

目前使用 SQLite，方便本地运行和开发。默认数据库文件名是 `pos.db`。

核心表：

- `categories`
- `products`
- `stock`
- `users`
- `orders`
- `order_items`

## 🛣️ 开发路线

### Phase 1：最小可用版本

- 完成商品、分类、库存基础管理
- 增加前台收银流程
- 保存订单并扣减库存
- 增加销售记录查询

### Phase 2：后台能力

- 增加基础报表
- 增加登录和角色
- 增加日志
- 增加店铺和系统配置

### Phase 3：设备与发布

- 支持扫码枪
- 支持小票打印
- 准备部署包
- 优化界面体验

## 🤝 贡献

项目还比较早期，想参与的话可以先看 `TODO.md`。提交代码前请先确认能正常构建：

```powershell
dotnet build
```

## 📄 License

MIT License，详情见 `LICENSE`。

