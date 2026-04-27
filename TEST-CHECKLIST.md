# POS System Test Checklist

This checklist is for manual verification of the current WPF desktop flow.

Mark completed items with `[x]`.

## Scope

Current focus:

- WPF shell startup
- English / Simplified Chinese switching
- Category, product, inventory, cashier, sales record, reports, and settings flows
- Receipt preview window after checkout

Out of current scope:

- Real receipt printer integration
- Barcode scanner hardware compatibility
- Login / roles
- Refunds

## Test Setup

Environment:

- Windows
- .NET 9 SDK

Recommended preparation:

1. Run `dotnet build`
2. Optionally seed sample data for easier validation:

```powershell
$env:POS_SEED_TEST_DATA = "1"
dotnet run
```

3. For normal-use verification, unset the variable or set it to `0`
4. If retesting database initialization, delete the local `pos.db` first

## Smoke Test

- [x] App starts without crash
- [ ] Local SQLite database is created on first launch when missing
- [x] Main window opens with left navigation and default module content
- [x] Current language follows saved preference, or system UI language on first launch
- [x] Switching language updates navigation text and current page text
- [x] Restarting the app keeps the selected language

## Category Management

- [x] Create a category from the new-category dialog with valid name and optional description
- [x] Edit an existing category from the edit dialog and confirm the list refreshes
- [x] Confirm the category detail panel outside the dialog is read-only
- [x] Delete a category that has no linked products
- [x] Confirm deletion is blocked for a category linked to products
- [x] Confirm empty category name is rejected

## Product Management

- [x] Create a product from the new-product dialog with code, name, barcode, category, cost, sale price, and low-stock threshold
- [x] Edit an existing product from the edit dialog and confirm changes are persisted
- [x] Confirm the product detail panel outside the dialog is read-only
- [x] Search by code, barcode, and name
- [x] Delete a product with no sales history and confirm it is removed
- [x] Delete a product with sales history and confirm it is disabled instead of hard deleted
- [x] Confirm empty required fields are rejected
- [x] Confirm negative price or low-stock values are rejected

## Inventory Management

- [x] Open inventory page and verify current stock list loads
- [x] Adjust stock by delta with positive quantity
- [x] Adjust stock by delta with negative quantity
- [x] Set stock directly to a valid value
- [x] Confirm stock cannot become negative
- [x] Confirm adjustment quantity `0` is rejected
- [x] Confirm low-stock items are visually distinguishable

## Cashier Basic Flow

- [x] Add product by code
- [x] Add product by barcode
- [x] Add product by unique name match
- [x] Confirm duplicate add increments quantity instead of creating a new line
- [x] Edit selected quantity with `F6`
- [x] Remove selected cart item with `Delete`
- [x] Clear cart with `F4`
- [x] Focus product input with `F2`
- [x] Clear input with `Esc`
- [x] Confirm empty cart cannot enter checkout

## Cashier Payment Flow

- [x] Checkout with full cash payment
- [x] Checkout with full online payment
- [x] Checkout with split payment using cash and online
- [x] Use `F7` for cash and `F8` for online inside payment dialog
- [x] Leave received amount empty and confirm it defaults to remaining due
- [x] Enter more than remaining due in cash and confirm change is calculated
- [x] Confirm checkout cannot complete while there is remaining due
- [x] Confirm received input is cleared after each recorded payment

## Receipt Preview

- [x] Confirm checkout success opens receipt preview window
- [x] Confirm preview shows store name
- [x] Confirm preview shows order number and checkout time
- [x] Confirm preview shows payment method
- [x] Confirm preview shows each line item with quantity, unit price, and amount
- [x] Confirm preview shows total, discount, payable, received, and change
- [x] Confirm preview is preview-only and no real printing occurs
- [x] Close preview and confirm cashier cart is cleared afterward

## Order Persistence And Stock Deduction

- [x] Complete a checkout and verify the order appears in sales records
- [x] Open the saved order and verify line items match the cart
- [x] Confirm stock is deducted after successful checkout
- [x] Confirm sold products cannot be hard deleted afterward

## Sales Records

- [x] Query orders for today
- [x] Query orders for a custom date range
- [x] Confirm invalid date range is rejected
- [x] Open order details and verify totals
- [x] Confirm summary totals match the current result set

## Reports

- [x] Open reports page and confirm dashboard metrics load
- [x] Verify today sales and today order count after recent checkouts
- [x] Verify stock overview reflects inventory changes
- [x] Verify daily, weekly, and monthly summary tabs load
- [x] Verify top-selling and slow-selling product rankings load

## Settings

- [x] Open settings page and verify store name, database path, printer name, settings file, language settings file, log directory, and runtime directory are displayed
- [x] Edit and save store name successfully
- [ ] Edit and save receipt printer name successfully
- [ ] Edit and save database path successfully
- [ ] Confirm reset reloads current saved values
- [ ] Confirm changing the database path initializes the target database automatically
- [ ] Confirm empty, invalid, or directory-only database paths are rejected before save

## Logging

- [x] Confirm log file is created under `%LOCALAPPDATA%\POS-system-cs\logs\`
- [x] Confirm app start and exit are recorded
- [x] Confirm checkout activity is recorded
- [x] Confirm inventory changes are recorded
- [x] Confirm unexpected operation failures are written to log

## Negative And Edge Cases

- [x] Try checkout with nonexistent product input
- [ ] Try checkout with multiple fuzzy product matches
- [ ] Try checkout with insufficient stock
- [ ] Try entering invalid discount format
- [ ] Try entering discount larger than total
- [ ] Try setting quantity to `0` or negative in quantity dialog
- [ ] Try database initialization with missing or invalid schema file only in a controlled dev environment

## Suggested Result Format

For each item, use:

- [x] Completed and passed
- [ ] Not completed yet

Recommended notes:

- Build version or commit
- Test date
- Tester
- Failed step
- Screenshot or log path if relevant
