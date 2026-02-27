# 360Retail - Hướng Dẫn Sử Dụng API

> **Tài liệu hướng dẫn thực hành cho Frontend Team**  
> Cập nhật: 27/02/2026  
> **Swagger UI**: http://localhost:5001/swagger (khi Docker đang chạy)

---

## 📋 Bảng tổng hợp tất cả Endpoints

> 💡 Click vào tên luồng ở cột **Luồng** để xem chi tiết request/response.
> **Auth**: ❌ = Public | ✅ = Bearer token | Role cụ thể = chỉ role đó mới gọi được.

### 🔐 Identity Service (`/identity`)

| # | Method | Endpoint | Auth | Mô tả |
|:-:|:------:|----------|:----:|-------|
| 1 | POST | `/identity/auth/register` | ❌ | Đăng ký tài khoản mới |
| 2 | POST | `/identity/auth/login` | ❌ | Đăng nhập → lấy accessToken |
| 3 | POST | `/identity/auth/external` | ❌ | Đăng nhập Google/Facebook |
| 4 | GET | `/identity/auth/me` | ✅ | Thông tin user hiện tại |
| 5 | POST | `/identity/auth/change-password` | ✅ | Đổi mật khẩu |
| 6 | POST | `/identity/auth/assign-store` | ✅ | Gán user vào store |
| 7 | POST | `/identity/auth/refresh-access` | ✅ | Refresh token (đổi store context) |
| 8 | POST | `/identity/subscription/start-trial` | ✅ | Bắt đầu Trial 7 ngày |
| 9 | GET | `/identity/subscription/status` | ✅ | Trạng thái subscription |
| 10 | POST | `/identity/staff/invite` | ✅ Owner | Mời nhân viên vào store |
| 11 | GET | `/identity/user-stores/stores-my` | ✅ | Danh sách store của tôi |
| 12 | GET | `/identity/admin/users` | SuperAdmin | Danh sách tất cả users |
| 13 | GET | `/identity/admin/users/{id}` | SuperAdmin | Chi tiết 1 user |
| 14 | POST | `/identity/admin/users` | SuperAdmin | Tạo user (admin) |
| 15 | PUT | `/identity/admin/users/{id}` | SuperAdmin | Sửa user |
| 16 | DELETE | `/identity/admin/users/{id}` | SuperAdmin | Xóa user |

### 🏪 SaaS Service (`/saas`)

| # | Method | Endpoint | Auth | Mô tả |
|:-:|:------:|----------|:----:|-------|
| 17 | GET | `/saas/subscriptions/plans` | ❌ | DS gói dịch vụ (Trial/Basic/Pro/Yearly) |
| 18 | GET | `/saas/subscriptions/my` | ✅ | Subscription của tôi |
| 19 | GET | `/saas/subscriptions/store/{storeId}/status` | ✅ | Status subscription 1 store |
| 20 | POST | `/saas/subscriptions/purchase` | ✅ | Mua gói → tạo payment |
| 21 | POST | `/saas/stores` | ✅ Owner | Tạo store mới (paid) |
| 22 | GET | `/saas/stores` | ✅ | DS tất cả stores |
| 23 | GET | `/saas/stores/{id}` | ✅ | Chi tiết 1 store |
| 24 | GET | `/saas/stores/my-owned-stores` | ✅ Owner | DS store tôi sở hữu |
| 25 | GET | `/saas/stores/my-store` | ✅ Staff | Store tôi đang làm việc |
| 26 | PUT | `/saas/stores/{id}` | ✅ Owner | Cập nhật store (tên, GPS, phone) |
| 27 | DELETE | `/saas/stores/{id}` | ✅ Owner | Xóa store (soft delete) |
| 28 | GET | `/saas/payments/initiate` | ✅ | Tạo link thanh toán VNPay |
| 29 | GET | `/saas/payments/vnpay-return` | ❌ | VNPay callback |
| 30 | POST | `/saas/plan-reviews` | ✅ Owner | Tạo đánh giá gói đã mua |
| 31 | GET | `/saas/plan-reviews/me/{planId}` | ✅ | Xem review của tôi |
| 32 | GET | `/saas/plan-reviews/plan/{planId}` | ❌ | DS reviews 1 gói (public) |
| 33 | GET | `/saas/plan-reviews/plan/{planId}/summary` | ❌ | Tổng hợp rating 1 gói |
| 34 | GET | `/saas/plan-reviews/summary` | ❌ | Tổng hợp tất cả gói (trang Pricing) |
| 35 | GET | `/saas/plan-reviews/admin` | SuperAdmin | DS reviews toàn hệ thống |
| 36 | GET | `/saas/plan-reviews/admin/dashboard` | SuperAdmin | Dashboard thống kê reviews |
| 37 | DELETE | `/saas/plan-reviews/admin/{reviewId}` | SuperAdmin | Xóa review spam |

