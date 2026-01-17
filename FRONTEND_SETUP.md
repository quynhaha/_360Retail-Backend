# Hướng dẫn chạy Backend cho Frontend Developers

Chào các bạn Frontend! Tài liệu này sẽ giúp các bạn chạy Backend của dự án **360Retail** một cách nhanh chóng và dễ dàng nhất.

## 1. Yêu cầu hệ thống
- Đã cài đặt [Docker Desktop](https://www.docker.com/products/docker-desktop/).

## 2. Cách chạy nhanh nhất (Khuyên dùng)
Bạn không cần cài đặt .NET SDK hay PostgreSQL trên máy thật. Chỉ cần chạy lệnh sau tại thư mục gốc của project:

```bash
docker-compose up -d
```

Lệnh này sẽ khởi chạy:
- **PostgreSQL**: Database (Port 5433).
- **pgAdmin**: Công cụ quản lý DB (Truy cập tại http://localhost:5050 - Email: `admin@360retail.com`, Pass: `admin`).
- **Identity API**: Quản lý tài khoản (Port 5297).
- **SaaS API**: Quản lý cửa hàng/hệ thống (Port 5031).
- **Sales API**: Quản lý bán hàng/sản phẩm (Port 5091).
- **HR API**: Quản lý nhân sự (Port 5280).
- **CRM API**: Quản lý khách hàng (Port 5169).

## 3. Danh sách API (Swagger)
Sau khi chạy Docker, các bạn có thể truy cập Swagger:

### 🎯 API Gateway (Khuyên dùng)
Truy cập **một URL duy nhất** để xem tất cả APIs:
- **[API Gateway](http://localhost:5001/swagger)** - Gộp tất cả services

### Swagger riêng từng service (nếu cần debug)
- [Identity API](http://localhost:5297/swagger)
- [SaaS API](http://localhost:5031/swagger)
- [Sales API](http://localhost:5091/swagger)
- [HR API](http://localhost:5280/swagger)
- [CRM API](http://localhost:5169/swagger)

### Routing qua API Gateway
Khi sử dụng API Gateway, các endpoint sẽ có prefix tương ứng:
| Service | Prefix | Ví dụ |
|---------|--------|-------|
| Identity | `/identity` | `http://localhost:5001/identity/auth/login` |
| SaaS | `/saas` | `http://localhost:5001/saas/stores` |
| Sales | `/sales` | `http://localhost:5001/sales/products` |
| HR | `/hr` | `http://localhost:5001/hr/employees` |
| CRM | `/crm` | `http://localhost:5001/crm/customers` |

## 4. Cấu hình CORS
Backend đã được cấu hình CORS để cho phép các request từ các port phổ biến sau:
- http://localhost:3000 (React mặc định)
- http://localhost:5173 (Vite mặc định)
- http://localhost:4200 (Angular mặc định)

Nếu các bạn chạy Frontend ở port khác, hãy báo cho team Backend cập nhật.

## 5. Cấu hình Database
Nếu bạn muốn kết nối trực tiếp vào Database:
- **Host**: `localhost`
- **Port**: `5433`
- **User**: `postgres`
- **Password**: `12345`
- **Database**: `360RetailDB`

## 6. Cách cập nhật code và dữ liệu mới

Khi team Backend có thay đổi về code hoặc cấu trúc Database, các bạn chỉ cần làm theo các bước sau:

1. **Lấy code mới nhất**:
   ```bash
   git pull
   ```

2. **Rebuild và khởi động lại**:
   Sử dụng flag `--build` để Docker đóng gói lại code mới:
   ```bash
   docker-compose up -d --build
   ```

### 3. Lưu ý về Database (Kỹ thuật DB First)
   - Vì dự án sử dụng **DB First**, team Backend sẽ cung cấp các file `.sql` trong thư mục `init-db/`.
   - Các file này sẽ **tự động chạy** khi Docker khởi tạo Database lần đầu tiên.
   - Khi Backend thông báo có cập nhật Database (thay đổi file SQL), các bạn chỉ cần chạy:
     ```bash
     docker-compose down -v
     docker-compose up -d --build
     ```
     *(Lệnh này sẽ xóa dữ liệu cũ và khởi tạo lại DB mới từ các file SQL mới nhất)*.

## 7. Truy cập pgAdmin để xem dữ liệu

Sau khi Docker đã chạy, các bạn có thể xem và quản lý dữ liệu trong database thông qua **pgAdmin**:

### Bước 1: Truy cập pgAdmin
Mở trình duyệt và vào: **http://localhost:5050**

### Bước 2: Đăng nhập
| Field | Value |
|-------|-------|
| Email | `admin@360retail.com` |
| Password | `admin` |

### Bước 3: Kết nối Database
1. Click chuột phải vào **Servers** → **Register** → **Server...**
2. Tab **General**: Đặt tên bất kỳ (VD: `360RetailDB`)
3. Tab **Connection**:

| Field | Value |
|-------|-------|
| Host name/address | `360retail-db` |
| Port | `5432` |
| Maintenance database | `360RetailDB` |
| Username | `postgres` |
| Password | `12345` |

4. Tick **Save password** → Click **Save**
5. Browse các tables trong schemas: `identity`, `saas`, `hr`, `sales`, `crm`

> **Lưu ý**: Nếu hostname `360retail-db` không được chấp nhận, hãy thử dùng IP address của container. Chạy lệnh sau để lấy IP:
> ```bash
> docker inspect 360retail-db --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}"
> ```

---

## 8. Xử lý lỗi Database thường gặp

### Lỗi thiếu cột (VD: `must_change_password`, `app_user_id`, v.v.)

**Nguyên nhân**: Docker volume giữ dữ liệu cũ, script SQL mới không được chạy lại.

**Giải pháp 1 - Reset hoàn toàn DB** (mất dữ liệu cũ):
```bash
docker compose down -v
docker compose up -d --build
```

**Giải pháp 2 - Giữ dữ liệu, thêm cột thủ công** (qua pgAdmin):
1. Mở pgAdmin → Kết nối database
2. Click chuột phải vào database `360RetailDB` → **Query Tool**
3. Chạy các lệnh ALTER TABLE cần thiết, ví dụ:
```sql
ALTER TABLE identity.app_users
ADD COLUMN IF NOT EXISTS must_change_password BOOLEAN DEFAULT FALSE;
```

---

Chúc các bạn code vui vẻ! 🚀
