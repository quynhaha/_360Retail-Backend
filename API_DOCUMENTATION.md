# 360Retail - Hướng Dẫn Sử Dụng API

> **Tài liệu hướng dẫn thực hành cho Frontend Team**  
> Cập nhật: 26/02/2026

---

## 🚀 Khởi động Backend

```bash
# Bước 1: Clone repo và vào thư mục
cd _360Retail-Backend

# Bước 2: Chạy Docker (chỉ cần lệnh này)
docker-compose up -d

# Bước 3: Mở Swagger
# Truy cập: http://localhost:5001/swagger
```

> **Rebuild khi code mới**: `docker-compose up -d --build`  
> **Reset database**: `docker-compose down -v && docker-compose up -d`

---

## 🎯 Swagger UI

**URL chính**: http://localhost:5001/swagger

Swagger gộp tất cả APIs từ các services. Prefix route:
- `/identity/*` → Identity Service
- `/saas/*` → SaaS Service  
- `/sales/*` → Sales Service
- `/hr/*` → HR Service
- `/crm/*` → CRM Service

---

# 📋 LUỒNG NGHIỆP VỤ CHI TIẾT

## Luồng 1: Đăng ký & Dùng thử (Trial)

### Bước 1.1: Đăng ký tài khoản

```
POST /identity/auth/register
```
```json
{
  "email": "owner@example.com",
  "password": "Password123!"
}
```
→ Response: `{ "message": "Register successful" }`

---

### Bước 1.2: Đăng nhập

