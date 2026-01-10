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
- **PostgreSQL**: Database (Port 5432).
- **pgAdmin**: Công cụ quản lý DB (Truy cập tại http://localhost:5050 - Email: `admin@360retail.com`, Pass: `admin`).
- **Identity API**: Quản lý tài khoản (Port 5297).
- **SaaS API**: Quản lý cửa hàng/hệ thống (Port 5031).
- **Sales API**: Quản lý bán hàng/sản phẩm (Port 5091).
- **HR API**: Quản lý nhân sự (Port 5280).
- **CRM API**: Quản lý khách hàng (Port 5169).

## 3. Danh sách API (Swagger)
Sau khi chạy Docker, các bạn có thể truy cập Swagger của từng service để xem tài liệu API:
- [Identity API](http://localhost:5297/swagger)
- [SaaS API](http://localhost:5031/swagger)
- [Sales API](http://localhost:5091/swagger)
- [HR API](http://localhost:5280/swagger)
- [CRM API](http://localhost:5169/swagger)

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

Chúc các bạn code vui vẻ! 🚀
