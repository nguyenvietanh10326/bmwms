# BÁO CÁO ĐỐI SOÁT TOÀN DIỆN TÀI LIỆU SRS VÀ MÃ NGUỒN HỆ THỐNG BMWMS
**Tài liệu kiểm tra:** `BMWMS_Report3_SRS_Code_Based_20260920.docx`  
**Hệ thống đối soát:** Dự án BMWMS (Building Material Warehouse Management System)  
**Ngày rà soát:** 20/09/2026  
**Phạm vi:** Toàn bộ 91 Use Cases, CSDL, Kiến trúc Controller/Razor Page, Phân quyền & Nghiệp vụ

---

## 1. BẢN ĐỒ CÁC BÁO CÁO CHI TIẾT THEO PHÂN HỆ

Báo cáo này được chia thành từng file Markdown độc lập theo từng phân hệ nghiệp vụ để thuận tiện tra cứu và chỉnh sửa:

| STT | Tên file báo cáo | Phân hệ nghiệp vụ | Phạm vi Use Case |
| :---: | :--- | :--- | :--- |
| **00** | [**`00_INDEX_Tong_Quan.md`**](file:///c:/bmwms/docs/srs_audit/00_INDEX_Tong_Quan.md) | **Tổng quan đối soát toàn bộ hệ thống** | Thống kê 91 UC, 3 nhóm lỗi lớn |
| **01** | [**`01_Phan_He_Xac_Thuc_Va_Nguoi_Dung.md`**](file:///c:/bmwms/docs/srs_audit/01_Phan_He_Xac_Thuc_Va_Nguoi_Dung.md) | **Xác thực, Hồ sơ & Quản trị Người dùng** | UC-001 -> UC-013 (Module 1 & 2) |
| **02** | [**`02_Phan_He_Product_Va_Master_Data.md`**](file:///c:/bmwms/docs/srs_audit/02_Phan_He_Product_Va_Master_Data.md) | **Dữ liệu nền, Sản phẩm, Nhóm hàng & EAV** | UC-014 -> UC-022 (Module 3) |
| **03** | [**`03_Phan_He_Kho_Va_Vi_Tri_Storage.md`**](file:///c:/bmwms/docs/srs_audit/03_Phan_He_Kho_Va_Vi_Tri_Storage.md) | **Kho hàng, Sơ đồ vị trí & Nhiệm vụ kho** | UC-023 -> UC-026 (Module 4) |
| **04** | [**`04_Phan_He_Mua_Hang_Va_Nhap_Kho.md`**](file:///c:/bmwms/docs/srs_audit/04_Phan_He_Mua_Hang_Va_Nhap_Kho.md) | **Đơn mua hàng, Nhập kho & Khách trả hàng** | UC-027 -> UC-043 (Module 5 & 6) |
| **05** | [**`05_Phan_He_Ban_Hang_Va_Xuat_Kho.md`**](file:///c:/bmwms/docs/srs_audit/05_Phan_He_Ban_Hang_Va_Xuat_Kho.md) | **Đơn bán hàng, Xuất kho & Nhặt hàng (Picking)** | UC-044 -> UC-056 (Module 7 & 8) |
| **06** | [**`06_Phan_He_Dieu_Chuyen_Kho_Transfer.md`**](file:///c:/bmwms/docs/srs_audit/06_Phan_He_Dieu_Chuyen_Kho_Transfer.md) | **Điều chuyển kho nội bộ (Direct Execution)** | UC-057 -> UC-062 (Module 9) |
| **07** | [**`07_Phan_He_Kiem_Kho_Stocktake.md`**](file:///c:/bmwms/docs/srs_audit/07_Phan_He_Kiem_Kho_Stocktake.md) | **Kiểm kê kho (Available Snapshot & Khóa vị trí)** | UC-068 -> UC-077 (Module 11) |
| **08** | [**`08_Phan_He_Ton_Kho_Va_Bao_Cao_Reports.md`**](file:///c:/bmwms/docs/srs_audit/08_Phan_He_Ton_Kho_Va_Bao_Cao_Reports.md) | **Tra cứu tồn kho theo vị trí & Báo cáo** | UC-063 -> UC-067, UC-078 -> UC-091 |
| **09** | [**`09_Huong_Dan_Chinh_Sua_Tai_Lieu_Word.md`**](file:///c:/bmwms/docs/srs_audit/09_Huong_Dan_Chinh_Sua_Tai_Lieu_Word.md) | **Kế hoạch & Hướng dẫn sửa file .docx** | Chi tiết vị trí trang và nội dung thay thế |

---

## 2. THỐNG KÊ TỔNG HỢP 91 USE CASES

Qua quét toàn bộ source code của 4 project (`BMWMS.API`, `BMWMS.Business`, `BMWMS.Repository`, `BMWMS.Web`), đối chiếu với từng mục đặc tả trong file DOCX:

* **Số Use Case khớp tốt:** **38 / 91** (~42%)  
  * Hoạt động chuẩn xác giữa đặc tả và mã nguồn. Chủ yếu ở phân hệ Auth, PO, SO, Outbound cơ bản.
* **Số Use Case sai Source Wireframe / Bảng trường (Field Description) do Copy-Paste:** **36 / 91** (~40%)  
  * Cực kỳ nghiêm trọng ở Master Data (gán nhầm sang `Products/Index.cshtml`), Inventory (gán nhầm sang `Inventory/Index.cshtml` - trang redirect rỗng) và Dashboard & Reports (tất cả 14 báo cáo bị gán vào `Admin/Dashboard.cshtml`).
* **Số Use Case lệch pha do Code đã Tinh gọn / Refactor gần đây:** **10 / 91** (~11%)  
  * Phân hệ Transfer Order (loại bỏ bước Approve giữ tồn, chuyển sang thực hiện trực tiếp) và Stocktake (loại bỏ hàng phát sinh, chuyển số lượng đối soát sang số khả dụng Available).
* **Số Use Case trong SRS nhưng KHÔNG TỒN TẠI trong Code:** **7 / 91** (~7%)  
  * UC-060 (Approve transfer - đã xóa), UC-074 (Unexpected items - đã xóa), UC-084 (Product stats), UC-086 (Stocktake stats), UC-087 (Warehouse KPI), UC-090 (Overdue alerts), UC-072 (Assign stocktake - thừa dạng độc lập).

---

## 3. BA NHÓM LỖI HỆ THỐNG CỐT LÕI

### Nhóm lỗi 1: Lỗi sao chép giao diện và mô tả trường (Copy-Paste Artifacts)
* **Master Data (UC-014, 017, 018, 019, 021, 022):** Tất cả các trang này đều được gán `Source: BMWMS.Web/Pages/Products/Index.cshtml` và mang bảng trường lọc: *Từ khóa, Trạng thái, Nhóm sản phẩm, Phương pháp xuất kho, Số dòng/trang*. Trong khi thực tế hệ thống đã có các trang riêng biệt chuẩn chỉnh: `Categories/Index.cshtml`, `Admin/ProductAttributes/Index.cshtml`, `Admin/UnitOfMeasures/Index.cshtml`, `Admin/Suppliers/Index.cshtml`...
* **Inventory (UC-063 đến UC-067):** Toàn bộ gán về `BMWMS.Web/Pages/Inventory/Index.cshtml`. Trong code, trang này chỉ có 25 dòng làm nhiệm vụ redirect. Màn hình thực tế là `BMWMS.Web/Pages/Inventory/Inventory.cshtml` với 4 thẻ KPI và bộ lọc Zone/Rack/Bin.
* **Dashboard & Reports (UC-078 đến UC-091):** Toàn bộ 14 use case gán về `Admin/Dashboard.cshtml` và chép bảng lọc: *Từ ngày, Đến ngày, Từ khóa/Vật tư, Trạng thái/Loại chứng từ, Xuất Excel*. Thực tế `Dashboard.cshtml` là màn hình chỉ số tức thời không có lọc ngày tháng hay xuất Excel; còn các báo cáo nằm ở các trang chuyên biệt trong `BMWMS.Web/Pages/Admin/Reports/`.

### Nhóm lỗi 2: Lỗi lệch pha giữa Code mới và Tài liệu
* **Transfer Orders:** Commit `22ef71e` và script SQL `20260919_transfer_direct_execution.sql` đã xóa bỏ hoàn toàn `TransferApprovalService.cs` và quy trình duyệt giữ chỗ. Hiện tại là luồng thực hiện trực tiếp (Direct Execution): Staff tạo phiếu -> Staff bấm thực hiện -> Chuyển tồn kho và hoàn tất tức thì. Tài liệu SRS vẫn giữ quy trình cũ (Duyệt -> Xác nhận -> Hoàn thành).
* **Stocktake (Kiểm kho):** Commit `22ef71e` đã xóa bỏ hoàn toàn `UnexpectedStocktakeItemDto` và API thêm hàng phát sinh ngoài danh mục kiểm. Đồng thời script `20260919_stocktake_available_snapshot.sql` chuyển số lượng sổ sách (`BookQuantity`) sang số lượng **Khả dụng** (Available = OnHand - Reserved), không phải On-hand thô. SRS vẫn giữ use case hàng phát sinh và mô tả On-hand.
* **Báo cáo tồn kho (Inventory Report - UC-080):** Trong code là báo cáo Snapshot thời điểm hiện tại ("Dữ liệu tính đến: CutOffTime"), không có lọc Từ ngày / Đến ngày. SRS lại mô tả có Từ ngày / Đến ngày.

### Nhóm lỗi 3: Lỗi Ma trận phân quyền (Permission Matrix) so với Controller
* **Bắt đầu kiểm kho (UC-071):** SRS đánh dấu Quản lý kho (WM: ✓). Thực tế code gắn `[Authorize(Roles = "WAREHOUSE_STAFF")]` và ràng buộc chỉ nhân viên được gán phụ trách phiếu mới được bấm Bắt đầu.
* **Tạo phiếu chuyển kho (UC-057):** SRS đánh dấu Quản lý kho (WM: ✓). Thực tế code gắn `[Authorize(Roles = "WAREHOUSE_STAFF")]`, giao diện chỉ hiển thị nút tạo cho Staff.
