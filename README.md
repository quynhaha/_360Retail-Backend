# 360Retail Backend

> Nền tảng quản lý bán lẻ thông minh dành cho cửa hàng vừa & nhỏ tại Việt Nam.

360Retail cung cấp hệ thống **all-in-one** giúp chủ cửa hàng quản lý bán hàng, nhân sự, khách hàng và gói dịch vụ trên cùng một nền tảng, với kiến trúc **Microservices** hiện đại.

---

## 🏗️ Kiến trúc

```
┌──────────────────────────────────────────────────┐
│              API Gateway (:5001)                  │
│           (Ocelot + Rate Limiting)                │
└──────┬───────┬───────┬───────┬───────┬───────────┘
       │       │       │       │       │
  ┌────▼──┐┌───▼──┐┌───▼──┐┌──▼───┐┌──▼───┐
  │Identity││ Saas ││Sales ││  HR  ││ CRM  │
  │ :5297  ││:5031 ││:5091 ││:5280 ││:5169 │
  └────┬───┘└──┬───┘└──┬───┘└──┬───┘└──┬───┘
       │       │       │       │       │
       └───────┴───────┴───────┴───────┘
                       │
              ┌────────▼────────┐
              │  PostgreSQL 16  │
              │    (:5433)      │
              └─────────────────┘
```

### Microservices

| Service | Port | Chức năng |
|---------|------|-----------|
| **API Gateway** | 5001 | Ocelot reverse proxy, routing, rate limiting |
| **Identity** | 5297 | JWT Auth, OAuth Google, User management, Role-based access |
| **SaaS** | 5031 | Store management, Subscription lifecycle, VNPay + SePay payment |
| **Sales** | 5091 | Products, Categories, Orders (POS), Inventory, Dashboard |
| **HR** | 5280 | Employees, Timekeeping (GPS), Task management |
| **CRM** | 5169 | Customer management, Loyalty points (auto-earn & rank), Feedback |

### Service Communication

| Luồng | Phương thức | Mô tả |
|-------|------------|-------|
| Identity → SaaS | HTTP (Internal) | Tạo trial store, kiểm tra subscription |
| SaaS → Identity | HTTP (Internal) | Activate subscription, assign store |
| Sales → CRM | HTTP (Internal) | Auto-earn loyalty points after order |
| Identity → HR | HTTP (Internal) | Tạo employee sau invite |
| HR → Identity | HTTP (Internal) | Sync employee roles |

---

## 🛠️ Tech Stack

- **Runtime:** .NET 8 / ASP.NET Core
- **Database:** PostgreSQL 16
- **Cache:** Redis (dashboard, token blacklist)
- **ORM:** Entity Framework Core
- **Auth:** JWT + Google OAuth 2.0
- **Payment:** VNPay API v2.1.0 + SePay (QR chuyển khoản)
- **AI:** Google Gemini API (chatbot)
- **Storage:** Cloudinary (product images, selfie check-in)
- **Real-time:** SignalR WebSocket (notifications)
- **Gateway:** Ocelot (routing, rate limiting, Swagger aggregation)
- **Container:** Docker + Docker Compose
- **Email:** Resend API
- **CI/CD:** GitHub Actions

---

## 🚀 Khởi động dự án

### Yêu cầu

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (bật WSL2 trên Windows)
- Git

### 1. Clone & cấu hình

```bash
git clone <repository-url>
cd _360Retail-Backend

# Copy file cấu hình
cp .env.example .env
```

Chỉnh sửa file `.env` với các giá trị thực:

```env
# Google OAuth
OAUTH_GOOGLE_CLIENT_ID=your_google_client_id
OAUTH_GOOGLE_CLIENT_SECRET=your_google_client_secret

# Cloudinary (upload ảnh sản phẩm + selfie)
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret

# Resend (gửi email)
RESEND_API_KEY=your_resend_api_key
RESEND_FROM_EMAIL=noreply@yourdomain.com

# Gemini AI (chatbot)
GEMINI_API_KEY=your_gemini_api_key
```

### 2. Khởi động toàn bộ

```bash
docker compose up --build
```

> ⏳ Lần chạy đầu tiên mất ~3-5 phút để build images.

### 3. Truy cập

| Service | URL |
|---------|-----|
| API Gateway (Swagger) | http://localhost:5001 |
| Identity Swagger | http://localhost:5297/swagger |
| SaaS Swagger | http://localhost:5031/swagger |
| Sales Swagger | http://localhost:5091/swagger |
| HR Swagger | http://localhost:5280/swagger |
| CRM Swagger | http://localhost:5169/swagger |
| PgAdmin | http://localhost:5050 |

**PgAdmin credentials:** `admin@360retail.com` / `admin`

### 4. Tài khoản mặc định

| Email | Password (SHA256) | Role |
|-------|-------------------|------|
| `admin` | `1` | SuperAdmin |

---

## 📦 Database Schema

Hệ thống sử dụng **1 Database chung** (`360RetailDB`) với các **schema riêng biệt** cho từng module:

| Schema | Module | Bảng chính |
|--------|--------|-----------|
| `identity` | Authentication | `app_users`, `app_roles`, `user_roles`, `user_store_access` |
| `saas` | SaaS Platform | `stores`, `service_plans`, `subscriptions`, `payments`, `plan_reviews` |
| `sales` | Bán hàng | `products`, `categories`, `orders`, `order_items`, `product_variants`, `inventory_tickets` |
| `hr` | Nhân sự | `employees`, `timekeepings`, `tasks` |
| `crm` | Khách hàng | `customers`, `loyalty_rules`, `loyalty_transactions`, `customer_feedbacks` |

