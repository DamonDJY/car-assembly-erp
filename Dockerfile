# ============================================================
# 生产环境 Dockerfile（多阶段构建，只包含 .NET + Nginx）
# 数据库已拆分：生产用 Render PostgreSQL，开发用 docker-compose.yml
# ============================================================

# ---------- 阶段 1：构建 ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---------- 阶段 2：运行 ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview

ENV DEBIAN_FRONTEND=noninteractive
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# 安装 nginx、supervisor、curl（健康检查用）
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    nginx \
    supervisor \
    && rm -rf /var/lib/apt/lists/*

# 配置 nginx
COPY nginx.conf /etc/nginx/nginx.conf
RUN mkdir -p /var/log/nginx && rm -f /etc/nginx/sites-enabled/default

# 配置 supervisord（生产环境，无 PostgreSQL）
COPY supervisord.prod.conf /etc/supervisor/conf.d/supervisord.conf

# 复制发布后的应用
COPY --from=build /app/publish /app/publish

# 创建日志目录
RUN mkdir -p /var/log/supervisor

EXPOSE 80

CMD ["/usr/bin/supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
