# Software Requirements Specification (SRS)
## Project: Building Materials Warehouse Management System (BMWMS)

> [!WARNING]
> Tài liệu này đang ở trạng thái Draft. Các thành viên trong nhóm cần review và chốt lại các yêu cầu nghiệp vụ (Business Requirements) trước khi chuyển sang giai đoạn thiết kế.

## 1. Tổng quan dự án (Project Overview)
**Tên dự án:** Hệ thống quản lý kho vật liệu xây dựng (BMWMS).
**Mục tiêu:** Xây dựng phần mềm quản lý kho chuyên biệt cho các đại lý tư nhân phân phối vật liệu xây dựng, giải quyết các bài toán đặc thù của ngành mà các phần mềm kho thông thường không đáp ứng được.

## 2. Đối tượng người dùng (Actors)
*(Chờ nhóm bổ sung)*
- **Admin / Chủ đại lý:** Quản trị toàn bộ hệ thống, xem báo cáo doanh thu, công nợ, tồn kho.
- **Thủ kho:** Thực hiện nhập xuất kho thực tế, kiểm kê.
- **Nhân viên kinh doanh / Kế toán:** Lên đơn hàng bán, theo dõi công nợ khách hàng (thợ thầu).

## 3. Các vấn đề nghiệp vụ đặc thù cần giải quyết (Domain Specifics)
> [!IMPORTANT]
> Đây là phần phân biệt đồ án của các bạn với một phần mềm kho "đồ chơi". Nhóm cần thảo luận và chốt xem có đưa các bài toán này vào scope không:
1. **Quy đổi đơn vị tính động (Dynamic Unit of Measurement):** Nhập bằng Tấn, bán bằng Kg, Cây (Thép). Nhập bằng Thùng, bán bằng Viên, m2 (Gạch).
2. **Hao hụt & Lưu trữ:** Cát/đá bán theo khối (m3) xúc bằng xe rùa sẽ có tỷ lệ hao hụt.
3. **Phân phối & Vận chuyển:** Đơn hàng lớn phải chia làm nhiều chuyến giao. Quản lý luồng xuất kho theo từng chuyến xe tải.
4. **Công nợ (Debt):** Đặc thù bán VLXD ở VN là nợ gối đầu rất nhiều. Có quản lý phần này không?

## 4. Phạm vi hệ thống (System Scope - Đề xuất)
*(Cần cắt giảm nếu thấy quá sức với 5 người trong 3 tháng)*
1. **Module Quản lý Danh mục (Catalog Management)**
2. **Module Quản lý Kho & Tồn kho (Inventory & Warehouse)**
3. **Module Nhập hàng (Purchasing)**
4. **Module Bán hàng & Xuất kho (Sales & Fulfillment)**
5. *(Tùy chọn)* **Module Công nợ (Debt/Receivables)**

## 5. Yêu cầu phi chức năng (Non-Functional Requirements)
- **Công nghệ (Tech Stack):** 
  - Backend: ASP.NET Core Web API, SQL Server.
  - Frontend: ASP.NET Core Razor Pages (hoặc React/Vue/Angular - cần chốt lại).
- **Công cụ quản lý:** GitHub.
