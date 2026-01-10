CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS saas;
CREATE SCHEMA IF NOT EXISTS hr;
CREATE SCHEMA IF NOT EXISTS sales;
CREATE SCHEMA IF NOT EXISTS crm;

CREATE TABLE IF NOT EXISTS saas.service_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    plan_name VARCHAR(100) NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    duration_days INT NOT NULL,
    features JSONB,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS saas.stores (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_name VARCHAR(200) NOT NULL,
    address TEXT,
    phone VARCHAR(20),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS saas.subscriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id) ON DELETE CASCADE,
    plan_id UUID NOT NULL REFERENCES saas.service_plans(id),
    start_date TIMESTAMP,
    end_date TIMESTAMP,
    status VARCHAR(50),
    auto_renew BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS saas.payments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    subscription_id UUID NOT NULL REFERENCES saas.subscriptions(id),
    amount DECIMAL(18,2) NOT NULL,
    payment_method VARCHAR(50),
    transaction_code VARCHAR(100),
    status VARCHAR(50),
    payment_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS identity.app_users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL,
    password_hash TEXT NOT NULL,
    phone_number VARCHAR(20),
    is_active BOOLEAN DEFAULT TRUE,
    store_id UUID REFERENCES saas.stores(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS identity.app_roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_name VARCHAR(50) NOT NULL,
    description VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS identity.user_roles (
    user_id UUID NOT NULL REFERENCES identity.app_users(id),
    role_id UUID NOT NULL REFERENCES identity.app_roles(id),
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS identity.user_store_access (
    user_id UUID NOT NULL REFERENCES identity.app_users(id) ON DELETE CASCADE,
    store_id UUID NOT NULL REFERENCES saas.stores(id) ON DELETE CASCADE,
    role_in_store VARCHAR(50) NOT NULL,
    is_default BOOLEAN DEFAULT FALSE,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, store_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_staff_single_store
ON identity.user_store_access(user_id)
WHERE role_in_store = 'Staff';

INSERT INTO identity.user_store_access (user_id, store_id, role_in_store, is_default)
SELECT
    u.id,
    u.store_id,
    r.role_name,
    TRUE
FROM identity.app_users u
JOIN identity.user_roles ur ON ur.user_id = u.id
JOIN identity.app_roles r ON r.id = ur.role_id
WHERE u.store_id IS NOT NULL
ON CONFLICT DO NOTHING;

CREATE TABLE IF NOT EXISTS hr.employees (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id) ON DELETE CASCADE,
    app_user_id UUID NOT NULL REFERENCES identity.app_users(id) ON DELETE CASCADE,
    full_name VARCHAR(100) NOT NULL,
    position VARCHAR(100),
    base_salary DECIMAL(18,2),
    join_date TIMESTAMP,
    status VARCHAR(50) DEFAULT 'Active'
);

CREATE TABLE IF NOT EXISTS hr.timekeepings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id UUID NOT NULL REFERENCES hr.employees(id),
    store_id UUID NOT NULL REFERENCES saas.stores(id),
    check_in_time TIMESTAMP,
    check_out_time TIMESTAMP,
    location_gps VARCHAR(255),
    check_in_image_url TEXT,
    is_late BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS hr.tasks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id),
    title VARCHAR(200) NOT NULL,
    assignee_id UUID NOT NULL REFERENCES hr.employees(id),
    status VARCHAR(50),
    priority VARCHAR(20),
    description TEXT,
    deadline TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales.categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id) ON DELETE CASCADE,
    category_name VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES sales.categories(id),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_categories_store
ON sales.categories(store_id);

CREATE TABLE IF NOT EXISTS sales.products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id) ON DELETE CASCADE,
    category_id UUID REFERENCES sales.categories(id),
    product_name VARCHAR(200) NOT NULL,
    bar_code VARCHAR(50),
    price DECIMAL(18,2) NOT NULL,
    cost_price DECIMAL(18,2),
    stock_quantity INT DEFAULT 0,
    image_url TEXT,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_products_store
ON sales.products(store_id);

CREATE TABLE IF NOT EXISTS sales.product_variants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES sales.products(id) ON DELETE CASCADE,
    sku VARCHAR(50) NOT NULL,
    size VARCHAR(20),
    color VARCHAR(20),
    price_override DECIMAL(18,2),
    stock_quantity INT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS crm.customers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id) ON DELETE CASCADE,
    full_name VARCHAR(100),
    phone_number VARCHAR(20) NOT NULL,
    total_points INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales.orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id),
    code VARCHAR(50) NOT NULL,
    employee_id UUID NOT NULL REFERENCES hr.employees(id),
    customer_id UUID REFERENCES crm.customers(id),
    total_amount DECIMAL(18,2) NOT NULL,
    discount_amount DECIMAL(18,2) DEFAULT 0,
    status VARCHAR(50),
    payment_method VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales.order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES sales.orders(id),
    product_id UUID NOT NULL REFERENCES sales.products(id),
    quantity INT NOT NULL,
    unit_price DECIMAL(18,2) NOT NULL,
    total DECIMAL(18,2) NOT NULL
);

