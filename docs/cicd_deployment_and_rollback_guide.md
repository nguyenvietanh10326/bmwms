# Hướng Dẫn Chi Tiết CI/CD: Deploy Bằng Nút Bấm & Rollback Bằng SSH Vào Server

Tài liệu này hướng dẫn đầy đủ quy trình:
1. **Thiết lập Secrets & Variables** trên GitHub Actions.
2. **Deploy bằng nút bấm** trên giao diện GitHub (Manual Trigger).
3. **Rollback hệ thống khi có sự cố** bằng cách SSH trực tiếp vào Server (thực thi trong vòng 10 giây).

---

## 1. Kiến trúc luồng CI/CD & Deploy

```
[Developer]
    │
    ├─ (1) Push lên branch `master`  ────────┐
    │                                        ├──► [GitHub Actions Workflow]
    └─ (2) Bấm nút "Run workflow" (Manual) ──┘        │
                                                      ├─► [Job 1: Build & Push Docker Hub]
                                                      │     Tags: :latest, :<timestamp>, :<sha>
                                                      │
                                                      └─► [Job 2: SSH Deploy to Server]
                                                            ├─► SSH vào Server
                                                            ├─► Cập nhật IMAGE_TAG trong .env
                                                            ├─► docker compose pull
                                                            └─► docker compose up -d (Zero Downtime)
```

---

## 2. Thiết lập Secrets & Variables trên GitHub

Trước khi sử dụng nút bấm hoặc đẩy code tự động deploy, bạn cần thiết lập các thông số sau trên GitHub repository:

Vào **GitHub Repo** $\rightarrow$ **Settings** $\rightarrow$ **Secrets and variables** $\rightarrow$ **Actions**.

### 2.1. Repository Secrets (Bảo mật)
Nhấn **New repository secret** để thêm:

| Tên Secret | Bắt buộc | Mô tả | Ví dụ |
| :--- | :---: | :--- | :--- |
| `DOCKERHUB_USERNAME` | **Có** | Username đăng nhập Docker Hub | `vietanh103` |
| `DOCKERHUB_TOKEN` | **Có** | Access Token từ Docker Hub *(Account Settings $\rightarrow$ Security $\rightarrow$ New Access Token)* | `dckr_pat_xxx...` |
| `SERVER_HOST` | **Có** | Địa chỉ IP công khai hoặc Domain của VPS / Server | `103.153.xx.xx` |
| `SERVER_USER` | **Có** | Tên tài khoản SSH trên server | `ubuntu` hoặc `root` |
| `SERVER_SSH_KEY` | Khuyên dùng | Private SSH Key để đăng nhập server (nội dung file `id_rsa` / `id_ed25519`) | `-----BEGIN OPENSSH PRIVATE KEY...` |
| `SERVER_PASSWORD` | Tùy chọn | Mật khẩu SSH (chỉ cần nếu không dùng SSH Key) | `MySecretPass123` |
| `SERVER_PORT` | Tùy chọn | Cổng SSH (mặc định là `22`) | `22` |

### 2.2. Repository Variables (Cấu hình chung)
Chuyển sang tab **Variables** $\rightarrow$ nhấn **New repository variable**:

| Tên Variable | Bắt buộc | Mô tả | Giá trị mặc định |
| :--- | :---: | :--- | :--- |
| `DOCKERHUB_NAMESPACE` | Tùy chọn | Namespace trên Docker Hub (thường là username hoặc tổ chức) | Mặc định lấy theo repo owner |
| `SERVER_DEPLOY_PATH` | Tùy chọn | Thư mục chứa project trên Server | `~/bmwms` |

---

## 3. Hướng dẫn Deploy Bằng "Nút Bấm" (Manual Trigger)

Khi bạn muốn chủ động deploy một phiên bản mới lên Server mà không cần push commit:

