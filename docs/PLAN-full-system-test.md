# PLAN: Full System Test — Reset & Validate All Flows

> **Mục tiêu**: Xóa sạch Docker data → Rebuild → Test toàn bộ luồng hoạt động từ đầu, bao gồm validate, error logs, Email (Resend), Google Login, và SignalR.

---

## Phase 0: Docker Reset & Rebuild

### Bước 1 — Dừng & Xóa sạch Docker
```bash
# 1. Dừng tất cả containers
docker-compose down

# 2. Xóa toàn bộ volumes (DB + Redis data)
docker volume rm exe-project_postgres_data exe-project_redis_data

# 3. Xóa cached images để build lại từ đầu
docker-compose build --no-cache

# 4. Khởi động lại tất cả services
docker-compose up -d
```

### Bước 2 — Verify containers đã healthy
```bash
docker ps
# Kiểm tra 8 containers: postgres, redis, pgadmin, identity-api, saas-api, sales-api, hr-api, crm-api, api-gateway
docker logs 360retail-identity-api --tail 50
docker logs 360retail-saas-api --tail 50
docker logs 360retail-sales-api --tail 50
docker logs 360retail-hr-api --tail 50
docker logs 360retail-crm-api --tail 50
docker logs 360retail-api-gateway --tail 50
```

### Bước 3 — Verify init-db script chạy đúng
```bash
docker logs 360retail-db --tail 100
# Confirm: schemas (identity, saas, hr, sales, crm) + tables + seed data (roles, plans, admin user)
```

---

## Phase 1: Identity Service — Auth Flow Testing

### 1.1 Register (4 tài khoản test)

| # | Email | Password | FullName | PhoneNumber |
|---|-------|----------|----------|-------------|
| 1 | `tranhoangminhbla123@gmail.com` | `Test@1234` | Tran Hoang Minh | 0901234567 |
| 2 | `minhtran0903tumi@gmail.com` | `Test@1234` | Minh Tran | 0902345678 |
| 3 | `tuanminh.tumi0903@gmail.com` | `Test@1234` | Tuan Minh | 0903456789 |
| 4 | `lanhuong8899z@gmail.com` | `Test@1234` | Lan Huong | 0904567890 |

**API Call** (qua Gateway `http://localhost:5001`):
```
POST /identity/auth/register
Content-Type: application/json

{
  "email": "tranhoangminhbla123@gmail.com",
  "password": "Test@1234",
  "fullName": "Tran Hoang Minh",
  "phoneNumber": "0901234567"
}
```

**Validate kỳ vọng**:
- ✅ Response: `200 OK` — `"Đăng ký thành công. Vui lòng kiểm tra email để nhập mã OTP xác nhận."`
- ✅ Email OTP được gửi đến inbox (kiểm tra Resend email)
- ✅ User status = `Pending`, `is_activated = false`

**Validate lỗi**:
- ❌ Register trùng email → Error rõ ràng
- ❌ Password < 8 ký tự → Validation error
- ❌ Email format sai → Validation error
- ❌ Thiếu Email/Password → Required field error

---

### 1.2 Verify Email (OTP)

```
POST /identity/auth/verify-email
{
  "email": "tranhoangminhbla123@gmail.com",
  "otpCode": "<OTP từ email>"
}
```

**Validate**:
- ✅ `200 OK` — `"Xác nhận email thành công"`
- ✅ User status → `Active`, `is_activated = true`
- ❌ OTP sai → Error rõ ràng
- ❌ OTP hết hạn → Error

---

### 1.3 Resend OTP

```
POST /identity/auth/resend-otp
{
  "email": "minhtran0903tumi@gmail.com"
}
```

**Validate**:
- ✅ `200 OK` — OTP mới gửi đến email
- ✅ Kiểm tra inbox `minhtran0903tumi@gmail.com` nhận được OTP mới
- ❌ Email không tồn tại → Vẫn trả OK (security, không leak info)