### 🛒 Sales Service (`/sales`)

| # | Method | Endpoint | Auth | Mô tả |
|:-:|:------:|----------|:----:|-------|
| 38 | GET | `/sales/categories` | ✅ | DS danh mục sản phẩm |
| 39 | POST | `/sales/categories` | ✅ Manager+ | Tạo danh mục |
| 40 | PUT | `/sales/categories/{id}` | ✅ Manager+ | Sửa danh mục |
| 41 | DELETE | `/sales/categories/{id}` | ✅ Manager+ | Xóa danh mục |
| 42 | GET | `/sales/products` | ✅ | DS sản phẩm |
| 43 | GET | `/sales/products/{id}` | ✅ | Chi tiết sản phẩm |
| 44 | POST | `/sales/products` | ✅ Manager+ | Tạo sản phẩm |
| 45 | PUT | `/sales/products/{id}` | ✅ Manager+ | Sửa sản phẩm |
| 46 | DELETE | `/sales/products/{id}` | ✅ Manager+ | Xóa sản phẩm |
| 47 | POST | `/sales/orders` | ✅ Staff+ | Tạo đơn hàng (POS) |
| 48 | GET | `/sales/orders` | ✅ | DS đơn hàng |
| 49 | GET | `/sales/orders/{id}` | ✅ | Chi tiết đơn hàng |
| 50 | PUT | `/sales/orders/{id}/status` | ✅ Manager+ | Đổi trạng thái đơn |
| 51 | PUT | `/sales/orders/{id}/cancel` | ✅ Manager+ | Hủy đơn hàng |
| 52 | POST | `/sales/inventory` | ✅ Manager+ | Tạo phiếu nhập/xuất kho |
| 53 | GET | `/sales/inventory` | ✅ | DS phiếu kho |
| 54 | GET | `/sales/inventory/{id}` | ✅ | Chi tiết phiếu kho |
| 55 | PUT | `/sales/inventory/{id}/confirm` | ✅ Manager+ | Xác nhận phiếu (cập nhật stock) |
| 56 | PUT | `/sales/inventory/{id}/cancel` | ✅ Manager+ | Hủy phiếu |
| 57 | DELETE | `/sales/inventory/{id}` | ✅ Manager+ | Xóa phiếu (Draft only) |
| 58 | GET | `/sales/dashboard/overview` | ✅ Owner | Tổng quan kinh doanh |
| 59 | GET | `/sales/dashboard/revenue-chart` | ✅ Owner | Biểu đồ doanh thu |
| 60 | GET | `/sales/dashboard/top-products` | ✅ Owner | Top sản phẩm bán chạy |
| 61 | GET | `/sales/dashboard/order-status` | ✅ Owner | Phân bổ trạng thái đơn |
| 62 | GET | `/sales/dashboard/inventory-summary` | ✅ Owner | Tổng quan tồn kho |
| 63 | GET | `/sales/dashboard/recent-activity` | ✅ Owner | Hoạt động gần đây |

### 👥 CRM Service (`/crm`)