---

## ✨ Tính năng chính

### 🔐 Authentication & Authorization
- JWT Token + Refresh token flow
- Google OAuth 2.0 login
- Role-based: SuperAdmin, StoreOwner, Manager, Staff, Customer
- Multi-store access control
- Forgot/Reset password (6-digit code via email)

### 🏪 SaaS & Subscription
- Trial 7 ngày → Paid plans (Basic, Pro, Yearly)
- **Bảng giá:** Basic 199k/th, Pro 499k/th, Yearly 4.99M/năm (tiết kiệm 17%)
- Thanh toán qua **VNPay** (redirect) và **SePay** (QR chuyển khoản)
- Auto-activate sau payment thành công
- **Feature Gate:** `[RequiresFeature]` — chặn tính năng theo gói subscription
- Subscription expiry notifications (email)
- Plan reviews & ratings
- **AI Chatbot:** Hybrid FAQ + Google Gemini hỗ trợ khách hàng

### 🛒 Sales (POS)
- Quản lý sản phẩm + biến thể (size, color, SKU)
- Upload ảnh sản phẩm (Cloudinary)
- Tạo đơn hàng POS với auto-deduct stock
- Cancel order + restore stock tự động
- Inventory management (import/export tickets)
- Dashboard analytics (revenue, top products, order status)
- **Export Excel** (báo cáo doanh thu, top sản phẩm)
- Low stock email alerts
- **Redis caching** (dashboard, products)

### 👥 HR
- Quản lý nhân viên + Avatar upload
- Chấm công GPS + **upload selfie** (Cloudinary)
- Giao việc với deadline + priority + status tracking
- **Export Excel** báo cáo chấm công

### 💎 CRM & Loyalty
- Quản lý khách hàng (phone unique per store)
- **Auto-earn** loyalty points khi tạo đơn hàng
- **Auto-rank** upgrade: Bronze → Silver → Gold → Platinum
- 3 loại rule: % order value, fixed/order, per quantity
- Đổi điểm (redeem) + lịch sử giao dịch
- Customer feedback (public QR + staff-entered)

### 💰 Phân quyền tính năng theo gói

| Tính năng | Trial | Basic | Pro | Yearly |
|-----------|:-----:|:-----:|:---:|:------:|
| Bán hàng cơ bản | ✅ | ✅ | ✅ | ✅ |
| Dashboard & Báo cáo | ❌ | ✅ | ✅ | ✅ |
| Tasks & Giao việc | ❌ | ✅ | ✅ | ✅ |
| Phiếu kho nâng cao | ❌ | ✅ | ✅ | ✅ |
| Mời nhân viên | ❌ | ✅ | ✅ | ✅ |
| Thông báo realtime | ❌ | ✅ | ✅ | ✅ |
| Chấm công GPS | ❌ | ❌ | ✅ | ✅ |
| CRM & Loyalty | ❌ | ❌ | ✅ | ✅ |
| Export Excel | ❌ | ❌ | ✅ | ✅ |
| Multi-store | ❌ | ❌ | ✅ | ✅ |
| Max nhân viên | 3 | 10 | 20 | 50 |
| Max sản phẩm | 50 | 200 | ∞ | ∞ |

---

## 📁 Cấu trúc thư mục

```
_360Retail-Backend/
├── src/
│   ├── ApiGateway/              # Ocelot Gateway + Rate Limiting
│   ├── Services/
│   │   ├── Identity/            # Auth, JWT, OAuth, Users
│   │   ├── Saas/                # Stores, Subscriptions, VNPay + SePay
│   │   ├── Sales/               # Products, Orders, POS, Dashboard
│   │   ├── HR/                  # Employees, Timekeeping, Tasks
│   │   └── CRM/                 # Customers, Loyalty, Feedback
│   └── Shared/                  # Common middleware, email, filters
├── tests/                       # Unit & Integration tests
│   └── Services/
│       ├── Identity/            # Auth tests (10)
│       ├── Sales/               # Order tests (6)
│       ├── Saas/                # Subscription tests (11)
│       └── CRM/                 # Loyalty tests (2)
├── .github/workflows/           # CI/CD pipelines
├── init-db/                     # SQL init scripts (auto-run)
├── docker-compose.yml
└── .env.example
```

---

## 🧪 Chạy Tests

```bash
# Chạy toàn bộ tests (29 tests)
dotnet test 360Retail.sln --verbosity normal

# Chạy test riêng từng service
dotnet test tests/Services/Identity/Identity.Auth.Tests
dotnet test tests/Services/Sales/Sales.Orders.Tests
dotnet test tests/Services/Saas/Saas.Subscription.Tests
dotnet test tests/Services/CRM/CRM.Loyalty.Tests
```

> **CI/CD:** Tests tự động chạy khi push/PR vào `main` hoặc `develop` qua GitHub Actions.

---

## 🛡️ Rate Limiting

API Gateway (Ocelot) có rate limiting cho tất cả routes:

| Route | Limit | Mục đích |
|-------|-------|----------|
| `/identity/*` | 30 req/s | Chống brute-force login |
| `/saas/*` | 100 req/s | CRUD operations |
| `/sales/*` | 100 req/s | POS operations |
| `/hr/*` | 100 req/s | HR operations |
| `/crm/*` | 100 req/s | CRM operations |

Khi vượt giới hạn → HTTP `429 Too Many Requests`

---

## 👥 Team

**EXE101 — FPT University**

---

*Last updated: 01/03/2026*
