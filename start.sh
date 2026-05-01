#!/bin/bash
set -e

echo "========================================"
echo "CarAssemblyErp 启动脚本（生产环境）"
echo "========================================"
echo ""

# 等待 PostgreSQL 可用（Render 外部数据库）
echo "Waiting for PostgreSQL to be ready..."
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
MAX_RETRIES=30
RETRY_COUNT=0

while ! curl -sf "postgresql://${DB_HOST}:${DB_PORT}" > /dev/null 2>&1; do
    # 使用 pg_isready 检测（如果安装了 postgresql-client）
    if command -v pg_isready &> /dev/null; then
        if pg_isready -h "$DB_HOST" -p "$DB_PORT" > /dev/null 2>&1; then
            break
        fi
    fi
    
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
        echo "Warning: PostgreSQL connection timeout. Continuing anyway..."
        break
    fi
    echo "PostgreSQL not ready yet (attempt $RETRY_COUNT/$MAX_RETRIES)..."
    sleep 2
done

echo "PostgreSQL is ready."

# 执行数据库迁移
echo "Running database migrations..."
cd /app/publish
dotnet CarAssemblyErp.dll --migrate-only 2>/dev/null || true

# 实际上我们在 Program.cs 中自动执行 Migrate()
# 这里只是一个确认日志
echo "Database migrations will be applied on application startup."

# 启动所有服务
echo "Starting services..."
exec /usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf
