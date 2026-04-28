FROM mcr.microsoft.com/dotnet/sdk:10.0-preview

ENV DEBIAN_FRONTEND=noninteractive
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    gnupg \
    nginx \
    supervisor \
    && rm -rf /var/lib/apt/lists/*

# Install PostgreSQL 17
RUN curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /usr/share/keyrings/postgresql.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/postgresql.gpg] https://apt.postgresql.org/pub/repos/apt noble-pgdg main" > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y postgresql-17 postgresql-client-17 \
    && rm -rf /var/lib/apt/lists/*

# Setup PostgreSQL
RUN mkdir -p /var/run/postgresql && chown postgres:postgres /var/run/postgresql

# Configure nginx
COPY nginx.conf /etc/nginx/nginx.conf
RUN mkdir -p /var/log/nginx && rm -f /etc/nginx/sites-enabled/default

# Configure supervisord
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Copy startup script
COPY start.sh /start.sh
RUN chmod +x /start.sh

# Copy application and build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

# Create necessary log directories
RUN mkdir -p /var/log/supervisor

# Expose ports
EXPOSE 80 5432

CMD ["/start.sh"]
