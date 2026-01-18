# 🚀 Backend Update Guide - January 18, 2026

## Tổng quan thay đổi

Phiên bản này bổ sung **Employee Profile Management** cho HR Service, bao gồm:
- Tự động tạo Employee khi Invite Staff
- API xem/cập nhật profile nhân viên
- Upload avatar cho nhân viên

---

## 📋 Các bước cần làm

### 1. Pull code mới
```bash
git pull origin main
```

### 2. Reset Database (BẮT BUỘC)
Database schema đã thay đổi, cần reset hoàn toàn:
```bash
docker-compose down -v
docker-compose up -d
```

> ⚠️ **Lưu ý:** Lệnh `-v` sẽ xóa toàn bộ dữ liệu cũ. Bạn cần tạo lại tài khoản test.

### 3. Cấu hình Cloudinary (nếu cần upload avatar)
Tạo file `.env` trong thư mục gốc với nội dung:
```env
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret
```

---

## 🆕 API mới cho Frontend

### HR Service - Employee Profile

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/hr/employees/me` | Lấy profile của mình | ✅ Token |
| PUT | `/hr/employees/me` | Cập nhật profile | ✅ Token |
| POST | `/hr/employees/me/avatar` | Upload avatar | ✅ Token |

### Request/Response Examples

#### GET /hr/employees/me
**Response:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "appUserId": "uuid",
    "storeId": "uuid",
    "fullName": "Nguyễn Văn A",
    "position": "Staff",
    "userName": "nguyenvana",
    "email": "a@example.com",
    "phoneNumber": "0901234567",
    "baseSalary": null,
    "joinDate": "2026-01-18T...",
    "status": "Active",
    "avatarUrl": "https://res.cloudinary.com/..."
  }
}
```

#### PUT /hr/employees/me
**Request Body:**
```json
{
  "fullName": "Tên mới",
  "userName": "username_moi",
  "phoneNumber": "0909999999"
}
```
> Tất cả fields đều optional, chỉ gửi những field muốn update.

#### POST /hr/employees/me/avatar
**Request:** `multipart/form-data` với field `file` chứa ảnh.

**Response:**
```json
{
  "success": true,
  "data": {
    "avatarUrl": "https://res.cloudinary.com/..."
  },
  "message": "Avatar uploaded successfully"
}
```

---

## 🔄 Thay đổi trong Invite Staff Flow

Khi gọi `POST /identity/staff/invite`, hệ thống giờ sẽ:
1. Tạo `AppUser` trong Identity Service
2. **TỰ ĐỘNG** tạo `Employee` trong HR Service (mới!)
3. Gửi email với mật khẩu tạm

→ Không cần gọi thêm API để tạo Employee nữa.

---

## ❓ Troubleshooting

### Lỗi "401 Unauthorized" khi gọi HR API
- Kiểm tra token có hợp lệ không
- Đảm bảo đã login và có `store_id` trong token

### Lỗi "Employee profile not found"
- User chưa được invite qua API mới
- Cần re-invite user để tạo Employee record

---

## 📞 Liên hệ hỗ trợ
Nếu có vấn đề, liên hệ Backend team qua [channel/email].
