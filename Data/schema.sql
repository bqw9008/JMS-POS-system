CREATE TABLE IF NOT EXISTS categories (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NULL
);

CREATE TABLE IF NOT EXISTS products (
    id TEXT PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    barcode TEXT NOT NULL UNIQUE,
    category_id TEXT NOT NULL,
    cost_price REAL NOT NULL,
    sale_price REAL NOT NULL,
    low_stock_threshold REAL NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NULL,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

CREATE TABLE IF NOT EXISTS stock (
    id TEXT PRIMARY KEY,
    product_id TEXT NOT NULL UNIQUE,
    quantity REAL NOT NULL DEFAULT 0,
    last_changed_at TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NULL,
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    user_name TEXT NOT NULL UNIQUE,
    display_name TEXT NOT NULL,
    role INTEGER NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NULL
);

CREATE TABLE IF NOT EXISTS orders (
    id TEXT PRIMARY KEY,
    order_no TEXT NOT NULL UNIQUE,
    ordered_at TEXT NOT NULL,
    operator_id TEXT NULL,
    total_amount REAL NOT NULL,
    discount_amount REAL NOT NULL DEFAULT 0,
    received_amount REAL NOT NULL,
    payment_method INTEGER NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NULL,
    FOREIGN KEY (operator_id) REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS order_items (
    id TEXT PRIMARY KEY,
    order_id TEXT NOT NULL,
    product_id TEXT NOT NULL,
    product_name TEXT NOT NULL,
    barcode TEXT NOT NULL,
    quantity REAL NOT NULL,
    unit_price REAL NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);
