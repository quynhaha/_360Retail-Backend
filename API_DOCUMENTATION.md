# 360Retail - Hướng Dẫn Sử Dụng API

> **Tài liệu hướng dẫn thực hành cho Frontend Team**  
> Cập nhật: 22/01/2026

---

## � Khởi động Backend

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
  "token": "eyJhbGciOiJIUzI1...",
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
```json
{
  "storeName": "Cửa hàng ABC"
}
```
→ Response: Token MỚI với `status: "Trial"` và `store_id`

⚠️ **Copy token mới** và cập nhật lại Authorize!

---

### Bước 1.4: Kiểm tra claims trong JWT

```
GET /identity/auth/me
```
→ Response: Danh sách claims trong token
```json
[
  { "type": "sub", "value": "user-id" },
  { "type": "store_id", "value": "store-id" },
  { "type": "status", "value": "Trial" },
  { "type": "trial_expired", "value": "false" }
]
```

**Giờ bạn có thể sử dụng tất cả APIs khác!**

---

## Luồng 2: Mua gói dịch vụ (Trial → Paid)

### Bước 2.1: Xem danh sách gói

```
GET /saas/subscriptions/plans
```
→ Response:
```json
{
  "success": true,
  "data": [
    { "id": "xxx", "planName": "Basic", "price": 199000, "durationDays": 30 },
    { "id": "yyy", "planName": "Premium", "price": 499000, "durationDays": 30 }
  ]
}
```

---

### Bước 2.2: Mua gói

```
POST /saas/subscriptions/purchase
```
```json
{
  "planId": "xxx"  // ID từ bước 2.1
}
```
→ Response:
```json
{
  "paymentId": "payment-id",
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/...",
  "amount": 199000,
  "planName": "Basic"
}
```

---

### Bước 2.3: Thanh toán

Copy `paymentUrl` và mở trong trình duyệt mới.

**Test VNPay Sandbox**:
- Ngân hàng: NCB
- Số thẻ: 9704198526191432198
- Tên: NGUYEN VAN A
- Ngày: 07/15
- OTP: 123456

---

### Bước 2.4: Refresh Token sau thanh toán

```
POST /identity/auth/refresh-access
```
(Không cần body)

→ Response: Token MỚI với `status: "Active"`

---

## Luồng 3: Tạo Store mới (cho Paid Owner)

### Bước 3.1: Tạo Store + Mua gói

```
POST /saas/stores
```
```json
{
  "storeName": "Chi nhánh 2",
  "address": "456 Lê Văn Việt",
  "phone": "0909123456",
  "planId": "xxx"  // Bắt buộc nếu status = Active
}
```
→ Response:
```json
{
  "success": true,
  "store": {
    "id": "new-store-id",
    "storeName": "Chi nhánh 2",
    "isActive": false  // Chờ thanh toán
  },
  "payment": {
    "paymentId": "payment-id",
    "paymentUrl": "/api/payments/initiate?paymentId=xxx",
    "amount": 199000
  }
}
```

---

### Bước 3.2: Lấy link thanh toán

```
GET /saas/payments/initiate?paymentId=xxx
```
→ Response:
```json
{
  "success": true,
  "paymentUrl": "https://sandbox.vnpayment.vn/...",
  "amount": 199000
}
```
→ Copy `paymentUrl` và thanh toán trên trình duyệt

---

### Bước 3.3: Chuyển sang Store mới

Sau khi thanh toán xong:
```
POST /identity/auth/refresh-access?storeId=new-store-id
```
→ Response: Token MỚI với `store_id` là store mới

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

---

### 4.2: Tạo Product (không có Variants)

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
| hasVariants | false |

---

### 4.3: Tạo Product (có Variants)

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

---

## Luồng 5: Tạo đơn hàng

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

> **Lưu ý**: Nếu product có Variants, phải truyền `productVariantId`

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

---

### 6.2: Xem nhân viên của store

```
GET /hr/employees
```

---

### 6.3: Giao việc cho nhân viên

```
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

# ⚠️ Xử lý lỗi thường gặp

## 401 Unauthorized
- Token hết hạn (60 phút)
- Token chưa được set trong Authorize
→ **Giải pháp**: Login lại hoặc Refresh Access

## 403 Forbidden
| Message | Nguyên nhân | Giải pháp |
|---------|-------------|-----------|
| TrialExpired | Trial 7 ngày đã hết | Mua gói |
| SubscriptionExpired | Subscription hết hạn | Gia hạn |

## 400 BadRequest: "Vui lòng chọn gói dịch vụ"
- Owner đã paid đang tạo store mới nhưng không truyền planId
→ **Giải pháp**: Thêm `planId` vào body request

## Store is not active
- User cố switch sang store chưa thanh toán
→ **Giải pháp**: Thanh toán cho store đó trước

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
