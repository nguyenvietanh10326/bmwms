# Deployment Guide BMWMS - Report 6

**Document Version:** v1.0
**Date:** 12/08/2026
**Project Version:** App v1.0
**Target Environment:** Docker / Docker Compose
- Ubuntu 22.04 LTS (Host OS)
- Docker & Docker Compose
- SQL Server 2022 (Container)
- .NET 9 ASP.NET Core API & Web (Containers)

## 1. Prerequisites

### 1.1 Software Requirements
| Component | Required Version | Notes |
| :--- | :--- | :--- |
| OS | Ubuntu 22.04 LTS | Any OS supporting Docker can be used |
| Runtime | Docker & Docker Compose | Must be installed on the host server |

### 1.2 Hardware Recommendations
| Resource | Minimum | Recommended |
| :--- | :--- | :--- |
| CPU | 2 vCPU | 4 vCPU |
| RAM | 4 GB | 8 GB |
| Disk | 30 GB | 60 GB |
| Network | 10 Mbps | 100 Mbps |

### 1.3 Network Requirements
| Port | Protocol | Direction | Purpose |
| :--- | :--- | :--- | :--- |
| 22 | TCP | Inbound | SSH administration |
| 80 | TCP | Inbound | HTTP Web access (or reverse proxy) |
| 5123 | TCP | Inbound | BMWMS Web Application |
| 5076 | TCP | Inbound | BMWMS API |

---

## 2. Server Preparation

### 2.1 Update the System
```bash
sudo apt update && sudo apt upgrade -y
```

### 2.2 Install Docker and Docker Compose
```bash
# Install Docker
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Verify Docker
sudo docker --version
sudo docker compose version
```

### 2.3 Create the Directory Structure
```bash
sudo mkdir -p /opt/bmwms/
sudo chown -R $USER:$USER /opt/bmwms
```

---

## 3. Application Deployment using Docker Compose

### 3.1 Copy Files to the Server
Upload the project source code or required Docker files to the server at `/opt/bmwms/`.
You need at least the following files:
- `docker-compose.yml`
- `BMWMS.API/Dockerfile` and `BMWMS.Web/Dockerfile` (if building from source)
- The source code folders (`BMWMS.API`, `BMWMS.Web`, `BMWMS.Business`, `BMWMS.Repository`)

### 3.2 Update Environment Variables in docker-compose.yml
Ensure that your `docker-compose.yml` uses secure passwords. Edit `/opt/bmwms/docker-compose.yml` and replace `Your_password123` with a strong random password for the `db` and `api` services.

### 3.3 Build and Start the Containers
Navigate to the project directory and run the deployment:
```bash
cd /opt/bmwms

# Build and run containers in detached mode
docker compose up --build -d
```

### 3.4 Verify Deployment
Check the status of the containers:
```bash
docker compose ps
```
You should see 3 containers running (`db`, `api`, `web`).

Check the logs to ensure there are no startup errors:
```bash
docker compose logs -f
```

---

## 4. Database Setup & Migrations

Since the deployment uses SQL Server inside a Docker container, the database schema and seed data must be applied.

### 4.1 Apply Schema and Seed Data
If the API does not auto-migrate on startup, you must run the provided SQL scripts (e.g. `BMWMS_Database_ToBe_v3.0.sql` or `SeedData.sql`) against the SQL Server container.

```bash
# Copy script into the DB container
docker cp Database_v3.sql $(docker compose ps -q db):/tmp/Database_v3.sql

# Execute script
docker compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -i /tmp/Database_v3.sql
```

---

## 5. Reverse Proxy Configuration (Optional but Recommended)

For production, use Nginx as a reverse proxy to handle HTTPS and route traffic to the Web UI (port `5123`) and API (port `5076`).

### 5.1 Install Nginx
```bash
sudo apt install -y nginx
```

### 5.2 Configure Virtual Host
```bash
sudo nano /etc/nginx/sites-available/bmwms
```

Example config for the Web App:
```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://127.0.0.1:5123;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site:
```bash
sudo ln -s /etc/nginx/sites-available/bmwms /etc/nginx/sites-enabled/
sudo systemctl reload nginx
```

---

## 6. Smoke Test

### 6.1 Web App Access
Open a browser and navigate to `http://your-domain.com/` (or `http://SERVER_IP:5123`). The BMWMS login page should appear.

### 6.2 API Access
Test the API Swagger UI by navigating to `http://SERVER_IP:5076/swagger`.

### 6.3 Sign-In Works
Sign in with the default admin credentials (if seeded). You should be redirected to the dashboard.

---

## 7. Backup & Recovery

### 7.1 Database Backup
Create a cron job to automatically backup the SQL Server database.
```bash
#!/bin/bash
# backup.sh
BACKUP_DIR="/opt/bmwms/backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
CONTAINER_NAME="bmwms-db-1"

docker exec $CONTAINER_NAME /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "BACKUP DATABASE [BMWMS] TO DISK = N'/var/opt/mssql/backup_$TIMESTAMP.bak' WITH NOFORMAT, NOINIT, NAME = 'BMWMS-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

docker cp $CONTAINER_NAME:/var/opt/mssql/backup_$TIMESTAMP.bak $BACKUP_DIR/
docker exec $CONTAINER_NAME rm /var/opt/mssql/backup_$TIMESTAMP.bak
```

---

## 8. Updating the Application

To deploy a new version of the application:
```bash
cd /opt/bmwms
git pull origin main

# Rebuild and restart the containers
docker compose up -d --build
```

To rollback:
```bash
docker compose down
git checkout <previous_commit_hash>
docker compose up -d --build
```
