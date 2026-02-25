# 360Retail Backend

> Nền tảng quản lý bán lẻ thông minh dành cho cửa hàng vừa & nhỏ tại Việt Nam.

360Retail cung cấp hệ thống **all-in-one** giúp chủ cửa hàng quản lý bán hàng, nhân sự, khách hàng và gói dịch vụ trên cùng một nền tảng, với kiến trúc **Microservices** hiện đại.

---

## 🏗️ Kiến trúc

```
┌──────────────────────────────────────────────────┐
│                  API Gateway (:5001)              │
│                 (YARP Reverse Proxy)              │
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
| **API Gateway** | 5001 | YARP reverse proxy, routing |
| **Identity** | 5297 | JWT Auth, OAuth Google, User management, Role-based access |
| **SaaS** | 5031 | Store management, Subscription lifecycle, VNPay payment |
| **Sales** | 5091 | Products, Categories, Orders (POS), Stock management |
| **HR** | 5280 | Employees, Timekeeping, Task management |
| **CRM** | 5169 | Customer management, Loyalty points (auto-earn & rank) |

### Service Communication

| Luồng | Phương thức | Mô tả |
|-------|------------|-------|
| Saas → Identity | HTTP (Internal) | Activate subscription, Assign store |
| Sales → CRM | HTTP (Internal) | Auto-earn loyalty points after order |
| HR → Identity | HTTP (Internal) | Sync employee roles |

---

## 🛠️ Tech Stack

- **Runtime:** .NET 8 / ASP.NET Core
- **Database:** PostgreSQL 16
- **ORM:** Entity Framework Core
- **Auth:** JWT + Google OAuth 2.0
- **Payment:** VNPay API v2.1.0
- **Storage:** Cloudinary (product images)
- **Gateway:** YARP Reverse Proxy
- **Container:** Docker + Docker Compose
- **Email:** Resend API

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

# Cloudinary (upload ảnh sản phẩm)
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret

# Resend (gửi email)
RESEND_API_KEY=your_resend_api_key
RESEND_FROM_EMAIL=noreply@yourdomain.com
```

### 2. Khởi động toàn bộ

```bash
docker compose up --build
```

> ⏳ Lần chạy đầu tiên mất ~3-5 phút để build images.

### 3. Truy cập

| Service | URL |
|---------|-----|
| API Gateway | http://localhost:5001 |
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
| `saas` | SaaS Platform | `stores`, `service_plans`, `subscriptions`, `payments` |
| `sales` | Bán hàng | `products`, `categories`, `orders`, `order_items`, `product_variants` |
| `hr` | Nhân sự | `employees`, `timekeepings`, `tasks` |
| `crm` | Khách hàng | `customers`, `loyalty_rules`, `loyalty_transactions` |

---

## ✨ Tính năng chính

### 🔐 Authentication & Authorization
- JWT Token + Refresh token flow
- Google OAuth 2.0 login
- Role-based: SuperAdmin, StoreOwner, Manager, Staff, Customer
- Multi-store access control

### 🏪 SaaS & Subscription
- Trial 7 ngày → Paid plans (Basic, Pro, Yearly)
- Thanh toán VNPay
- Auto-activate sau payment thành công

### 🛒 Sales (POS)
- Quản lý sản phẩm + biến thể (size, color, SKU)
- Tạo đơn hàng POS với auto-deduct stock
- Cancel order + restore stock tự động
- Phân quyền: Customer chỉ xem đơn của mình

### 👥 HR
- Quản lý nhân viên + Face data
- Chấm công GPS + ảnh check-in
- Giao việc với deadline + priority

### 💎 CRM & Loyalty
- Quản lý khách hàng (phone unique per store)
- **Auto-earn** loyalty points khi tạo đơn hàng
- **Auto-rank** upgrade: Bronze → Silver → Gold → Platinum
- 3 loại rule: % order value, fixed/order, per quantity
- Đổi điểm (redeem) + lịch sử giao dịch

---

## 📁 Cấu trúc thư mục

```
_360Retail-Backend/
├── src/
│   ├── ApiGateway/              # YARP Reverse Proxy
│   ├── Services/
│   │   ├── Identity/            # Auth, JWT, OAuth, Users
│   │   ├── Saas/                # Stores, Subscriptions, VNPay
│   │   ├── Sales/               # Products, Orders, POS
│   │   ├── HR/                  # Employees, Timekeeping, Tasks
│   │   └── CRM/                 # Customers, Loyalty, Points
│   └── Shared/                  # Common middleware, utilities
├── tests/                       # Unit & Integration tests
├── init-db/                     # SQL init scripts (auto-run)
├── docker-compose.yml
└── .env.example
```

---

## 🧪 Chạy Tests

```bash
cd tests/Services/CRM/CRM.Loyalty.Tests
dotnet test --verbosity normal
```

> **Lưu ý:** Tests sử dụng [Testcontainers](https://dotnet.testcontainers.org/) nên cần Docker đang chạy.

---

## 👥 Team

**EXE101 — FPT University**

---

*Last updated: 25/02/2026*