```
POST /identity/auth/login
```
```json
{
  "email": "owner@example.com",
  "password": "Password123!"
}
```
→ Response:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1...",
  "expiresAt": "2026-01-22T19:00:00Z",
  "mustChangePassword": false
}
```

⚠️ **Quan trọng**: Copy token này, click nút **Authorize** ở góc trên phải Swagger, dán vào ô `Value`: `Bearer eyJhbGciOiJIUzI1...`

---

### Bước 1.3: Bắt đầu Trial (7 ngày miễn phí)

```
POST /identity/subscription/start-trial
```
→ Response:
```json
{
  "storeId": "e97072b9-...",
  "storeName": "Store của owner@example.com",
  "trialEndDate": "2026-03-05T14:11:31Z",
  "daysRemaining": 7
}
```

⚠️ **Login lại** để lấy token mới với `store_id` và role `StoreOwner`!

---

### Bước 1.4: Kiểm tra claims trong JWT

```
GET /identity/auth/me
```
→ Response: Danh sách claims trong token (store_id, role, status, trial_expired...)

---

## Luồng 2: Mua gói dịch vụ (Trial → Paid)

### Bước 2.1: Xem danh sách gói

```
GET /saas/subscriptions/plans
```

### Bước 2.2: Mua gói

```
POST /saas/subscriptions/purchase
```
```json
{
  "planId": "xxx"
}
```
→ Response chứa `paymentUrl` để redirect thanh toán

### Bước 2.3: Thanh toán VNPay Sandbox
- Ngân hàng: NCB
- Số thẻ: 9704198526191432198
- Tên: NGUYEN VAN A
- Ngày: 07/15 | OTP: 123456

### Bước 2.4: Refresh Token sau thanh toán
```
POST /identity/auth/refresh-access
```

---

## Luồng 3: Tạo Store mới (cho Paid Owner)

```
POST /saas/stores
```
```json
{
  "storeName": "Chi nhánh 2",
  "address": "456 Lê Văn Việt",
  "phone": "0909123456",
  "planId": "xxx"
}
```

Sau khi thanh toán:
```
POST /identity/auth/refresh-access?storeId=new-store-id
```

---

## Luồng 4: Quản lý sản phẩm

### 4.1: Tạo Category

```
POST /sales/categories
```
```json
{
  "categoryName": "Điện thoại",
  "isActive": true
}
```

### 4.2: Tạo Product (multipart/form-data)

```
POST /sales/products
Content-Type: multipart/form-data
```
| Field | Value |
|-------|-------|
| productName | iPhone 15 Pro |
| categoryId | category-id |
| barCode | IP15PRO001 |
| price | 32990000 |
| costPrice | 28000000 |
| stockQuantity | 50 |

### 4.3: Tạo Product có Variants

```
POST /sales/products
Content-Type: multipart/form-data
```
| Field | Value |
|-------|-------|
| productName | Áo thun Polo |
| categoryId | category-id |
| hasVariants | true |
| variants[0].sku | POLO-M-DEN |
| variants[0].size | M |
| variants[0].color | Đen |
| variants[0].priceOverride | 350000 |
| variants[0].stockQuantity | 20 |

### 4.4: Xem danh sách sản phẩm

```
GET /sales/products
GET /sales/products/{id}
```

---

## Luồng 5: Tạo đơn hàng (POS)

```
POST /sales/orders
```
```json
{
  "customerId": null,
  "paymentMethod": "Cash",
  "discountAmount": 0,
  "items": [
    {
      "productId": "product-id",
      "quantity": 2,
      "productVariantId": null
    }
  ]
}
```

> **Lưu ý**: Đơn hàng POS tự động trừ stock khi tạo. Nếu product có Variants, phải truyền `productVariantId`.

### Xem đơn hàng

```
GET /sales/orders
GET /sales/orders/{id}
```

---

## Luồng 6: Quản lý nhân viên

### 6.1: Mời nhân viên

```
POST /identity/staff/invite
```
```json
{
  "email": "staff@example.com",
  "storeId": "store-id",
  "roleInStore": "Staff",
  "fullName": "Nguyễn Văn A",
  "position": "Nhân viên bán hàng",
  "baseSalary": 7000000
}
```
→ Email được gửi với password tạm thời

### 6.2: Xem & giao việc

```
GET /hr/employees
POST /hr/tasks
```
```json
{
  "title": "Kiểm kê hàng tồn kho",
  "description": "Kiểm tra số lượng hàng trong kho",
  "assignedToEmployeeId": "employee-id",
  "dueDate": "2026-01-25T17:00:00Z",
  "priority": "High"
}
```

---

## Luồng 7: Quản lý tồn kho (Nhập/Xuất kho)

> Roles: **StoreOwner, Manager**

### 7.1: Tạo phiếu nhập kho

```
POST /sales/inventory
```
```json
{
  "type": "Import",
  "note": "Nhập hàng đợt 1",
  "items": [
    {
      "productId": "product-id-1",
      "quantity": 100,
      "note": "iPhone nhập thêm"
    },
    {
      "productId": "product-id-2",
      "quantity": 50,
      "productVariantId": null,
      "note": "Samsung nhập thêm"
    }
  ]
}
```
→ Response:
```json
{
  "success": true,
  "message": "Inventory ticket created successfully",
  "data": "ticket-id"
}
```

> Phiếu mới tạo có status = **Draft**, stock **chưa thay đổi**

---

### 7.2: Xem danh sách phiếu

```
GET /sales/inventory
GET /sales/inventory?type=Import&status=Draft&page=1&pageSize=20
```
→ Hỗ trợ filter: `type` (Import/Export), `status` (Draft/Confirmed/Cancelled), phân trang

---

### 7.3: Xem chi tiết phiếu

```
GET /sales/inventory/{ticketId}
```
→ Trả về ticket + danh sách items kèm tên sản phẩm

---

### 7.4: Xác nhận phiếu (cập nhật stock)

```
PUT /sales/inventory/{ticketId}/confirm
```
→ **Khi confirm:**
- **Import**: stock +quantity cho mỗi item
- **Export**: stock -quantity (kiểm tra đủ hàng trước)

---

### 7.5: Hủy phiếu

```
PUT /sales/inventory/{ticketId}/cancel
```
→ Chỉ hủy được phiếu **Draft**, stock không thay đổi

---

### 7.6: Xóa phiếu (soft delete)

```
DELETE /sales/inventory/{ticketId}
```
→ Chỉ xóa được phiếu **Draft/Cancelled** (phiếu Confirmed không xóa được)

---

### 7.7: Tạo phiếu xuất kho

```
POST /sales/inventory
```
```json
{
  "type": "Export",
  "note": "Xuất hàng lỗi",
  "items": [
    {
      "productId": "product-id",
      "quantity": 5,
      "note": "Hàng lỗi trả NCC"
    }
  ]
}
```

---

## Luồng 8: CRM - Khách hàng & Loyalty

### 8.1: Quản lý khách hàng

```
POST /crm/customers
```
```json
{
  "fullName": "Nguyễn Văn A",
  "phoneNumber": "0901234567"
}
```

```
GET /crm/customers
GET /crm/customers/{id}
```

---

### 8.2: Loyalty Rules (cấu hình tích điểm)

```
POST /crm/loyalty-rules
```
```json
{
  "name": "Tích 1 điểm/10,000đ",
  "type": 0,
  "earningRate": 1,
  "minSpend": 10000,
  "status": 0
}
```

```
GET /crm/loyalty-rules
PUT /crm/loyalty-rules/{id}
DELETE /crm/loyalty-rules/{id}
```

---

### 8.3: Xem loyalty khách hàng

```
GET /crm/customers/{customerId}/loyalty-summary
```
→ Response:
```json
{
  "data": {
    "customerId": "xxx",
    "customerName": "Nguyễn Văn A",
    "totalPoints": 150,
    "rank": "Bronze"
  }
}
```

```
GET /crm/customers/{customerId}/loyalty-transactions?page=1&pageSize=20
```

---

### 8.4: Đổi điểm

```
POST /crm/customers/{customerId}/redeem
```
```json
{
  "customerId": "xxx",
  "points": 100,
  "description": "Đổi điểm giảm giá"
}
```

---

## Luồng 9: Admin Dashboard (Thống kê)

> Roles: **StoreOwner, Manager**  
> Tất cả endpoint hỗ trợ filter: `?from=2026-01-01&to=2026-02-26`  
> Mặc định: 30 ngày gần nhất

### 9.1: Tổng quan (KPI Cards)

```
GET /sales/dashboard/overview?from=2026-01-01&to=2026-02-28
```
→ Response:
```json
{
  "success": true,
  "data": {
    "totalRevenue": 91970000,
    "totalOrders": 15,
    "totalCustomers": 5,
    "totalProducts": 20,
    "avgOrderValue": 6131333,
    "revenueGrowth": 25.5,
    "orderGrowth": 12.3
  }
}
```
> `revenueGrowth` / `orderGrowth`: % so sánh với cùng kỳ trước

---

### 9.2: Biểu đồ doanh thu (Line/Bar Chart)

```
GET /sales/dashboard/revenue-chart?from=2026-02-01&to=2026-02-28&groupBy=day
```

**groupBy options**: `day` | `week` | `month`

→ Response:
```json
{
  "success": true,
  "data": {
    "dataPoints": [
      { "label": "2026-02-01", "revenue": 5000000, "orderCount": 3 },
      { "label": "2026-02-02", "revenue": 8000000, "orderCount": 5 },
      { "label": "2026-02-03", "revenue": 0, "orderCount": 0 }
    ],
    "totalRevenue": 13000000,
    "groupBy": "day"
  }
}
```

---

### 9.3: Top sản phẩm bán chạy (Horizontal Bar)

```
GET /sales/dashboard/top-products?from=2026-01-01&to=2026-02-28&top=10
```
→ Response:
```json
{
  "success": true,
  "data": [
    { "productId": "xxx", "productName": "iPhone 16 Pro", "quantitySold": 25, "revenue": 65980000 },
    { "productId": "yyy", "productName": "Samsung S25", "quantitySold": 12, "revenue": 25990000 }
  ]
}
```

---

### 9.4: Phân bổ trạng thái đơn hàng (Pie Chart)

```
GET /sales/dashboard/order-status?from=2026-01-01&to=2026-02-28
```
→ Response:
```json
{
  "success": true,
  "data": {
    "statuses": [
      { "status": "Completed", "count": 10, "percentage": 66.7 },
      { "status": "Processing", "count": 3, "percentage": 20.0 },
      { "status": "Cancelled", "count": 2, "percentage": 13.3 }
    ],
    "totalOrders": 15
  }
}
```

---

### 9.5: Tổng quan tồn kho (Table + Alerts)

```
GET /sales/dashboard/inventory-summary
```
→ Response:
```json
{
  "success": true,
  "data": {
    "totalProducts": 20,
    "inStockCount": 15,
    "lowStockCount": 3,
    "outOfStockCount": 2,
    "lowStockProducts": [
      { "productId": "xxx", "productName": "Ốp lưng iPhone", "stockQuantity": 3, "sku": "OL001" },
      { "productId": "yyy", "productName": "Cáp sạc", "stockQuantity": 0, "sku": "CS001" }
    ]
  }
}
```

> **Low stock**: stock ≤ 10 | **Out of stock**: stock = 0

---

### 9.6: Hoạt động gần đây (Timeline)

```
GET /sales/dashboard/recent-activity?limit=20
```
→ Response:
```json
{
  "success": true,
  "data": {
    "activities": [
      {
        "type": "Order",
        "code": "ORD-260226-9311",
        "description": "Đơn hàng ORD-260226-9311 - Cash",
        "amount": 91970000,
        "status": "Completed",
        "createdAt": "2026-02-26T14:18:48Z"
      },
      {
        "type": "Import",
        "code": "IMP-260226-6359",
        "description": "Phiếu Import IMP-260226-6359 - 150 sản phẩm",
        "amount": null,
        "status": "Confirmed",
        "createdAt": "2026-02-26T14:18:48Z"
      }
    ]
  }
}
```

---

# ⚠️ Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Giải pháp |
|-----|-------------|-----------|
| **401 Unauthorized** | Token hết hạn (60 phút) hoặc chưa set | Login lại |
| **403 TrialExpired** | Trial 7 ngày đã hết | Mua gói |
| **403 SubscriptionExpired** | Subscription hết hạn | Gia hạn |
| **400 "Vui lòng chọn gói"** | Owner paid tạo store không có planId | Thêm planId |
| **400 "Insufficient stock"** | Export vượt quá tồn kho | Kiểm tra stock |
| **400 "Only Draft tickets"** | Confirm/Cancel phiếu không phải Draft | Chỉ dùng với Draft |

---

# 🧪 Test nhanh bằng cURL/PowerShell

### Login lấy token

```powershell
$body = '{"email":"owner@example.com","password":"Password123!"}'
$r = Invoke-WebRequest -Uri 'http://localhost:5001/identity/auth/login' -Method Post -ContentType 'application/json' -Body $body -UseBasicParsing
$token = ($r.Content | ConvertFrom-Json).accessToken
$headers = @{Authorization = "Bearer $token"}
```

### Test Dashboard

```powershell
# Overview
Invoke-WebRequest -Uri "http://localhost:5001/sales/dashboard/overview" -Headers $headers -UseBasicParsing | Select -Expand Content

