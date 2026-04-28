#!/bin/bash
set -e

# Initialize PostgreSQL data directory if needed
if [ ! -s /var/lib/postgresql/17/main/PG_VERSION ]; then
    echo "Initializing PostgreSQL data directory..."
    mkdir -p /var/lib/postgresql/17/main
    chown -R postgres:postgres /var/lib/postgresql/17/main
    su - postgres -c "/usr/lib/postgresql/17/bin/initdb -D /var/lib/postgresql/17/main"
fi

# Configure PostgreSQL to listen on all interfaces and use trust auth
su - postgres -c "echo \"listen_addresses = '*'\" >> /var/lib/postgresql/17/main/postgresql.conf" || true

# Update pg_hba.conf to allow local trust connections
su - postgres -c "cat > /var/lib/postgresql/17/main/pg_hba.conf << 'EOF'
local   all             all                                     trust
host    all             all             127.0.0.1/32            trust
host    all             all             ::1/128                 trust
host    all             all             0.0.0.0/0               trust
EOF"

chown -R postgres:postgres /var/lib/postgresql/17/main

# Start PostgreSQL temporarily to create user and database
su - postgres -c "/usr/lib/postgresql/17/bin/pg_ctl -D /var/lib/postgresql/17/main -l /var/lib/postgresql/17/main/logfile start"

# Wait for PostgreSQL to be ready
sleep 2

# Ensure password is set and database exists
su - postgres -c "psql -c \"ALTER USER postgres WITH PASSWORD 'postgres';\"" || true
su - postgres -c "psql -c \"CREATE DATABASE CarAssemblyErp;\"" || true

su - postgres -c "/usr/lib/postgresql/17/bin/pg_ctl -D /var/lib/postgresql/17/main stop"

# Start all services via supervisord
exec /usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf
