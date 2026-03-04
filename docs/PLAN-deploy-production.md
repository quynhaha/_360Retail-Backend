# 🚀 PLAN: Deploy 360Retail Backend lên DigitalOcean

## Tổng quan

Deploy hệ thống 360Retail Backend lên **DigitalOcean Droplet** sử dụng Docker Compose + Nginx + Let's Encrypt. Frontend deploy riêng.

---

## Kiến trúc Production

```
Mobile App / Web Frontend
          │ HTTPS
          ▼
┌──────────────────────────────┐
│  Nginx (Reverse Proxy)       │  ← api.360retail.com (HTTPS)
│  + Let's Encrypt SSL         │
└──────────┬───────────────────┘
           │ HTTP (internal)
┌──────────▼───────────────────┐
│  API Gateway (:5001)         │
├──────────────────────────────┤
│  Identity API (:5297)        │
│  Sales API    (:5091)        │
│  Saas API     (:5031)        │
│  HR API       (:5280)        │
│  CRM API      (:5169)        │
├──────────────────────────────┤
│  PostgreSQL   (:5432)        │  ← Không expose ra ngoài
│  Redis        (:6379)        │  ← Không expose ra ngoài
└──────────────────────────────┘
     DigitalOcean Droplet
         4GB RAM / $24
```

> [!IMPORTANT]
> Docker Compose chạy **y hệt local** — chỉ thêm Nginx phía trước làm reverse proxy + SSL.

---

## Các bước thực hiện

### Phase 1: Chuẩn bị DigitalOcean (Thủ công trên web)
- [ ] Tạo tài khoản DigitalOcean
- [ ] Tạo Droplet Ubuntu 22.04, **4GB RAM / 2 vCPU** ($24/tháng)
- [ ] Setup SSH key
- [ ] Mua domain (hoặc dùng miễn phí từ Freenom/tạm dùng IP)

### Phase 2: Cấu hình Server (SSH vào Droplet)
- [ ] Cài Docker + Docker Compose trên Droplet
- [ ] Cài Nginx
- [ ] Cài Certbot (Let's Encrypt)
- [ ] Setup firewall (UFW): chỉ mở port 80, 443, 22

### Phase 3: Thay đổi code cho Production
- [ ] Cập nhật `docker-compose.prod.yml` — không expose ports ngoài gateway
- [ ] Thêm Nginx config file
- [ ] Tạo script deploy (`deploy.sh`)
- [ ] Cấu hình CORS cho frontend domain
- [ ] Tạo `.env.production` template

### Phase 4: Deploy
- [ ] Clone repo trên Droplet
- [ ] Copy `.env` production lên server
- [ ] Chạy `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d`
- [ ] Cấu hình Nginx reverse proxy → API Gateway
- [ ] Setup SSL với Certbot

### Phase 5: Kiểm tra & Xác minh
- [ ] Test HTTPS truy cập
- [ ] Test login API
- [ ] Test Google OAuth
- [ ] Test từ mobile app
- [ ] Kiểm tra auto-restart khi server reboot

---

## Thay đổi code cần thiết

### 1. [MODIFY] `docker-compose.prod.yml` — Bảo mật ports

Hiện tại file prod override chưa ẩn ports của các microservices. Cần cập nhật để **chỉ expose API Gateway port**.

```yaml
services:
  postgres:
    restart: always
    ports: []  # Không expose ra ngoài

  redis:
    restart: always
    ports: []

  identity-api:
    restart: always
    ports: []  # Chỉ truy cập qua internal network
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

  # ... tương tự cho các service khác

  api-gateway:
    restart: always
    ports:
      - "127.0.0.1:5001:8080"  # Chỉ listen localhost, Nginx proxy vào
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

### 2. [NEW] `nginx/api.conf` — Reverse Proxy config

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name api.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.yourdomain.com/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:5001;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # WebSocket support (cho SignalR notifications)
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

### 3. [NEW] `deploy.sh` — Script tự động deploy

```bash
#!/bin/bash
set -e
echo "🚀 Deploying 360Retail Backend..."

cd /opt/360retail
git pull origin dev

docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

echo "✅ Deploy completed!"
docker compose ps
```

### 4. [MODIFY] CORS Configuration

Thêm frontend production domain vào CORS allowed origins trong API Gateway hoặc từng service.

---

## Environment Variables (Production)

> [!CAUTION]
> Dùng **strong keys** cho production, KHÔNG dùng key development!

```env
ASPNETCORE_ENVIRONMENT=Production

# JWT - PHẢI đổi key mạnh hơn
JWT_SECRET_KEY=<generate-random-64-char-string>
INTERNAL_API_KEY=<generate-random-32-char-string>

# Cloudinary (giữ nguyên)
CLOUDINARY_CLOUD_NAME=dndfromwh
CLOUDINARY_API_KEY=229628221747212
CLOUDINARY_API_SECRET=BfH4CP-SKwndC_5zgyBcUzyPEXE

# Email (giữ nguyên)
RESEND_API_KEY=re_e1hdptzQ_5oqEXd7y7xoLFLyf6RqPZA5a
RESEND_FROM_EMAIL=no-reply@360retail-cortexa.online

# Google OAuth (giữ nguyên)
OAUTH_GOOGLE_CLIENT_ID=<web-id>,<mobile-id>
OAUTH_GOOGLE_CLIENT_SECRET=<secret>

# AI & Payment (giữ nguyên)
GEMINI_API_KEY=<key>
SEPAY_MERCHANT_ID=<id>
SEPAY_SECRET_KEY=<key>
```

---

## Chi phí

| Hạng mục | Giá/tháng |
|----------|-----------|
| DigitalOcean Droplet 4GB RAM | **$24** |
| Domain (tùy chọn) | ~$1/tháng ($12/năm) |
| **Tổng** | **~$25/tháng** |

> [!TIP]
> DigitalOcean thường có **$200 free credit 60 ngày** cho tài khoản mới → đủ dùng 2 tháng miễn phí!

---

## Verification Plan

### Automated Tests (chạy trước khi deploy)
```bash
dotnet test 360Retail.sln --configuration Release
```

### Manual Verification (sau khi deploy)

| # | Test | Expected |
|---|------|----------|
| 1 | `curl https://api.yourdomain.com/health` | 200 OK |
| 2 | Login API qua HTTPS | Trả về JWT token có `full_name` |
| 3 | Google OAuth từ mobile | Login thành công |
| 4 | Tạo sản phẩm + upload ảnh | Ảnh lưu Cloudinary OK |
| 5 | Reboot Droplet | Tất cả containers tự khởi động lại |

---

## Timeline

| Phase | Thời gian |
|-------|-----------|
| Phase 1: Tạo Droplet | 15 phút |
| Phase 2: Cài Docker + Nginx | 30 phút |
| Phase 3: Cập nhật code | 1 giờ |
| Phase 4: Deploy | 30 phút |
| Phase 5: Testing | 30 phút |
| **Tổng** | **~3 giờ** |