# Revenue chart theo tuần
Invoke-WebRequest -Uri "http://localhost:5001/sales/dashboard/revenue-chart?groupBy=week" -Headers $headers -UseBasicParsing | Select -Expand Content

# Top 5 sản phẩm
Invoke-WebRequest -Uri "http://localhost:5001/sales/dashboard/top-products?top=5" -Headers $headers -UseBasicParsing | Select -Expand Content
```

### Test Inventory

```powershell
# Tạo phiếu nhập
$invBody = '{"type":"Import","note":"Test","items":[{"productId":"PRODUCT_ID","quantity":10}]}'
Invoke-WebRequest -Uri "http://localhost:5001/sales/inventory" -Method Post -Headers $headers -ContentType 'application/json' -Body $invBody -UseBasicParsing

# Confirm phiếu (thay TICKET_ID)
Invoke-WebRequest -Uri "http://localhost:5001/sales/inventory/TICKET_ID/confirm" -Method Put -Headers $headers -UseBasicParsing
```

---

# 🔧 Database Access (pgAdmin)

- **URL**: http://localhost:5050
- **Login**: admin@360retail.com / admin
- **Kết nối DB**:
  - Host: `360retail-db`
  - Port: `5432`
  - Database: `360RetailDB`
  - User/Pass: `postgres` / `12345`

---

Chúc các bạn code vui vẻ! 🚀
