# 🚀 PLAN: Deployment Readiness Audit — 360Retail Backend (Updated)

> **Cập nhật lần cuối**: 2026-03-04 10:50  
> **Phase 1**: ✅ Hoàn thành

---

## 📊 Tổng trạng thái

| Hạng mục | Trạng thái | Ghi chú |
|----------|:---:|---------|
| Dockerfiles (6 services) | ✅ Sẵn sàng | Multi-stage builds |
| docker-compose dev/prod | ✅ Sẵn sàng | Đã cập nhật env vars |
| Health Checks `/health` | ✅ Sẵn sàng | Tất cả 5 services |
| CI Pipeline (build+test) | ✅ Sẵn sàng | GitHub Actions |
| Unit Tests (6 projects) | ✅ Sẵn sàng | Auth, HR×3, Subscription, Order |
| DB Init Script | ✅ Sẵn sàng | `init-db/01-script.sql` |
| `.gitignore` + `.dockerignore` | ✅ Sẵn sàng | `.env` ignored |
| Serilog Logging | ✅ Sẵn sàng | Tất cả services đã có |
| Global Exception Handler | ✅ Sẵn sàng | `UseGlobalExceptionHandler()` |
| Swagger (dev-only) | ✅ Sẵn sàng | JWT auth + dev-only |
| HTTPS Redirection | ✅ Sẵn sàng | `UseHttpsRedirection()` |
| **Hardcoded Secrets** | ✅ **Đã fix** | Chuyển sang env vars |
| **CORS configurable** | ✅ **Đã fix** | `CorsOrigins` env var |
| **CRM thiếu CORS** | ✅ **Đã fix** | Thêm `AddCors` + `UseCors` |

---

## 🟡 CÒN LẠI — Việc nên làm trước deploy

### 1. ⚡ Rate Limiting (Chống DDoS/brute-force)

**Trạng thái**: ❌ Chưa có  
**Mức ưu tiên**: 🟡 Trung bình (nên có nhưng không chặn deploy)

Thêm ASP.NET Core Rate Limiting middleware cho tất cả services:
- Login endpoint: max 5 requests/phút  
- API chung: max 100 requests/phút/IP

**Files cần sửa**: Tất cả `Program.cs` (5 services)

---

### 2. 🏥 Mở rộng Health Checks

**Trạng thái**: ⚠️ Có nhưng cơ bản (chỉ check service alive)  
**Mức ưu tiên**: 🟡 Trung bình

Mở rộng health check để kiểm tra:
- Kết nối PostgreSQL
- Kết nối Redis (Identity, Sales)
- Cross-service connectivity

---

### 3. 🚀 CD Pipeline (Continuous Deployment)

**Trạng thái**: ❌ Chưa có  
**Mức ưu tiên**: 🟡 Trung bình (có thể deploy thủ công trước)

Cần tạo `.github/workflows/cd.yml`:  
- Build Docker images → Push registry  
- Auto-deploy staging/production  
- Phụ thuộc vào bạn chọn platform nào

---

### 4. 🔄 Rotate API Keys đã bị lộ

**Trạng thái**: ⚠️ Cần làm thủ công  
**Mức ưu tiên**: 🔴 Cao (bảo mật)

Các key sau đã từng được hardcode trong git history:
- Resend API Key
- VNPay TmnCode + HashSecret  
- JWT Secret Key
- Gemini API Key
- Cloudinary keys
- SePay keys

> [!WARNING]
> Mặc dù đã xóa khỏi code, **git history vẫn lưu giá trị cũ**. Nên rotate (tạo key mới) trên các dashboard tương ứng.

---

### 5. 🧪 Pre-Deploy Verification

Chạy kiểm tra trước deploy:

```bash
# 1. Unit tests
dotnet test 360Retail.sln --configuration Release

# 2. Docker build
docker compose build

# 3. Docker compose up (production mode)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# 4. Health check
curl http://localhost:5297/health  # Identity
curl http://localhost:5031/health  # Saas
curl http://localhost:5091/health  # Sales
curl http://localhost:5280/health  # HR
curl http://localhost:5169/health  # CRM
```

---

## ❓ Câu hỏi vẫn cần trả lời

1. **Platform deploy**: Azure / VPS / Railway / DigitalOcean?
2. **Domain**: `360retail-cortexa.online` sẽ dùng chính?
3. **SSL**: Let's Encrypt free?
4. **Budget**: Ngân sách hàng tháng?
5. **VNPay**: Giữ sandbox hay chuyển production?

---

## 🎯 Đề xuất thứ tự tiếp theo

| Bước | Việc | Thời gian ước tính |
|------|------|-------------------|
| **Bước 1** | Rotate API keys (thủ công) | 30 phút |
| **Bước 2** | Thêm Rate Limiting | 1-2 giờ |
| **Bước 3** | Mở rộng Health Checks | 30 phút |
| **Bước 4** | Chọn platform + tạo CD pipeline | 2-3 giờ |
| **Bước 5** | Pre-deploy verification | 1 giờ |
