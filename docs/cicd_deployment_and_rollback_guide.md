# Huong Dan Chi Tiet CI/CD: Build, Deploy, Phe Duyet Nut Bam & Rollback

Tai lieu nay huong dan toan dien ve quy trinh CI/CD cua he thong BMWMS tren GitHub Actions va Server Linux/VPS:

1. **Kien truc he thong CI/CD**: Build image, day len Docker Hub, co che quyet dinh deploy bang nut bam, va co che tu dong phuc hoi (auto-rollback).
2. **Thiet lap Secrets va Variables** tren GitHub Actions.
3. **Co che nut bam phe duyet truoc khi Deploy** (co the bat/tat tuy chon nay).
4. **Deploy chu dong bang nut bam** (Manual Trigger qua workflow_dispatch).
5. **Co che Tu Dong Rollback khi Deploy gap su co** (Auto-Rollback on Deploy Failure).
6. **Huong dan Rollback chu dong** (Qua nut bam GitHub hoac qua SSH vao Server).

---

## 1. Kien Truc Tong Quan Luong CI/CD

```
[Developer]
    |
    |-- Push code vao branch `master` --------------------+
    |                                                     |
    |-- Bam nut "Run workflow" (workflow_dispatch) -------+
                                                          |
                                           [GitHub Actions Workflow]
                                                          |
                     +------------------------------------+------------------------------------+
                     |                                                                         |
        [Truong hop Rollback nut bam]                                            [Luong Build va Deploy thuong]
     (Nhap rollback_tag tren giao dien)                                                        |
                     |                                                            [Job 1: Build and Push]
                     |                                                            - Build API & Web Docker images
                     |                                                            - Tag: :latest, :<timestamp>, :<sha>
                     |                                                            - Push len Docker Hub
                     |                                                                         |
                     |                                                        +----------------+----------------+
                     |                                                        |                                 |
                     |                                              [Neu bat nut bam duyet]          [Neu tat nut bam duyet]
                     |                                              (Environment: production)       (Tu dong chay tiep)
                     |                                                        |                                 |
                     |                                              Tam dung pipeline,                  Chay luon
                     |                                              hien nut "Review deployments"       khong can hoi
                     |                                                        |                                 |
                     |                                                        +----------------+----------------+
                     |                                                                         |
                     +------------------------------------------------------------> [Job 2: Deploy to Server]
                                                                                    - SSH vao server VPS
                                                                                    - Luu phien ban cu vao .last_image_tag
                                                                                    - Cap nhat IMAGE_TAG trong .env
                                                                                    - docker compose pull & up -d
                                                                                    - Kiem tra Health Check containers
                                                                                               |
                                                                              +----------------+----------------+
                                                                              |                                 |
                                                                         [Thanh cong]                       [That bai]
                                                                              |                                 |
                                                                      Hoan tat deploy            [Step: Auto-Rollback]
                                                                                                 - Doc lai .last_image_tag
                                                                                                 - Khoi phuc .env ve tag cu
                                                                                                 - docker compose up -d lai
```

---

## 2. Thiet Lap Secrets & Variables tren GitHub

Vao **GitHub Repository** -> **Settings** -> **Secrets and variables** -> **Actions**.

### 2.1. Repository Secrets (Bao mat)
Chon **New repository secret**:

| Ten Secret | Bat buoc | Mo ta | Vi du |
| :--- | :---: | :--- | :--- |
| `DOCKERHUB_USERNAME` | Co | Ten tai khoan dang nhap Docker Hub | `vietanh103` |
| `DOCKERHUB_TOKEN` | Co | Personal Access Token tao tu Docker Hub | `dckr_pat_xxx...` |
| `SERVER_HOST` | Co | Dia chi IP cong khai hoac domain cua Server/VPS | `103.153.xx.xx` |
| `SERVER_USER` | Co | Ten tai khoan SSH tren server | `ubuntu` hoac `root` |
| `SERVER_SSH_KEY` | Khuyen dung | Private SSH Key de dang nhap khong can mat khau | `-----BEGIN OPENSSH PRIVATE KEY...` |
| `SERVER_PASSWORD` | Tuy chon | Mat khau SSH (chi dung khi khong dung SSH Key) | `MySecretPass123` |
| `SERVER_PORT` | Tuy chon | Cong SSH cua server (mac dinh la 22) | `22` |

### 2.2. Repository Variables (Bien cau hinh)
Chon tab **Variables** -> **New repository variable**:

| Ten Variable | Bat buoc | Mo ta | Gia tri mac dinh |
| :--- | :---: | :--- | :--- |
| `DOCKERHUB_NAMESPACE` | Tuy chon | Namespace chua repository tren Docker Hub | Mac dinh lay owner repo |
| `SERVER_DEPLOY_PATH` | Tuy chon | Thu muc chua ma nguon/docker-compose tren server | `~/bmwms` |
| `AUTO_DEPLOY` | Tuy chon | Tu dong deploy khi push code len master (true/false) | `false` (Chi build, khong tu deploy) |

---

## 3. Co Che Nut Bam Quyet Dinh Deploy (Co The Bat Hoac Tat)

De tranh viec deploy nham len production khi chua san sang, he thong duoc thiet ke de nguoi quan tri co quyen quyet dinh khi nao deploy. Co 3 cach su dung:

### 3.1. Cach 1: Nut bam phe duyet truc tiep tren man hinh Workflow (GitHub Environment)
Job deploy duoc gan vao `environment: production`.

