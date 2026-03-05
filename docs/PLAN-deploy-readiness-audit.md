# 🚀 360Retail Backend — Deployment Readiness Audit

> **Mục tiêu**: Tổng hợp toàn bộ vấn đề cần sửa/bổ sung trước khi deploy production.
> **Ngày audit**: 05/03/2026

---

## 📊 Tổng quan đánh giá

| Hạng mục | Trạng thái | Chi tiết |
|----------|-----------|----------|
| **Docker & Container** | ⚠️ Gần sẵn sàng | Thiếu HEALTHCHECK trong Dockerfile |
| **Security / Secrets** | ✅ OK | `.env` đã trong `.gitignore`, appsettings rỗng secret |
| **CI/CD** | ⚠️ Chỉ Build+Test | Không có deploy pipeline |
| **Health Checks** | ✅ OK | Tất cả 5 service có `/health` |
| **Swagger** | ✅ OK | Chỉ bật trong Development |
| **Logging** | ✅ OK | Serilog structured logging trên tất cả services |
| **Exception Handling** | ✅ OK | `UseGlobalExceptionHandler()` trên tất cả services |
| **CORS** | ✅ OK | Configurable qua env var `CorsOrigins` |
| **Database** | ⚠️ Có vấn đề nhỏ | CRM sai default DB name, thiếu connection pool |
| **Production Config** | ❌ Thiếu | Không có `appsettings.Production.json` |

---

## 🔴 ƯU TIÊN CAO — Cần sửa trước deploy

### 1. CRM Service — Sai DB Connection String mặc định

**File**: `src/Services/CRM/API/appsettings.json`

```diff
 "ConnectionStrings": {
-    "DefaultConnection": "Host=localhost;Database=360retail_crm;Username=postgres;Password=postgres"
+    "DefaultConnection": "Host=localhost;Port=5432;Database=360RetailDB;Username=postgres;Password=12345"
 }
```

> CRM đang trỏ tới database `360retail_crm` thay vì `360RetailDB` chung. Khi chạy Docker thì env var ghi đè nên OK, nhưng khi chạy local dotnet sẽ fail.

### 2. Thêm Connection Pool Size cho tất cả DB Connections

