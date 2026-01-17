# 360Retail API Documentation

> **Tài liệu hướng dẫn call API cho Frontend Team**  
> Cập nhật: 14/01/2026

---

## 📌 Thông tin chung

### Base URLs (Development)

| Service | URL | Port |
|---------|-----|------|
| **API Gateway** | `http://localhost:5001` | 5001 |
| **Identity** | `http://localhost:5297` | 5297 |
| **SaaS** | `http://localhost:5031` | 5031 |
| **Sales** | `http://localhost:5091` | 5091 |
| **HR** | `http://localhost:5280` | 5280 |
| **CRM** | `http://localhost:5169` | 5169 |

### Authentication

Tất cả API (trừ Login/Register) đều yêu cầu **Bearer Token** trong header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🔐 IDENTITY SERVICE

### 1. Đăng ký tài khoản (StoreOwner)

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "owner@example.com",
  "password": "Password123!"
}
```

**Response:** `200 OK`
```json
{
  "message": "Register successful"
}
```

---

### 2. Đăng nhập

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "owner@example.com",
  "password": "Password123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-01-14T22:00:00Z",
  "mustChangePassword": false
}
```

---

### 3. Refresh Token / Switch Store

```http
POST /api/auth/refresh-access?storeId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Authorization: Bearer {token}
```

**Response:** Token mới với store_id được cập nhật

---

### 4. Đổi mật khẩu

```http
POST /api/auth/change-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword456!",
  "confirmNewPassword": "NewPassword456!"
}
```

---

### 5. Xem thông tin User hiện tại

```http
GET /api/auth/me
Authorization: Bearer {token}
```

**Response:** Claims trong token

---

## 🏪 SAAS SERVICE (Stores)

### 1. Tạo Store mới

```http
POST /api/stores
Authorization: Bearer {token}
Content-Type: application/json

{
  "storeName": "Cửa hàng ABC",
  "address": "123 Nguyễn Văn Linh, Q7, TP.HCM",
  "phone": "0901234567"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Store created successfully",
  "data": {
    "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "storeName": "Cửa hàng ABC",
    "address": "123 Nguyễn Văn Linh, Q7, TP.HCM",
    "phone": "0901234567",
    "isActive": true
  }
}
```

---

### 2. Lấy danh sách Stores

```http
GET /api/stores
Authorization: Bearer {token}
```

---

### 3. Cập nhật Store

```http
PUT /api/stores/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "storeName": "Cửa hàng ABC - Updated",
  "address": "456 Lê Văn Việt, Q9, TP.HCM",
  "phone": "0909876543",
  "isActive": true
}
```

---

## 📦 SALES SERVICE

### Categories

#### Lấy danh sách Categories

```http
GET /api/categories?storeId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Authorization: Bearer {token}
```

#### Tạo Category

```http
POST /api/categories
Authorization: Bearer {token}
Content-Type: application/json

{
  "categoryName": "Điện thoại",
  "parentId": null,
  "isActive": true
}
```

#### Cập nhật Category

```http
PUT /api/categories/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": "category-id-here",
  "categoryName": "Điện thoại di động",
  "parentId": null,
  "isActive": true
}
```

#### Xóa Category (Soft Delete)

```http
DELETE /api/categories/{id}
Authorization: Bearer {token}
```

---

### Products

#### Lấy danh sách Products

```http
GET /api/products?storeId=xxx&categoryId=xxx&page=1&pageSize=20
Authorization: Bearer {token}
```

#### Tạo Product (không có Variants)

```http
POST /api/products
Authorization: Bearer {token}
Content-Type: multipart/form-data

{
  "productName": "iPhone 15 Pro Max",
  "categoryId": "category-id-here",
  "barCode": "IP15PM001",
  "price": 32990000,
  "costPrice": 28000000,
  "stockQuantity": 50,
  "description": "iPhone mới nhất từ Apple",
  "isActive": true,
  "hasVariants": false,
  "variants": []
}
```

#### Tạo Product (có Variants)

