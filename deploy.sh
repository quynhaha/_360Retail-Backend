#!/bin/bash
# ===========================================
# 360Retail Production Deploy Script
# Usage: bash deploy.sh
# ===========================================

set -e

COMPOSE_FILES="-f docker-compose.yml -f docker-compose.prod.yml"
DB_CONTAINER="360retail-db"
DB_USER="postgres"
DB_PASSWORD="12345"
DB_NAME="360RetailDB"

echo "========================================="
echo " 360Retail Production Deploy"
echo " $(date '+%Y-%m-%d %H:%M:%S')"
echo "========================================="

# 1. Pull latest code
echo ""
echo "[1/5] Pulling latest code..."
git pull origin main

# 2. Build & start all services
echo ""
echo "[2/5] Building and starting services..."
docker compose $COMPOSE_FILES up -d --build

# 3. Wait for PostgreSQL to be ready
echo ""
echo "[3/5] Waiting for PostgreSQL..."
for i in {1..30}; do
    if docker exec $DB_CONTAINER pg_isready -U $DB_USER > /dev/null 2>&1; then
        echo "  PostgreSQL is ready!"
        break
    fi
    echo "  Waiting... ($i/30)"
    sleep 2
done

# 4. Sync DB password (prevents desync after volume recreation)
echo ""
echo "[4/5] Syncing database password..."
docker exec $DB_CONTAINER psql -U $DB_USER -c "ALTER USER $DB_USER PASSWORD '$DB_PASSWORD';" 2>/dev/null && \
    echo "  Password synced successfully!" || \
    echo "  Warning: Could not sync password"

# 5. Run init/migration script (safe - uses IF NOT EXISTS)
echo ""
echo "[5/5] Running database migrations..."
docker exec -i $DB_CONTAINER psql -U $DB_USER -d $DB_NAME < init-db/01-script.sql 2>/dev/null && \
    echo "  Migrations applied!" || \
    echo "  Warning: Could not run migrations"

# Health check
echo ""
echo "========================================="
echo " Checking service health..."
echo "========================================="
sleep 3

services=("360retail-identity-api" "360retail-saas-api" "360retail-sales-api" "360retail-hr-api" "360retail-crm-api" "360retail-api-gateway")
all_healthy=true

for svc in "${services[@]}"; do
    status=$(docker inspect --format='{{.State.Status}}' $svc 2>/dev/null || echo "not found")
    if [ "$status" = "running" ]; then
        echo "  ✅ $svc: running"
    else
        echo "  ❌ $svc: $status"
        all_healthy=false
    fi
done

echo ""
if [ "$all_healthy" = true ]; then
    echo "🎉 Deploy successful! All services running."
else
    echo "⚠️  Deploy completed but some services have issues."
    echo "   Check logs: docker logs <container-name> --tail 50"
fi

echo ""
echo "========================================="
echo " Deploy finished at $(date '+%H:%M:%S')"
echo "========================================="