| # | Method | Endpoint | Auth | Mô tả |
|:-:|:------:|----------|:----:|-------|
| 64 | GET | `/crm/customers` | ✅ Staff+ | DS khách hàng |
| 65 | GET | `/crm/customers/{id}` | ✅ Staff+ | Chi tiết khách |
| 66 | POST | `/crm/customers` | ✅ Staff+ | Tạo khách hàng |
| 67 | PUT | `/crm/customers/{id}` | ✅ Staff+ | Sửa khách hàng |
| 68 | DELETE | `/crm/customers/{id}` | ✅ Manager+ | Xóa khách hàng |
| 69 | POST | `/crm/feedback/public/{orderId}` | ❌ | Feedback qua QR (khách) |
| 70 | POST | `/crm/feedback` | ✅ Staff+ | Staff tạo feedback hộ |
| 71 | GET | `/crm/feedback` | ✅ Staff+ | DS feedback (filter, paging) |
| 72 | GET | `/crm/feedback/summary` | ✅ Manager+ | Tổng hợp rating |
| 73 | GET | `/crm/customers/{id}/feedback` | ✅ Staff+ | Feedback 1 khách |
| 74 | GET | `/crm/loyalty-rules` | ✅ | DS quy tắc loyalty |
| 75 | POST | `/crm/loyalty-rules` | ✅ Manager+ | Tạo quy tắc |
| 76 | PUT | `/crm/loyalty-rules/{id}` | ✅ Manager+ | Sửa quy tắc |
| 77 | DELETE | `/crm/loyalty-rules/{id}` | ✅ Manager+ | Xóa quy tắc |
| 78 | GET | `/crm/customers/{id}/loyalty-summary` | ✅ | Điểm tích lũy khách |
| 79 | GET | `/crm/customers/{id}/loyalty-transactions` | ✅ | Lịch sử điểm |
| 80 | POST | `/crm/customers/{id}/redeem` | ✅ Staff+ | Đổi điểm thưởng |

### ⏰ HR Service (`/hr`)

| # | Method | Endpoint | Auth | Mô tả |
|:-:|:------:|----------|:----:|-------|
| 81 | GET | `/hr/employees` | ✅ Manager+ | DS nhân viên |
| 82 | GET | `/hr/employees/{id}` | ✅ Manager+ | Chi tiết nhân viên |
| 83 | GET | `/hr/employees/me` | ✅ | Hồ sơ nhân viên của tôi |
| 84 | PUT | `/hr/employees/me` | ✅ | Cập nhật hồ sơ |
| 85 | POST | `/hr/employees/me/avatar` | ✅ | Upload ảnh đại diện |
| 86 | PUT | `/hr/employees/{id}` | ✅ Manager+ | Sửa nhân viên |
| 87 | POST | `/hr/timekeeping/check-in` | ✅ | Chấm công vào (GPS) |
| 88 | POST | `/hr/timekeeping/check-out` | ✅ | Chấm công ra |
| 89 | GET | `/hr/timekeeping/today` | ✅ | Trạng thái hôm nay + GPS warning |
| 90 | GET | `/hr/timekeeping` | ✅ | Lịch sử chấm công |
| 91 | GET | `/hr/timekeeping/summary` | ✅ Manager+ | Tổng hợp chấm công tháng |
| 92 | POST | `/hr/tasks` | ✅ Manager+ | Tạo task cho nhân viên |
| 93 | GET | `/hr/tasks` | ✅ Manager+ | DS tasks |
| 94 | GET | `/hr/tasks/me` | ✅ | Tasks được giao cho tôi |
| 95 | GET | `/hr/tasks/{id}` | ✅ | Chi tiết task |
| 96 | PUT | `/hr/tasks/{id}` | ✅ Manager+ | Sửa task |
| 97 | PUT | `/hr/tasks/{id}/status` | ✅ | Cập nhật trạng thái task |
| 98 | DELETE | `/hr/tasks/{id}` | ✅ Manager+ | Xóa task |

---

## 🧪 Test API bằng Postman (Khuyến nghị)

### Bước 1: Import vào Postman

Trong thư mục `docs/` có 2 file:

