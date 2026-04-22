# POS System for Small Retail Stores (C#)

Language: English | [简体中文](README.zh-CN.md)

A small Windows desktop POS app for neighborhood shops, convenience stores, and other small retail scenarios. It starts simple: keep products organized, track stock, and build toward a cashier flow that actually works end to end.

This is still an early-stage project, so the focus is on a clean foundation rather than feature overload.

## ✅ What Works Now

The basics are in place:

- WinForms desktop app shell
- Local SQLite database setup on first launch
- Category management
- Product management
- Inventory viewing and adjustment
- Cashier checkout with cart, discount, received amount, and change calculation
- Order saving and inventory deduction
- Low-stock indicators
- Layered project structure
- Database schema script

Still on the way:

- Sales record search
- Reports and statistics
- Login and roles
- Barcode scanner and receipt printer support

## 🧰 Tech Stack

- C#
- .NET 9
- WinForms
- SQLite
- Microsoft.Data.Sqlite

## 💻 Requirements

- Windows
- .NET 9 SDK

## 🚀 Quick Start

```powershell
git clone <your-repo-url>
cd POS-system-cs
dotnet restore
dotnet build
dotnet run
```

The app creates a local SQLite database the first time it runs. The table schema lives in `Data/schema.sql`.

## 🗂️ Project Layout

```text
POS-system-cs/
├── Application/          # App-level models, navigation, and service interfaces
├── Configuration/        # App settings models
├── Data/                 # Database scripts
├── Domain/               # Entities and enums
├── Infrastructure/       # SQLite access, service implementations, composition root
├── UI/                   # WinForms screens and controls
├── Program.cs            # App entry point
├── TODO.md               # Development checklist
└── POS-system-cs.csproj  # Project file
```

## 🧩 Current Modules

### Cashier

Add products by code, barcode, or a unique name match; manage the cart; apply discounts; enter received cash; calculate change; save the order and deduct inventory.

### Category Management

Create, edit, list, and delete categories. Categories linked to products are protected from accidental deletion.

### Product Management

Maintain product code, name, barcode, category, cost price, sale price, low-stock threshold, and active status.

### Inventory Management

View stock levels, set stock directly, adjust by quantity, and spot low-stock items quickly.

## 🗄️ Database

SQLite is used for now to keep local setup simple. The default database file is `pos.db`.

Core tables:

- `categories`
- `products`
- `stock`
- `users`
- `orders`
- `order_items`

## 🛣️ Roadmap

### Phase 1: MVP

- Finish product, category, inventory, and cashier basics
- Add sales record search

### Phase 2: Back Office

- Add basic reports
- Add login and roles
- Add logging
- Add configurable store/system settings

### Phase 3: Devices and Release

- Support barcode scanners
- Support receipt printing
- Prepare deployment package
- Polish the UI

## 🤝 Contributing

The project is young, so `TODO.md` is the best place to see what still needs work. Before opening a PR or committing changes, make sure the project builds:

```powershell
dotnet build
```

## 📄 License

MIT License. See `LICENSE` for details.