---

### 1.4 Login

```
POST /identity/auth/login
{
  "email": "tranhoangminhbla123@gmail.com",
  "password": "Test@1234"
}
```

**Validate**:
- ✅ `200 OK` — trả về `accessToken`, `expiresAt`
- ❌ Sai password → Error rõ ràng
- ❌ Email chưa verify → Error
- ❌ Email không tồn tại → Error

---

### 1.5 Me (JWT Test)

```
GET /identity/auth/me
Authorization: Bearer <token>
```

**Validate**:
- ✅ `200 OK` — trả claims (NameIdentifier, Email, Role, v.v.)
- ❌ Không có token → `401 Unauthorized`
- ❌ Token hết hạn → `401`

---

### 1.6 Change Password

```
POST /identity/auth/change-password
Authorization: Bearer <token>
{
  "currentPassword": "Test@1234",
  "newPassword": "NewPass@5678"
}
```

**Validate**:
- ✅ `200 OK` — `"Password changed successfully"`
- ✅ Login lại bằng password mới thành công
- ❌ `currentPassword` sai → Error

---

### 1.7 Forgot Password

```
POST /identity/auth/forgot-password
{
  "email": "tuanminh.tumi0903@gmail.com"
}
```

**Validate**:
- ✅ `200 OK` — Email reset code gửi đến inbox
- ✅ Kiểm tra inbox nhận được mã reset

---

### 1.8 Reset Password

```
POST /identity/auth/reset-password
{
  "email": "tuanminh.tumi0903@gmail.com",
  "resetCode": "<code từ email>",
  "newPassword": "Reset@9999"
}
```

**Validate**:
- ✅ `200 OK` — `"Đặt lại mật khẩu thành công"`
- ✅ Login bằng password mới
- ❌ Sai reset code → Error

---

### 1.9 Logout (Token Blacklist → Redis)

```
POST /identity/auth/logout
Authorization: Bearer <token>
```

**Validate**:
- ✅ `200 OK` — `"Logged out successfully"`
- ✅ Dùng lại token cũ → bị reject (`401`)

---

### 1.10 Google Login (External OAuth)

```
POST /identity/auth/external
{
  "provider": "Google",
  "idToken": "<Google ID Token>"
}
```

> ⚠️ **Cần Google ID Token thật**: Lấy từ Google Sign-In SDK hoặc OAuth Playground.

**Validate**:
- ✅ `200 OK` — `accessToken`, `isNewUser`, `email`, `profilePictureUrl`
- ❌ Token không hợp lệ → Error
- ❌ Provider sai (không phải Google/Facebook) → Validation error

---

## Phase 2: SaaS Service — Store & Subscription Flow

### 2.1 Danh sách Plans

```
GET /saas/plans
```
- ✅ Trả về Trial, Basic, Pro, Yearly plans

### 2.2 Tạo Store (Owner tự tạo sau khi register + verify)

- Kiểm tra luồng assign-store hoặc auto-create store

### 2.3 Subscription Flow

- Kiểm tra Trial → Active subscription
- Test payment (SePay webhook)

---

## Phase 3: Sales Service — Products, Orders, Dashboard

### 3.1 Categories CRUD
```
GET /sales/categories
POST /sales/categories
PUT /sales/categories/{id}
DELETE /sales/categories/{id}
```

### 3.2 Products CRUD
```
GET /sales/products
POST /sales/products
PUT /sales/products/{id}
```

### 3.3 Orders Flow
```
POST /sales/orders
GET /sales/orders
GET /sales/orders/{id}
```

### 3.4 Dashboard
```
GET /sales/dashboard
```

### 3.5 Inventory
```
GET /sales/inventory
POST /sales/inventory
```

---

## Phase 4: HR Service — Employees, Tasks, Timekeeping

### 4.1 Employees
```
GET /hr/employees
POST /hr/employees
PUT /hr/employees/{id}
```

