# POS System for Small Retail Stores (C#)

Language: English | [简体中文](README.zh-CN.md)

A small Windows desktop POS app for neighborhood shops, convenience stores, and other small retail scenarios. It starts simple: keep products organized, track stock, and build toward a cashier flow that actually works end to end.

This is still an early-stage project, so the focus is on a clean foundation rather than feature overload.

## ✅ What Works Now

The basics are in place:

- WPF desktop shell with native WPF pages for the current main modules
- Built-in English / Simplified Chinese UI switch
- Basic local file logging for startup, initialization, and unexpected errors
- Local SQLite database setup on first launch
- Category management
- Product management
- Inventory viewing and adjustment
- Native WPF cashier checkout with cart, discount, split cash/online payment, and change calculation
- Order saving and inventory deduction
- Sales record search with date filters, order details, and summary totals
- Basic reports: today sales, order count, stock overview, daily/weekly/monthly sales, and product rankings
- Read-only settings page for store, database, language, and log paths
- Low-stock indicators
- Layered project structure
- Database schema script

Still on the way:

- Login and roles
- Configuration screens
- Barcode scanner and receipt printer support
- Report export

## 🧰 Tech Stack

- C#
- .NET 9
- XAML-based WPF shell and module pages
- Archived WinForms UI code kept separately as a migration fallback/reference
- SQLite
- Microsoft.Data.Sqlite

## 💻 Requirements

- Windows
- .NET 9 SDK

## 🚀 Quick Start

```powershell
git clone https://github.com/bqw9008/JMS-POS-system.git
cd POS-system-cs
dotnet restore
dotnet build
dotnet run
```

The app creates a local SQLite database the first time it runs. The table schema lives in `Data/schema.sql`.

## 🪵 Logs

Basic application logs are written per user under:

```text
%LOCALAPPDATA%\POS-system-cs\logs\app-YYYYMMDD.log
```

The current logging covers startup, language/database initialization, unexpected app errors, and WPF operation failures.


## 🧪 Test Data

Test data is not inserted by default. To seed sample categories, products, and stock while developing, enable it before running the app:

```powershell
$env:POS_SEED_TEST_DATA = "1"
dotnet run
```

Unset the variable or set it to `0` for normal use.

## 🌐 Language

The desktop UI supports English and Simplified Chinese. Use the language selector in the left navigation area to switch languages while the app is running.

Current behavior:

- On first launch, the app follows the system UI language when possible: Chinese systems use Simplified Chinese, other systems use English.
- Switching languages refreshes the current WPF page and navigation text.
- After the user switches languages, the selected language is persisted in the user's AppData settings and reused on restart.

## 🗂️ Project Layout

```text
POS-system-cs/
├── Application/          # App-level models, navigation, and service interfaces
├── Configuration/        # App settings models
├── Data/                 # Database scripts
├── Domain/               # Entities and enums
├── Infrastructure/       # SQLite access, service implementations, composition root
├── UI/
│   ├── Wpf/              # Current XAML-based WPF shell, module pages, and localization
│   └── LegacyWinForms/   # Archived legacy WinForms shell and controls
├── Program.cs            # App entry point
├── TODO.md               # Development checklist
└── POS-system-cs.csproj  # Project file
```

## 🧩 Current Modules

The app can switch between English and Simplified Chinese from the left navigation area.

### Cashier

The cashier page is native WPF. Add products by code, barcode, or a unique name match; manage the cart; use shortcuts including one-shot quantity editing; apply discounts; record cash or online payments in multiple steps with remaining amount and change calculation; use F7 for cash and F8 for online payment; save the order and deduct inventory.

### Category Management

Create, edit, list, and delete categories. Categories linked to products are protected from accidental deletion.

### Product Management

Maintain product code, name, barcode, category, cost price, sale price, low-stock threshold, active status, and safe delete/disable behavior.

### Inventory Management

View stock levels, set stock directly, adjust by quantity, and spot low-stock items quickly.

### Sales Records

Search orders by date range, review order details, and see summary totals for the current result set.

### Reports

Review today sales metrics, stock overview, daily/weekly/monthly sales summaries, and top/slow-selling product rankings.

### Settings

Review current store name, database path, receipt printer name, language settings file, log directory, and runtime directory. Editing these settings is planned for a later configuration pass.

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

- Finish product, category, inventory, cashier, sales record, and basic report flows

### Phase 2: Back Office

- Add login and roles
- Add logging
- Add configurable store/system settings
- Add report export

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