| File | Import cách nào |
|------|----------------|
| `360Retail.postman_collection.json` | Postman → **Import** → chọn file |
| `360Retail.postman_environment.json` | Postman → **Import** → chọn file |

### Bước 2: Chọn Environment

Góc **trên bên phải** Postman → dropdown "No environment" → chọn **"360Retail Local"**

> ⚠️ Bắt buộc! Nếu không chọn thì `{{baseUrl}}` sẽ trống → request lỗi.

### Bước 3: Test theo thứ tự

```
📌 Register → Login ⭐ → Start Trial → Login ⭐ lại → Xong! Test gì cũng được
```

| Bước | Request | Folder | Ghi chú |
|:----:|---------|--------|---------|
| 1 | **Register** | 1. Identity - Auth | Đăng ký (email/pass đã điền sẵn) |
| 2 | **Login ⭐** | 1. Identity - Auth | Token **tự lưu** vào biến `accessToken` |
| 3 | **Start Trial** | 2. Identity - Subscription | Tạo store + storeId **tự lưu** |
| 4 | **Login ⭐** (lần 2) | 1. Identity - Auth | Token mới có storeId + StoreOwner |
| 5+ | Bất kỳ endpoint | Bất kỳ folder | Auth tự gắn, không cần paste token |

### Tại sao không cần nhập gì?

1. **Body có sẵn**: Mỗi request đã có JSON body mẫu (click tab **Body** để xem/sửa)
2. **Token tự lưu**: Login xong → script trong tab **Tests** tự extract token → lưu vào biến
3. **Auth kế thừa**: Collection đã set `Bearer {{accessToken}}` → mọi request con tự có auth
4. **Biến tự cập nhật**: `storeId`, `userId`, `customerId` tự lưu khi gọi các endpoint tương ứng

### Kiểm tra biến đã lưu

Click **👁️ icon** (Quick Look) góc trên phải → xem giá trị `accessToken`, `storeId`, `userId`

### Publish Documentation (tùy chọn)

Postman → Collection 360Retail API → **⋯** → **View Documentation** → **Publish** → Tạo link online doc chia sẻ cho team.

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

## Luồng 10: Customer Feedback (QR Code)

> **Nghiệp vụ**: Khách mua hàng → Nhận hóa đơn có QR → Quét QR → Feedback (không cần đăng nhập)
> Mỗi đơn hàng chỉ được feedback **1 lần**.

### 10.1: Feedback qua QR (Public — không cần auth)

```
POST /crm/feedback/public/{orderId}
```
```json
{
  "customerId": "customer-uuid",
  "storeId": "store-uuid",
  "rating": 5,
  "content": "Sản phẩm rất tốt!"
}
```
→ Response:
```json
{
  "success": true,
  "message": "Cảm ơn bạn đã đánh giá!",
  "data": {
    "id": "feedback-uuid",
    "customerId": "...",
    "customerName": "Nguyễn Văn A",
    "content": "Sản phẩm rất tốt!",
    "rating": 5,
    "source": "QRCode",
    "createdAt": "2026-02-27T15:21:01"
  }
}
```

| Lỗi | Nguyên nhân |
|-----|------------|
| 400 "Đơn hàng này đã được đánh giá rồi" | Trùng orderId |
| 400 "Thông tin khách hàng không hợp lệ" | customerId/storeId sai |

> ⚠️ **FE**: Trên QR in URL dạng: `https://your-domain/feedback/{orderId}?customerId=xxx&storeId=yyy`. FE mở form cho khách chọn ⭐ + viết nhận xét.

### 10.2: Feedback do Staff nhập hộ (cần auth)

```
POST /crm/feedback
```
| Auth | Role |
|------|------|
| ✅ Bearer | StoreOwner, Manager, Staff |

```json
{
  "customerId": "customer-uuid",
  "content": "Khách rất hài lòng",
  "rating": 4,
  "source": "InStore"
}
```

### 10.3: Danh sách feedback

```
GET /crm/feedback?rating=5&from=2026-01-01&to=2026-12-31&page=1&pageSize=20
```
| Auth | Role |
|------|------|
| ✅ | StoreOwner, Manager, Staff |

