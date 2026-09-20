#!/usr/bin/env bash
set -e

# Script Rollback nhanh BMWMS qua SSH
# Cách dùng:
#   1. ./scripts/rollback.sh <IMAGE_TAG>
#   2. ./scripts/rollback.sh              (Sẽ hiển thị danh sách tag gần nhất để chọn)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_DIR"

echo "======================================================"
echo "          BMWMS - QUY TRÌNH ROLLBACK HỆ THỐNG          "
echo "======================================================"
echo "Thư mục dự án: $PROJECT_DIR"

if [ ! -f .env ]; then
  echo "❌ Lỗi: Không tìm thấy file .env tại $PROJECT_DIR."
  exit 1
fi

CURRENT_TAG=$(grep "^IMAGE_TAG=" .env | cut -d '=' -f2 | tr -d ' ' || echo "chưa xác định")
echo "📌 Image Tag hiện tại trong .env: $CURRENT_TAG"
echo ""

TARGET_TAG="$1"

if [ -z "$TARGET_TAG" ]; then
  echo "📋 Danh sách các Docker Image BMWMS hiện có trên server:"
  docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.CreatedAt}}\t{{.Size}}" | grep -E "REPOSITORY|bmwms" || true
  echo ""
  read -r -p "👉 Nhập Image Tag bạn muốn rollback về (ví dụ: 20260920-120000 hoặc b50365d): " TARGET_TAG
fi

if [ -z "$TARGET_TAG" ]; then
  echo "❌ Chưa nhập Image Tag. Đã hủy thao tác rollback."
  exit 1
fi

echo ""
echo "⏳ Bắt đầu rollback hệ thống về phiên bản Tag: [$TARGET_TAG]..."

# Cập nhật IMAGE_TAG vào .env
if grep -q "^IMAGE_TAG=" .env; then
  sed -i "s/^IMAGE_TAG=.*/IMAGE_TAG=$TARGET_TAG/" .env
else
  echo "IMAGE_TAG=$TARGET_TAG" >> .env
fi

echo "✅ Đã cập nhật IMAGE_TAG=$TARGET_TAG trong file .env."

# Khởi động lại containers với image tag chỉ định
echo "🚀 Đang áp dụng phiên bản mới cho containers..."
docker compose -f docker-compose.app.yml pull || {
  echo "⚠️ Cảnh báo: Không thể pull image từ Docker Hub (có thể dùng image có sẵn ở local cache)."
}

docker compose -f docker-compose.app.yml up -d --remove-orphans

echo ""
echo "======================================================"
echo "🎉 ROLLBACK HOÀN TẤT!"
echo "======================================================"
echo "Trạng thái các container:"
docker compose -f docker-compose.app.yml ps
echo ""
echo "Để theo dõi logs xem hệ thống đã sẵn sàng chưa, chạy lệnh:"
echo "  docker compose -f docker-compose.app.yml logs -f --tail=50"