**Vấn đề**: Đã từng gặp lỗi `53300 - too many clients` (xem conversation #78ea86df). Cần set `Maximum Pool Size` trong connection string.

**Docker Compose**: Thêm vào tất cả connection strings:
```
Host=postgres;Port=5432;Database=360RetailDB;Username=postgres;Password=12345;Maximum Pool Size=20
```

**Các service cần sửa trong `docker-compose.yml`:**
- `identity-api` → `ConnectionStrings__DefaultConnection`
- `saas-api` → `ConnectionStrings__SaasDb`
- `sales-api` → `ConnectionStrings__DefaultConnection`
- `hr-api` → `ConnectionStrings__DefaultConnection`
- `crm-api` → `ConnectionStrings__DefaultConnection`

### 3. Thêm `appsettings.Production.json` cho từng Service

Hiện không có file config riêng cho Production. Khi `ASPNETCORE_ENVIRONMENT=Production`, chỉ đọc `appsettings.json`. Cần tạo cho các service cần config khác biệt:

**Tối thiểu cần tạo cho mỗi service:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

> Giảm log level từ `Information` → `Warning` trong production để tiết kiệm tài nguyên.

---

## 🟡 ƯU TIÊN TRUNG BÌNH — Nên sửa

### 4. Thêm Docker HEALTHCHECK trong Dockerfiles

Hiện các Dockerfile không có instruction `HEALTHCHECK`. Docker/Docker Compose sẽ không tự restart khi service down.

**Thêm vào tất cả 6 Dockerfiles** (trước `ENTRYPOINT`):
```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```

> **Lưu ý**: Base image `mcr.microsoft.com/dotnet/aspnet:8.0` không có `curl`. Có 2 cách:
> - Dùng `wget` (có sẵn trên Alpine) nếu đổi sang Alpine image
> - Hoặc dùng Docker Compose healthcheck thay vì Dockerfile

**Cách đơn giản hơn — healthcheck trong `docker-compose.yml`:**
```yaml
identity-api:
  healthcheck:
    test: ["CMD-SHELL", "wget -qO- http://localhost:8080/health || exit 1"]
    interval: 30s
    timeout: 5s
    retries: 3
    start_period: 30s
```

### 5. Docker Compose — Thêm `depends_on` condition

Hiện `depends_on` chỉ đợi container start, không đợi service ready:

```yaml
identity-api:
  depends_on:
    postgres:
      condition: service_healthy
    redis:
      condition: service_healthy
```

Cần thêm healthcheck cho `postgres` và `redis`:
```yaml
postgres:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres"]
    interval: 5s
    timeout: 5s
    retries: 5

redis:
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 5s
    timeout: 5s
    retries: 5
```

### 6. CORS — Thêm Production Domain

**File**: `.env` (hoặc production env vars)

```env
CORS_ORIGINS=http://localhost:3000,http://localhost:5173,https://360retail-cortexa.online,https://www.360retail-cortexa.online
```

> Cần thêm domain thực khi biết frontend URL.

### 7. VNPay — Production URL

**File**: `src/Services/Saas/API/appsettings.json` line 18

```diff
-    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
+    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
```

> Hiện đang trỏ tới **sandbox**. Khi go-live cần đổi sang production URL của VNPay. Có thể override bằng env var `VNPay__BaseUrl`.

---

## 🟢 ĐÃ OK — Không cần sửa

| Hạng mục | Chi tiết |
|----------|----------|
| **`.env` bảo mật** | `.gitignore` có `.env` ở dòng 7 ✅ |
| **Secrets trong appsettings** | Tất cả đều rỗng `""`, inject qua env var ✅ |
| **Swagger gating** | `if (app.Environment.IsDevelopment())` trên 5/5 services ✅ |
| **Health endpoints** | `/health` trên 5/5 services ✅ |
| **Serilog logging** | Structured console logging trên 5/5 services ✅ |
| **Global exception handler** | `UseGlobalExceptionHandler()` trên tất cả ✅ |
| **CORS configurable** | `CorsOrigins` env var trên tất cả services ✅ |
| **Docker prod override** | `docker-compose.prod.yml` ẩn ports DB/Redis, pgadmin profile=debug ✅ |
| **CI/CD build+test** | GitHub Actions chạy trên push/PR tới main/develop ✅ |
| **Init DB scripts** | `init-db/01-script.sql` auto-run khi tạo container ✅ |
| **Rate limiting** | Ocelot gateway rate limiting đầy đủ ✅ |
| **Internal API key** | Cross-service auth với shared key ✅ |

---

## 📋 Checklist Hành Động

| # | Hành động | Mức ưu tiên | Ảnh hưởng |
|---|-----------|------------|-----------|
| 1 | Sửa CRM `appsettings.json` DB name | 🔴 Cao | Local dev sẽ fail |
| 2 | Thêm `Maximum Pool Size=20` vào connection strings | 🔴 Cao | Tránh lỗi 53300 |
| 3 | Tạo `appsettings.Production.json` cho mỗi service | 🔴 Cao | Log level production |
| 4 | Thêm healthcheck cho postgres + redis trong compose | 🟡 Trung bình | Startup ordering |
| 5 | Thêm healthcheck cho app services trong compose | 🟡 Trung bình | Auto-restart |
| 6 | Cập nhật CORS domain production | 🟡 Trung bình | Frontend access |
| 7 | Review VNPay sandbox → production URL | 🟡 Trung bình | Payment go-live |

---

## ❓ Câu hỏi cần xác nhận trước khi làm

1. **Frontend domain** đã xác định chưa? (để set CORS)
2. **VNPay** đã có tài khoản production chưa hay vẫn dùng sandbox?
3. **Hosting**: Deploy Docker Compose lên đâu? (VPS, AWS ECS, DigitalOcean, etc.)
4. Có muốn mình **implement luôn** các fix ưu tiên cao (#1, #2, #3) không?

---

*Tạo bởi Antigravity — Deployment Readiness Audit*