```http
POST /api/products
Authorization: Bearer {token}
Content-Type: multipart/form-data

{
  "productName": "Áo thun Polo",
  "categoryId": "category-id-here",
  "barCode": "POLO001",
  "price": 350000,
  "costPrice": 150000,
  "stockQuantity": 0,
  "description": "Áo thun Polo cao cấp",
  "isActive": true,
  "hasVariants": true,
  "variants": [
    {
      "sku": "POLO-M-DEN",
      "size": "M",
      "color": "Đen",
      "priceOverride": 350000,
      "stockQuantity": 20
    },
    {
      "sku": "POLO-L-TRANG",
      "size": "L",
      "color": "Trắng",
      "priceOverride": 360000,
      "stockQuantity": 15
    }
  ]
}
```

---

### Orders

#### Tạo Order (không có Customer, không có Variant)

```http
POST /api/sales/orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "customerId": null,
  "paymentMethod": "Cash",
  "discountAmount": 0,
  "items": [
    {
      "productId": "product-id-here",
      "quantity": 2,
      "productVariantId": null
    }
  ]
}
```

#### Tạo Order (có Customer, có Variant)

```http
POST /api/sales/orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "customerId": "customer-id-here",
  "paymentMethod": "Card",
  "discountAmount": 50000,
  "items": [
    {
      "productId": "product-id-ao-thun",
      "quantity": 2,
      "productVariantId": "variant-id-size-M-mau-den"
    },
    {
      "productId": "product-id-khong-co-variant",
      "quantity": 1,
      "productVariantId": null
    }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Order created successfully",
  "data": "order-id-uuid",
  "errors": null
}
```

---

#### Lấy danh sách Orders

```http
GET /api/sales/orders?status=Pending&fromDate=2026-01-01&toDate=2026-01-31&page=1&pageSize=20
Authorization: Bearer {token}
```

**Query Parameters:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `status` | string | No | Filter: `Pending`, `Processing`, `Completed`, `Cancelled` |
| `fromDate` | date | No | Format: `YYYY-MM-DD` |
| `toDate` | date | No | Format: `YYYY-MM-DD` |
| `page` | int | No | Default: 1 |
| `pageSize` | int | No | Default: 20 |

---

#### Xem chi tiết Order

```http
GET /api/sales/orders/{id}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "order-id",
    "code": "ORD-260114-1234",
    "storeId": "store-id",
    "employeeId": "employee-id",
    "customerId": null,
    "totalAmount": 700000,
    "discountAmount": 0,
    "status": "Completed",
    "paymentMethod": "Cash",
    "paymentStatus": "Paid",
    "createdAt": "2026-01-14T12:00:00Z",
    "orderItems": [
      {
        "id": "item-id",
        "productId": "product-id",
        "productName": "Áo thun Polo",
        "barCode": "POLO001",
        "quantity": 2,
        "unitPrice": 350000,
        "total": 700000,
        "productVariantId": "variant-id",
        "sku": "POLO-M-DEN",
        "size": "M",
        "color": "Đen"
      }
    ]
  }
}
```

---

#### Cập nhật trạng thái Order

```http
PUT /api/sales/orders/{id}/status?status=Completed
Authorization: Bearer {token}
```

**Status values:** `Pending`, `Processing`, `Completed`, `Cancelled`

---

## 📋 Response Format

Tất cả API đều trả về format chuẩn:

### Success Response

```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... },
  "errors": null
}
```

### Error Response

```json
{
  "success": false,
  "message": "Error message here",
  "data": null,
  "errors": ["Detail error 1", "Detail error 2"]
}
```

---

## ⚠️ Lưu ý quan trọng

1. **Token hết hạn**: Mặc định 60 phút, cần gọi `/api/auth/refresh-access` để lấy token mới
2. **store_id trong Token**: Sau khi tạo Store, cần gọi `/api/auth/refresh-access` để cập nhật token
3. **Product với Variants**: Nếu `hasVariants = true`, bắt buộc phải truyền `productVariantId` khi tạo Order
4. **Employee ID**: Nếu user chưa có trong bảng `hr.employees`, order sẽ có `employeeId = null`

---

## 🔧 Debug Tips

1. Dùng `GET /api/auth/me` để xem claims trong token
2. Kiểm tra `store_id` có trong token chưa
3. Đảm bảo chạy cả Identity và Sales services cùng lúc