CREATE TABLE IF NOT EXISTS sales.inventory_tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id),
    code VARCHAR(50),
    type VARCHAR(50),
    created_by_employee_id UUID REFERENCES hr.employees(id),
    note TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS crm.loyalty_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES crm.customers(id),
    order_id UUID REFERENCES sales.orders(id),
    points_changed INT NOT NULL,
    type VARCHAR(50),
    change_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS crm.customer_feedbacks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id),
    customer_id UUID NOT NULL REFERENCES crm.customers(id),
    content TEXT,
    rating INT,
    source VARCHAR(50),
    created_by_employee_id UUID REFERENCES hr.employees(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO identity.app_roles (id, role_name, description)
VALUES
(uuid_generate_v4(), 'SuperAdmin', 'System administrator'),
(uuid_generate_v4(), 'StoreOwner', 'Store owner'),
(uuid_generate_v4(), 'Manager', 'Store manager'),
(uuid_generate_v4(), 'Staff', 'Store staff')
ON CONFLICT DO NOTHING;


CREATE TABLE IF NOT EXISTS sales.payment_transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    order_id UUID NOT NULL REFERENCES sales.orders(id) ON DELETE CASCADE,
    store_id UUID NOT NULL REFERENCES saas.stores(id) ON DELETE CASCADE,

    provider VARCHAR(50) NOT NULL,
    provider_transaction_id VARCHAR(100),
    payment_method VARCHAR(50),

    amount DECIMAL(18,2) NOT NULL,
    currency VARCHAR(10) DEFAULT 'VND',

    status VARCHAR(50) NOT NULL,

    request_payload JSONB,
    response_payload JSONB,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_payment_order
ON sales.payment_transactions(order_id);

CREATE INDEX IF NOT EXISTS idx_payment_store
ON sales.payment_transactions(store_id);

CREATE INDEX IF NOT EXISTS idx_payment_provider_tx
ON sales.payment_transactions(provider_transaction_id);

ALTER TABLE sales.orders
ADD COLUMN IF NOT EXISTS payment_status VARCHAR(50) DEFAULT 'Unpaid';

ALTER TABLE saas.payments
ADD COLUMN IF NOT EXISTS provider VARCHAR(50),
ADD COLUMN IF NOT EXISTS provider_transaction_id VARCHAR(100),
ADD COLUMN IF NOT EXISTS request_payload JSONB,
ADD COLUMN IF NOT EXISTS response_payload JSONB;

ALTER TABLE identity.app_users
    DROP COLUMN IF EXISTS is_active;

ALTER TABLE identity.app_users
    ALTER COLUMN password_hash DROP NOT NULL;

ALTER TABLE identity.app_users
    ADD COLUMN IF NOT EXISTS status VARCHAR(30) DEFAULT 'Pending',
    ADD COLUMN IF NOT EXISTS is_activated BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS activation_token VARCHAR(100),
    ADD COLUMN IF NOT EXISTS activation_token_expired_at TIMESTAMP;

--  Mới thêm ngày 6/1/2026
ALTER TABLE saas.stores
ALTER COLUMN created_at
TYPE timestamptz;

-- subscriptions
ALTER TABLE saas.subscriptions
ALTER COLUMN start_date TYPE timestamptz;

ALTER TABLE saas.subscriptions
ALTER COLUMN end_date TYPE timestamptz;

-- service_plans
ALTER TABLE saas.service_plans
ALTER COLUMN created_at TYPE timestamptz;

-- payments
ALTER TABLE saas.payments
ALTER COLUMN payment_date TYPE timestamptz;


INSERT INTO identity.app_users (
    id,
    user_name,
    email,
    password_hash,
    phone_number,
    store_id,
    status,
    is_activated,
    created_at
)
SELECT
    uuid_generate_v4(),
    'admin',
    'admin',
    'pmWkWSBCL51Bfkhn79xPuKBKHz//H6B+mY6G9/eieuM=',
    NULL,
    NULL,
    'Active',
    TRUE,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM identity.app_users WHERE email = 'admin');

-- Script thêm admin 
INSERT INTO identity.app_users (
    id,
    user_name,
    email,
    password_hash,
    phone_number,
    store_id,
    status,
    is_activated,
    created_at
)
SELECT
    uuid_generate_v4(),
    'admin',
    'admin',
    'pmWkWSBCL51Bfkhn79xPuKBKHz//H6B+mY6G9/eieuM=',
    NULL,
    NULL,
    'Active',
    TRUE,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM identity.app_users WHERE email = 'admin'
);

INSERT INTO identity.user_roles (user_id, role_id)
SELECT u.id, r.id
FROM identity.app_users u
JOIN identity.app_roles r ON r.role_name = 'SuperAdmin'
WHERE u.email = 'admin'
AND NOT EXISTS (
    SELECT 1
    FROM identity.user_roles ur
    WHERE ur.user_id = u.id AND ur.role_id = r.id
);


-- 1/9
ALTER TABLE sales.order_items 
ADD COLUMN IF NOT EXISTS product_variant_id UUID NULL REFERENCES sales.product_variants(id);

ALTER TABLE sales.orders 
ADD COLUMN IF NOT EXISTS payment_status VARCHAR(50);