### Các bước thực hiện:
1. Truy cập vào GitHub repository của dự án.
2. Nhấn vào tab **Actions** trên thanh điều hướng phía trên.
3. Ở thanh danh sách bên trái, chọn workflow: **`CI/CD Pipeline - Build, Push & Deploy`**.
4. Bạn sẽ thấy một banner màu xanh xuất hiện bên phải với nút **`Run workflow`**:
   ![Run workflow](https://docs.github.com/assets/cb-32007/mw-1440/images/help/actions/workflow-dispatch.webp)
5. Nhấp vào nút **`Run workflow`**, một bảng điều khiển sẽ mở ra với các tùy chọn:
   - **Use workflow from**: Chọn nhánh muốn build (mặc định: `master`).
   - **Deploy lên Server sau khi build & push**: Đánh dấu tích `[x]` (mặc định là bật). Nếu chỉ muốn build & push image lên Docker Hub mà chưa muốn deploy thì bỏ tích.
   - **Custom Image Tag**:
     - *Để trống*: Hệ thống tự động tạo tag dạng `YYYYMMDD-HHmmss` (ví dụ: `20260920-143000`) và gắn thêm tag `latest`.
     - *Nhập tag*: Bạn có thể điền tag tùy chỉnh (ví dụ: `v1.2.0` hoặc `hotfix-1`).
6. Nhấn nút xanh **`Run workflow`** ở dưới cùng bảng điều khiển để bắt đầu.
7. Bạn có thể nhấp vào lượt chạy đang thực thi để xem trực tiếp logs từng bước:
   - Bước 1: Build Docker images `bmwms-api` và `bmwms-web`, đẩy lên Docker Hub.
   - Bước 2: SSH kết nối vào VPS, kéo image mới về và khởi chạy container.

---

## 4. Hướng dẫn Rollback Bằng Cách SSH Vào Server

Khi bản deploy mới gặp sự cố ngoài ý muốn trên production, bạn có thể rollback về phiên bản trước **ngay lập tức** thông qua SSH mà không cần phải chờ GitHub Actions build lại.

### 4.1. Cách 1: Sử dụng Script tự động `rollback.sh` (Nhanh nhất & Tiện lợi nhất)

Script [`scripts/rollback.sh`](file:///c:/bmwms/scripts/rollback.sh) đã được tích hợp sẵn trong thư mục dự án trên server.

**Bước 1: SSH vào Server:**
```bash
ssh <SERVER_USER>@<SERVER_HOST>
# Ví dụ: ssh ubuntu@103.153.xx.xx
```

**Bước 2: Di chuyển vào thư mục dự án:**
```bash
cd ~/bmwms
```

**Bước 3: Chạy script rollback:**
- **Cách A - Tương tác (Xem danh sách tag trước rồi chọn):**
  ```bash
  chmod +x scripts/rollback.sh
  ./scripts/rollback.sh
  ```
  Script sẽ in ra bảng danh sách các image đang có sẵn trên máy:
  ```text
  📋 Danh sách các Docker Image BMWMS hiện có trên server:
  REPOSITORY              TAG                  CREATED              SIZE
  nguyenvietanh10326/bmwms-api   20260920-143000      10 minutes ago       250MB
  nguyenvietanh10326/bmwms-api   20260920-120000      2 hours ago          248MB
  nguyenvietanh10326/bmwms-api   b50365d              3 hours ago          248MB

  👉 Nhập Image Tag bạn muốn rollback về: 20260920-120000
  ```
- **Cách B - Truyền tag trực tiếp:**
  ```bash
  ./scripts/rollback.sh 20260920-120000
  ```

Script sẽ tự động cập nhật `IMAGE_TAG` trong `.env`, khởi động lại containers trong khoảng **3 - 5 giây**!

---

### 4.2. Cách 2: Rollback Thủ Công Bằng Lệnh Docker Compose

Nếu bạn muốn tự tay thao tác từng lệnh:

**Bước 1: SSH vào server và vào thư mục dự án:**
```bash
ssh ubuntu@<SERVER_HOST>
cd ~/bmwms
```

**Bước 2: Xem các tag image đã từng tải về:**
```bash
docker images | grep bmwms
```

**Bước 3: Thay đổi `IMAGE_TAG` trong file `.env`:**
```bash
# Ví dụ muốn đổi về tag 20260920-120000:
sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=20260920-120000/' .env

# Hoặc dùng nano để mở và sửa trực tiếp:
nano .env
```

**Bước 4: Áp dụng phiên bản cũ cho containers:**
```bash
docker compose -f docker-compose.app.yml up -d
```

**Bước 5: Kiểm tra trạng thái và logs:**
```bash
# Xem trạng thái running của containers
docker compose -f docker-compose.app.yml ps

# Xem log kiểm tra lỗi
docker compose -f docker-compose.app.yml logs -f --tail=50
```

---

### 4.3. Cách 3: Rollback Cơ Sở Dữ Liệu (Nếu có migration SQL đi kèm)

Nếu bản deploy vừa rồi có chạy file migration SQL thay đổi cấu trúc bảng:
1. Kết nối vào SQL Server container:
   ```bash
   docker exec -it $(docker ps -qf "name=db") /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C
   ```
2. Thực thi script rollback tương ứng trong thư mục `docs/migrations/` (nếu có chuẩn bị file rollback riêng).

---

## 5. Tóm tắt các lệnh cần nhớ khi SSH vào Server

| Mục đích | Lệnh thực thi |
| :--- | :--- |
| **Xem trạng thái hệ thống** | `docker compose -f docker-compose.app.yml ps` |
| **Xem log trực tiếp** | `docker compose -f docker-compose.app.yml logs -f --tail=100` |
| **Rollback nhanh 1 lệnh** | `./scripts/rollback.sh <TAG>` |
| **Khởi động lại toàn bộ app** | `docker compose -f docker-compose.app.yml restart` |
| **Dọn dẹp rác & image cũ** | `docker system prune -f` |
