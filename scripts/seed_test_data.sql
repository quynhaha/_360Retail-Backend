-- Seed fake data for dashboard testing

-- Tạo thêm 4 stores
INSERT INTO saas.stores (id, store_name, is_active, created_at) VALUES
  ('aaaaaaaa-0001-0001-0001-000000000001', 'Cửa hàng Minh Anh', true, NOW() - INTERVAL '60 days'),
  ('aaaaaaaa-0002-0002-0002-000000000002', 'Shop Thời Trang HN', true, NOW() - INTERVAL '45 days'),
  ('aaaaaaaa-0003-0003-0003-000000000003', 'Điện máy Phúc Lộc', true, NOW() - INTERVAL '30 days'),
  ('aaaaaaaa-0004-0004-0004-000000000004', 'Tạp hoá Bình Định', true, NOW() - INTERVAL '15 days');

-- Store 2: Mua Basic (Active)
INSERT INTO saas.subscriptions (id, store_id, plan_id, start_date, end_date, status)
SELECT 'bbbbbbbb-0001-0001-0001-000000000001', 'aaaaaaaa-0001-0001-0001-000000000001', id, NOW() - INTERVAL '25 days', NOW() + INTERVAL '5 days', 'Active'
FROM saas.service_plans WHERE plan_name = 'Basic';

INSERT INTO saas.payments (subscription_id, amount, payment_method, status, payment_date)
VALUES ('bbbbbbbb-0001-0001-0001-000000000001', 199000, 'BankTransfer', 'Completed', NOW() - INTERVAL '25 days');

-- Store 3: Mua Pro (Active) - 2 payments (renew lần 2)
INSERT INTO saas.subscriptions (id, store_id, plan_id, start_date, end_date, status)
SELECT 'bbbbbbbb-0002-0002-0002-000000000002', 'aaaaaaaa-0002-0002-0002-000000000002', id, NOW() - INTERVAL '10 days', NOW() + INTERVAL '20 days', 'Active'
FROM saas.service_plans WHERE plan_name = 'Pro';

INSERT INTO saas.payments (subscription_id, amount, payment_method, status, payment_date) VALUES
  ('bbbbbbbb-0002-0002-0002-000000000002', 499000, 'VNPay', 'Completed', NOW() - INTERVAL '40 days'),
  ('bbbbbbbb-0002-0002-0002-000000000002', 499000, 'BankTransfer', 'Completed', NOW() - INTERVAL '10 days');

-- Store 4: Còn Trial (Trialing)
INSERT INTO saas.subscriptions (id, store_id, plan_id, start_date, end_date, status)
SELECT 'bbbbbbbb-0003-0003-0003-000000000003', 'aaaaaaaa-0003-0003-0003-000000000003', id, NOW() - INTERVAL '3 days', NOW() + INTERVAL '4 days', 'Trialing'
FROM saas.service_plans WHERE plan_name = 'Trial';

-- Store 5: Hết hạn (Expired) - đã từng mua Basic
INSERT INTO saas.subscriptions (id, store_id, plan_id, start_date, end_date, status)
SELECT 'bbbbbbbb-0004-0004-0004-000000000004', 'aaaaaaaa-0004-0004-0004-000000000004', id, NOW() - INTERVAL '35 days', NOW() - INTERVAL '5 days', 'Expired'
FROM saas.service_plans WHERE plan_name = 'Basic';

INSERT INTO saas.payments (subscription_id, amount, payment_method, status, payment_date)
VALUES ('bbbbbbbb-0004-0004-0004-000000000004', 199000, 'BankTransfer', 'Completed', NOW() - INTERVAL '35 days');