* **De BAT tuy chon nut bam duyet:**
  1. Vao **Settings** tren GitHub repo -> muc **Environments**.
  2. Nhan **New environment** -> dat ten la `production` (neu chua co).
  3. Trong muc **Deployment protection rules**, tich chon **Required reviewers**.
  4. Them tai khoan GitHub cua ban (hoac team phu trach) -> Nhan **Save protection rules**.
* **Cach hoat dong khi bat:**
  - Moi khi workflow chay xong buoc Build and Push, pipeline se tam dung o trang thai *Waiting*.
  - Tren giao dien GitHub se hien nut **Review deployments**.
  - Ban nhan vao nut nay, tich chon moi truong `production` va bam **Approve and deploy**.
  - Chi khi do, lenh SSH deploy moi duoc phep thuc thi tren server.
* **De TAT tuy chon nut bam duyet:**
  - Vao lai **Settings** -> **Environments** -> `production` -> bo tich muc **Required reviewers**. Khi do buoc deploy se chay luon ma khong can bam nut phe duyet.

### 3.2. Cach 2: Tuy chon Deploy khi bam "Run workflow" thu cong
Khi vao tab **Actions** -> Chon **CI/CD Pipeline - Build, Push & Deploy** -> Bam nut **Run workflow**:
* O input **deploy**:
  - Tich chon `[x]` (true): Cho phep tien hanh deploy len server sau khi build xong.
  - Bo tich `[ ]` (false): Chi thuc hien build va day image len Docker Hub, bo qua hoan toan buoc deploy.

### 3.3. Cach 3: Kiem soat hanh vi khi Push code vao nhanh `master`
* **Mac dinh an toan**: Khi co commit moi tren `master`, workflow **chi thuc hien Build va Push Docker image**, khong tu y can thiep vao server production.
* **Neu muon tu dong deploy khi push**: Vao Settings -> Variables -> dat bien `AUTO_DEPLOY` co gia tri la `true`.

---

## 4. Co Che Tu Dong Rollback Khi Deploy Gap Su Co (Auto-Rollback)

Trong qua trinh deploy len Server, neu container gap loi khoi dong (vi du loi cau hinh, loi database connection, crash app...):

1. **Tu dong luu tag cu**: Truoc khi thay doi `.env`, script deploy se tu dong sao luu tag hien tai dang on dinh vao file `.last_image_tag`.
2. **Kiem tra Health Check**: Sau khi chay `docker compose up -d`, script cho 15 giay va kiem tra trang thai cua tat ca containers qua `docker compose ps`.
3. **Phat hien su co**: Neu co bat ky container nao khong o trang thai `running`, buoc deploy se bao loi (`exit 1`).
4. **Kich hoat Auto-Rollback**:
   - Step `Auto-Rollback on Deploy Failure` tu dong chay (su dung dieu kien `if: failure()`).
   - Doc nguoc lai tag cu tu `.last_image_tag`.
   - Ghi lai tag cu vao file `.env`.
   - Khoi dong lai toan bo containers ve phien ban on dinh truoc do.
   - Tranh hoan toan tinh trang he thong bi chet (downtime) do loi phien ban moi.

---

## 5. Huong Dan Rollback Chu Dong (Khi Phat Hien Loi Nghiep Vu)

Neu ban deploy thanh cong nhung trong qua trinh su dung nguoi dung phat hien loi logic/nghiep vu va muon quay ve phien ban truoc do, ban co the chon 1 trong 2 cach sau:

### 5.1. Cach A: Rollback bang nut bam tren giao dien GitHub (Khong can dung Terminal)
1. Vao tab **Actions** tren GitHub repo.
2. Chon workflow **CI/CD Pipeline - Build, Push & Deploy**.
3. Nhan nut **Run workflow**:
   - O o **Rollback to specific tag**: Nhap tag muon quay ve (Vi du: `20260920-143000` hoac `b50365d`).
   - Nhan nut xanh **Run workflow**.
4. Workflow se **bo qua hoan toan buoc build**, ket noi thang vao server, cap nhat `.env` va khoi dong lai ung dung ve tag da chi dinh trong vong duoi 10 giay.

### 5.2. Cach B: Rollback truc tiep qua SSH bang script `rollback.sh`
1. Mo terminal va SSH vao server:
   ```bash
   ssh ubuntu@<SERVER_HOST>
   cd ~/bmwms
   ```
2. Chay script rollback:
   * **Cach chon tu danh sach image co san tren server:**
     ```bash
     chmod +x scripts/rollback.sh
     ./scripts/rollback.sh
     ```
     Script se liet ke cac image da tai ve, ban chi can go tag muon quay ve va Enter.
   * **Cach truyen truc tiep tag:**
     ```bash
     ./scripts/rollback.sh 20260920-143000
     ```

### 5.3. Cach C: Rollback thu cong bang Docker Compose
```bash
cd ~/bmwms
# 1. Sua tag trong file .env ve tag cu
sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=20260920-143000/' .env

# 2. Khoi dong lai containers
docker compose -f docker-compose.app.yml up -d

# 3. Kiem tra trang thai
docker compose -f docker-compose.app.yml ps
```

---

## 6. Bang Tra Cuu Lenh Nhanh Tren Server

| Yeu cau | Lenh thuc thi |
| :--- | :--- |
| Xem trang thai containers | `docker compose -f docker-compose.app.yml ps` |
| Xem log realtime he thong | `docker compose -f docker-compose.app.yml logs -f --tail=100` |
| Rollback nhanh ve 1 tag | `./scripts/rollback.sh <TAG>` |
| Khoi dong lai toan bo app | `docker compose -f docker-compose.app.yml restart` |
| Xem danh sach image da pull | `docker images \| grep bmwms` |
| Don dep cac image cu khong dung | `docker image prune -f` |
