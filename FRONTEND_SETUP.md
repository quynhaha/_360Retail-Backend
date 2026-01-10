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

Chúc các bạn code vui vẻ! 🚀