→ Response:
```json
{
  "success": true,
  "data": [
    { "id": "...", "customerName": "KH A", "rating": 5, "content": "Tốt!", "source": "QRCode", "createdAt": "..." }
  ],
  "meta": { "page": 1, "pageSize": 20, "total": 1 }
}
```

### 10.4: Tổng hợp feedback (Dashboard)

```
GET /crm/feedback/summary
```
| Auth | Role |
|------|------|
| ✅ | StoreOwner, Manager |

```json
{
  "success": true,
  "data": {
    "avgRating": 4.5,
    "totalCount": 120,
    "distribution": { "1": 5, "2": 3, "3": 10, "4": 42, "5": 60 }
  }
}
```

### 10.5: Feedback của 1 khách

```
GET /crm/customers/{customerId}/feedback?page=1&pageSize=20
```
| Auth | Role |
|------|------|
| ✅ | StoreOwner, Manager, Staff |

---

## Luồng 11: Đánh giá gói SaaS (Plan Reviews)

> **Nghiệp vụ**: Store Owner mua gói → sử dụng → đánh giá gói → Hiển thị trên trang Pricing cho khách mới xem.
> SuperAdmin quản lý review, xóa spam.

### 11.1: Tạo review gói (Owner)

```
POST /saas/plan-reviews
```
| Auth | Role | Điều kiện |
|------|------|-----------|
| ✅ | StoreOwner, Owner | Phải có subscription active + chưa review gói này |

```json
{
  "planId": "plan-uuid",
  "rating": 5,
  "content": "Gói Pro rất đáng tiền!"
}
```
→ Response:
```json
{
  "success": true,
  "message": "Đánh giá thành công",
  "data": {
    "id": "review-uuid",
    "planId": "...",
    "planName": "Pro",
    "userId": "...",
    "storeId": "...",
    "storeName": "Store ABC",
    "rating": 5,
    "content": "Gói Pro rất đáng tiền!",
    "createdAt": "2026-02-27T15:22:28Z"
  }
}
```

| Lỗi | Nguyên nhân |
|-----|------------|
| 400 "Bạn chưa đăng ký gói này" | Không có subscription active |
| 400 "Bạn đã đánh giá gói này rồi" | Duplicate |

### 11.2: Xem review của tôi

```
GET /saas/plan-reviews/me/{planId}
```
| Auth |
|------|
| ✅ |

> FE dùng để check user đã review chưa → ẩn/hiện nút "Đánh giá". `data = null` nếu chưa review.

### 11.3: List reviews 1 gói (Public)

```
GET /saas/plan-reviews/plan/{planId}?page=1&pageSize=10
```
| Auth |
|------|
| ❌ Không cần |

### 11.4: Summary 1 gói (Public)

```
GET /saas/plan-reviews/plan/{planId}/summary
```
```json
{
  "success": true,
  "data": {
    "planId": "...", "planName": "Pro",
    "avgRating": 4.8, "totalReviews": 25,
    "distribution": { "1": 0, "2": 1, "3": 2, "4": 8, "5": 14 }
  }
}
```

### 11.5: Summary TẤT CẢ gói (Public — cho trang Pricing)

```
GET /saas/plan-reviews/summary
```
→ Trả về mảng summary cho mỗi gói (Trial, Basic, Pro, Yearly).

> **FE**: Gọi endpoint này trên trang Pricing, hiển thị ⭐ avgRating bên cạnh mỗi gói.

### 11.6: SuperAdmin — Quản lý reviews

```
GET    /saas/plan-reviews/admin?planId=&rating=&page=1&pageSize=20    [SuperAdmin]
GET    /saas/plan-reviews/admin/dashboard                              [SuperAdmin]
DELETE /saas/plan-reviews/admin/{reviewId}                              [SuperAdmin]
```

**Dashboard response:**
```json
{
  "success": true,
  "data": {
    "overallAvgRating": 4.5,
    "totalReviews": 45,
    "reviewsThisMonth": 12,
    "planStats": [
      { "planId": "...", "planName": "Trial", "avgRating": 4.5, "totalReviews": 25 },
      { "planId": "...", "planName": "Pro", "avgRating": 4.8, "totalReviews": 8 }
    ]
  }
}
```

