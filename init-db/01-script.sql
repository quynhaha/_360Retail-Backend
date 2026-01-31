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
    app_user_id UUID REFERENCES identity.app_users(id) ON DELETE CASCADE,
    full_name VARCHAR(100),
    phone_number VARCHAR(20) NOT NULL,
    total_points INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales.orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    store_id UUID NOT NULL REFERENCES saas.stores(id),
    code VARCHAR(50) NOT NULL,
    employee_id UUID REFERENCES hr.employees(id),
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
(uuid_generate_v4(), 'Staff', 'Store staff'),
(uuid_generate_v4(), 'Customer', 'App customer')
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

-- 11/1/2026 - Account Activation Tokens & Temp Password Flow
CREATE TABLE IF NOT EXISTS identity.account_activation_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    token VARCHAR(200) NOT NULL,
    expired_at TIMESTAMPTZ NOT NULL,
    is_used BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_activation_user
        FOREIGN KEY (user_id)
        REFERENCES identity.app_users(id)
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_activation_token
ON identity.account_activation_tokens(token);

CREATE INDEX IF NOT EXISTS idx_activation_user
ON identity.account_activation_tokens(user_id);

-- Remove old activation columns from app_users (now using separate table)
ALTER TABLE identity.app_users
    DROP COLUMN IF EXISTS activation_token,
    DROP COLUMN IF EXISTS activation_token_expired_at;

-- Add must_change_password flag for temp password login flow
ALTER TABLE identity.app_users
ADD COLUMN IF NOT EXISTS must_change_password BOOLEAN DEFAULT FALSE;

-- Set existing active users to not require password change
UPDATE identity.app_users
SET must_change_password = FALSE
WHERE status = 'Active'
  AND is_activated = TRUE
  AND password_hash IS NOT NULL;

-- 14/1/2026: Ensure default is FALSE for must_change_password
ALTER TABLE identity.app_users
ALTER COLUMN must_change_password SET DEFAULT FALSE;

-- 18/1/2026: Add missing columns to hr.employees for Employee entity
ALTER TABLE hr.employees
ADD COLUMN IF NOT EXISTS face_data TEXT;

ALTER TABLE hr.employees
ADD COLUMN IF NOT EXISTS registered_device_id VARCHAR(100);

-- 18/1/2026: Add avatar_url column for employee avatars
ALTER TABLE hr.employees
ADD COLUMN IF NOT EXISTS avatar_url TEXT;

-- 19/1/2026: Change hr.employees from status (varchar) to is_active (boolean)
-- Step 1: Add new column
ALTER TABLE hr.employees
ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;

-- Step 2: Migrate existing data (status = 'Active' → is_active = true)
UPDATE hr.employees
SET is_active = CASE WHEN status = 'Active' THEN TRUE ELSE FALSE END
WHERE is_active IS NULL OR status IS NOT NULL;

-- Step 3: Drop old status column (optional - uncomment if you want to remove it)
-- ALTER TABLE hr.employees DROP COLUMN IF EXISTS status;

-- 20/1/2026: Add is_active column to hr.tasks for soft delete
ALTER TABLE hr.tasks
ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;

-- Set all existing tasks as active
UPDATE hr.tasks
SET is_active = TRUE
WHERE is_active IS NULL;

-- 20/1/2026: Add created_by_employee_id column to hr.tasks for update permission
ALTER TABLE hr.tasks
ADD COLUMN IF NOT EXISTS created_by_employee_id UUID REFERENCES hr.employees(id);

-- 20/1/2026: Trial period support for Manual Trial Flow
-- Add trial columns to identity.app_users
ALTER TABLE identity.app_users 
ADD COLUMN IF NOT EXISTS trial_start_date TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS trial_end_date TIMESTAMPTZ;

-- Add index for trial expiry queries
CREATE INDEX IF NOT EXISTS idx_users_trial_end 
ON identity.app_users(trial_end_date);

-- Add PotentialOwner role (user registered but not started trial yet)
INSERT INTO identity.app_roles (id, role_name, description)
VALUES (uuid_generate_v4(), 'PotentialOwner', 'User pending trial or subscription')
ON CONFLICT DO NOTHING;

-- Add Trial service plan for trial subscriptions
INSERT INTO saas.service_plans (id, plan_name, price, duration_days, features, is_active)
SELECT 
    uuid_generate_v4(), 
    'Trial', 
    0, 
    7, 
    '{"products": -1, "orders": -1, "employees": 3, "trial": true}'::jsonb, 
    TRUE
WHERE NOT EXISTS (SELECT 1 FROM saas.service_plans WHERE plan_name = 'Trial');

-- 20/1/2026: Add paid service plans for subscription purchase
-- Basic Plan - 199,000 VND / 30 days
INSERT INTO saas.service_plans (id, plan_name, price, duration_days, features, is_active, created_at)
SELECT 
    uuid_generate_v4(), 
    'Basic', 
    199000, 
    30, 
    '{"max_products": 100, "max_employees": 5, "max_orders": 500}'::jsonb, 
    TRUE,
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM saas.service_plans WHERE plan_name = 'Basic');

-- Pro Plan - 499,000 VND / 30 days
INSERT INTO saas.service_plans (id, plan_name, price, duration_days, features, is_active, created_at)
SELECT 
    uuid_generate_v4(), 
    'Pro', 
    499000, 
    30, 
    '{"max_products": 500, "max_employees": 20, "max_orders": 2000}'::jsonb, 
    TRUE,
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM saas.service_plans WHERE plan_name = 'Pro');

-- Yearly Plan - 1,990,000 VND / 365 days (save 2 months)
INSERT INTO saas.service_plans (id, plan_name, price, duration_days, features, is_active, created_at)
SELECT 
    uuid_generate_v4(), 
    'Yearly', 
    1990000, 
    365, 
    '{"max_products": 1000, "max_employees": 50, "max_orders": -1}'::jsonb, 
    TRUE,
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM saas.service_plans WHERE plan_name = 'Yearly');

-- 21/1/2026: Add user_id to payments for tracking who made the payment
ALTER TABLE saas.payments
ADD COLUMN IF NOT EXISTS user_id UUID NULL;

-- 31/1/2026: OAuth Login Support (Google, Facebook)
-- Add OAuth provider columns to identity.app_users
ALTER TABLE identity.app_users
ADD COLUMN IF NOT EXISTS auth_provider VARCHAR(20) DEFAULT 'Local',
ADD COLUMN IF NOT EXISTS external_user_id VARCHAR(255),
ADD COLUMN IF NOT EXISTS profile_picture_url TEXT;

-- Set default auth_provider for existing users
UPDATE identity.app_users
SET auth_provider = 'Local'
WHERE auth_provider IS NULL;

-- Index for faster OAuth lookups
CREATE INDEX IF NOT EXISTS idx_users_oauth_provider
ON identity.app_users(auth_provider, external_user_id);