### 4.2 Tasks
```
GET /hr/tasks
POST /hr/tasks
PUT /hr/tasks/{id}
```

### 4.3 Timekeeping
```
POST /hr/timekeeping/check-in
POST /hr/timekeeping/check-out
GET /hr/timekeeping
```

---

## Phase 5: CRM Service — Customers, Loyalty, Feedback

### 5.1 Customers
```
GET /crm/customers
POST /crm/customers
```

### 5.2 Loyalty
```
GET /crm/loyalty/rules
POST /crm/loyalty/rules
```

### 5.3 Feedback
```
POST /crm/feedback
GET /crm/feedback
```

---

## Phase 6: SignalR — Real-time Notifications

### 6.1 Kết nối NotificationHub

**Hub URL** (qua Gateway WebSocket):
```
ws://localhost:5001/notifications/hub
```
**Hoặc trực tiếp Identity**:
```
ws://localhost:5297/notifications/hub
```

**Test steps**:
1. Kết nối SignalR client với JWT token → Verify connected log
2. Trigger notification (ví dụ: tạo order, gửi task) → Verify nhận real-time notification
3. Ngắt kết nối → Verify disconnected log

### 6.2 Notification CRUD

```
GET /identity/notifications                    — Danh sách thông báo
GET /identity/notifications/unread-count       — Đếm chưa đọc
PUT /identity/notifications/{id}/read          — Đánh dấu đã đọc
PUT /identity/notifications/read-all           — Đánh dấu tất cả đã đọc
```

---

## Phase 7: Error Logs & Validation Checklist

### 7.1 Error Response Format
Mỗi endpoint, kiểm tra error response có:
- HTTP status code đúng (400, 401, 404, 500)
- Message rõ ràng bằng tiếng Việt hoặc English
- Không leak stack trace ra client

### 7.2 Docker Logs
```bash
# Kiểm tra logs sau mỗi phase test
docker logs 360retail-identity-api --tail 100
docker logs 360retail-saas-api --tail 100
docker logs 360retail-sales-api --tail 100
docker logs 360retail-hr-api --tail 100
docker logs 360retail-crm-api --tail 100
```

### 7.3 Redis Monitoring
```bash
docker exec 360retail-redis redis-cli KEYS "*"
# Kiểm tra: blacklisted tokens, cached data
```

---

## Execution Plan — Thứ tự thực hiện

| Step | Action | Công cụ |
|------|--------|---------|
| 1 | Docker Reset & Rebuild | Terminal |
| 2 | Verify containers healthy | Terminal + Logs |
| 3 | Test Auth Flow (Register → Verify → Login → Me) | Browser (Swagger/API) |
| 4 | Test Email (Resend OTP, Forgot Password) | Browser + Email Inbox |
| 5 | Test Google Login | Browser + Google OAuth |
| 6 | Test SaaS Flow | Browser (Swagger) |
| 7 | Test Sales Flow | Browser (Swagger) |
| 8 | Test HR Flow | Browser (Swagger) |
| 9 | Test CRM Flow | Browser (Swagger) |
| 10 | Test SignalR (WebSocket client) | Browser (JS client) |
| 11 | Verify Error Logs | Terminal |
| 12 | Final Report | Walkthrough document |

---

## Tài khoản Test Email

| Email | Dùng cho |
|-------|----------|
| `tranhoangminhbla123@gmail.com` | Register + Verify + Login + Change Password |
| `minhtran0903tumi@gmail.com` | Register + Resend OTP + Verify |
| `tuanminh.tumi0903@gmail.com` | Register + Forgot Password + Reset Password |
| `lanhuong8899z@gmail.com` | Register + Full flow backup |

---

> **⚠️ Lưu ý**: Google Login cần **ID Token thật** từ Google Sign-In SDK. Cần user cung cấp hoặc dùng [OAuth 2.0 Playground](https://developers.google.com/oauthplayground/) để lấy token test.