---

## Luồng 12: Cập nhật GPS / Vị trí cửa hàng

> **Nghiệp vụ**: Owner tạo store → chưa có GPS → vào Cài đặt → chọn vị trí trên Google Maps → lưu tọa độ → Geofencing check-in hoạt động (200m).

### 12.1: Cập nhật GPS cho store

```
PUT /saas/stores/{storeId}
```
| Auth | Role |
|------|------|
| ✅ | StoreOwner, SuperAdmin |

```json
{
  "address": "123 Nguyễn Huệ, Quận 1, TP.HCM",
  "latitude": 10.7769,
  "longitude": 106.7009
}
```

**Validation:**
- `latitude`: -90 → 90
- `longitude`: -180 → 180
- **Phải gửi CẢ 2** (latitude + longitude) hoặc **không gửi cái nào**
- Các field khác (`storeName`, `phone`) gửi kèm = cập nhật, không gửi = giữ nguyên

### 12.2: GET Store (có GPS)

```
GET /saas/stores/{storeId}
```
```json
{
  "id": "store-uuid",
  "storeName": "Store ABC",
  "address": "123 Nguyễn Huệ, Q1, HCM",
  "phone": "0901234567",
  "latitude": 10.7769,
  "longitude": 106.7009,
  "isActive": true,
  "createdAt": "2026-02-27T15:07:49Z"
}
```

> **FE**: Check `latitude == null` → hiển thị banner "Cửa hàng chưa cài đặt vị trí" + link đến Cài đặt Store.
> Sử dụng **Google Maps JavaScript API** cho form chọn vị trí (place autocomplete + pin trên map).

---

## Luồng 13: Chấm công GPS (Timekeeping Warning)

> **Nghiệp vụ**: Khi store chưa cài GPS → check-in vẫn thành công nhưng kèm `warning` nhắc Owner cập nhật.
> Khi đã cài GPS → Geofencing: khoảng cách ≤ 200m mới cho check-in.

### 13.1: Trạng thái hôm nay

```
GET /hr/timekeeping/today
```
| Auth |
|------|
| ✅ (Employee thuộc store) |

→ Response (store **chưa** cài GPS):
```json
{
  "success": true,
  "data": {
    "hasCheckedIn": false,
    "hasCheckedOut": false,
    "isGpsConfigured": false,
    "warning": "⚠️ Cửa hàng chưa cài đặt tọa độ GPS. Vui lòng cập nhật địa chỉ trong Cài đặt để sử dụng chấm công GPS.",
    "record": null
  }
}
```

→ Response (store **đã** cài GPS, đã check-in):
```json
{
  "success": true,
  "data": {
    "hasCheckedIn": true,
    "hasCheckedOut": false,
    "isGpsConfigured": true,
    "warning": null,
    "record": {
      "id": "...", "employeeName": "Nguyễn Văn A",
      "checkInTime": "2026-02-27T09:00:12",
      "isLate": false, "workHours": null, "warning": null
    }
  }
}
```

> **FE**: Check `warning != null` → hiển thị banner vàng. Check `isGpsConfigured` → hiện link "Cập nhật GPS" (chỉ cho Owner).

### 13.2: Check-in

```
POST /hr/timekeeping/check-in
```
```json
{
  "locationGps": "10.7780,106.7015",
  "checkInImageUrl": "https://..."
}
```

| Tình huống | Response |
|-----------|----------|
| Store chưa GPS | ✅ Check-in OK + `warning` nhắc cài GPS |
| Store có GPS, trong 200m | ✅ Check-in OK, `warning: null` |
| Store có GPS, ngoài 200m | ❌ 400 "Bạn đang ở quá xa cửa hàng (350m)" |

### 13.3: Check-out

```
POST /hr/timekeeping/check-out
```
```json
{
  "locationGps": "10.7780,106.7015"
}
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
