# KẾ HOẠCH VÀ HƯỚNG DẪN CHỈNH SỬA TÀI LIỆU WORD (SRS)
**Tệp tài liệu cần sửa:** `BMWMS_Report3_SRS_Code_Based_20260920.docx`  
**Mục tiêu:** Cập nhật lại toàn bộ nội dung sai lệch, đảm bảo tài liệu SRS phản ánh chính xác 100% mã nguồn thực tế của hệ thống BMWMS.

---

## 1. DANH SÁCH CÁC MỤC CHÍNH CẦN SỬA ĐỔI TRONG FILE DOCX

### Bước 1: Cập nhật Mục 1.3 - Conceptual Data Model & Business Rules
1. **Bảng `TransferOrders`:**
   * Sửa cột `Status`: Xóa bỏ giá trị `APPROVED`. Chỉ còn các giá trị hợp lệ: `DRAFT` (Chờ thực hiện), `COMPLETED` (Hoàn thành), `CANCELLED` (Đã hủy), và `ASSIGNED` (Phiếu cũ chỉ xem).
2. **Bảng `StocktakeSessions` & `StocktakeItems`:**
   * Cập nhật quy tắc `BookQuantity`: Là số lượng **Khả dụng** tại thời điểm bắt đầu kiểm (`Available = OnHand - Reserved`), không dùng OnHand đơn thuần.
   * Xóa bỏ trường hoặc cờ `IsUnexpected` (Hàng phát sinh) trong các bảng liên quan đến kiểm kho.

---

### Bước 2: Cập nhật Mục 4.4 - Permission Matrix (Ma trận phân quyền)
Tìm bảng ma trận phân quyền tại trang ~242 và cập nhật các dòng sau:

| Mã UC | Tên Use Case | Sửa đổi phân quyền | Lý do kỹ thuật trong Code |
| :---: | :--- | :--- | :--- |
| **UC-057** | Create transfer order | Bỏ tick **WM**, chỉ giữ tick **WS** | API `POST api/transfers/create` gắn `[Authorize(Roles = "WAREHOUSE_STAFF")]` |
| **UC-060** | Approve transfer order | **XÓA NGUYÊN DÒNG NÀY** | Code đã bỏ bước duyệt, chuyển sang thực hiện trực tiếp |
| **UC-061 & 062** | Confirm & Complete transfer | Gộp thành 1 dòng: *Execute transfer* (Chỉ tick **WS**) | Nhân viên bấm Thực hiện là chuyển tồn và hoàn tất luôn |
| *(Mới)* | Cancel transfer order | **BỔ SUNG DÒNG NÀY**: Tick cả **WM** và **WS** | Cả Quản lý và Nhân viên đều có thể hủy phiếu Draft |
| **UC-071** | Start stocktake | Đổi tick từ **WM** sang **WS** | API `POST api/stocktakes/{id}/start` gắn `[Authorize(Roles = "WAREHOUSE_STAFF")]` |
| **UC-072** | Assign stocktake | Gộp vào dòng UC-068 (Tạo phiếu) | Phân công nhân viên là trường bắt buộc khi tạo |
| **UC-074** | Record unexpected items | **XÓA NGUYÊN DÒNG NÀY** | Code đã bỏ chức năng hàng phát sinh |
| *(Mới)* | Cancel stocktake session | **BỔ SUNG DÒNG NÀY**: Tick **WM** | Quản lý có API hủy phiên kiểm khi đang tạo hoặc đang kiểm |
| **UC-084, 086, 087, 090** | Báo cáo stats / KPI / Overdue | Đổi thành gạch ngang `—` hoặc xóa dòng | Không có Controller và View trong code |

---

### Bước 3: Cập nhật Mục 5.1 - Screen Inventory & 5.4 Traceability
1. **Số lượng màn hình Web:**
   * Sửa câu: *"The BMWMS Web project contains 87 Razor Page files"* thành:  
     `"The BMWMS Web project contains 75 Razor Page files under BMWMS.Web/Pages."`
2. **Cập nhật danh mục màn hình chính:**
   * Bổ sung trang `BMWMS.Web/Pages/Inventory/Inventory.cshtml` (Tồn Kho Theo Kho & Vị Trí).
   * Bổ sung đầy đủ 7 trang Báo cáo chuyên biệt dưới `BMWMS.Web/Pages/Admin/Reports/`:
     - `Inventory.cshtml` (Báo cáo tồn kho tức thời)
     - `InOutStock.cshtml` (Báo cáo xuất nhập tồn có lọc khoảng ngày)
     - `Inbound.cshtml` (Báo cáo nhập kho)
     - `Outbound.cshtml` (Báo cáo xuất kho)
     - `LowStock.cshtml` (Cảnh báo tồn kho thấp)
     - `ExpiringLot.cshtml` (Cảnh báo lô sắp hết hạn)
     - `SupplierStatistics.cshtml` (Thống kê nhà cung cấp)
   * Bổ sung phân hệ Khách trả hàng: `CustomerReturns/Index.cshtml`, `Create.cshtml`, `Details.cshtml`.
   * Bổ sung trang nhặt hàng xuất kho: `OutboundOrders/Process.cshtml`.
   * Bổ sung trang cất hàng nhập kho: `Admin/Inbound/Putaway.cshtml`.

---

### Bước 4: Sửa chi tiết các bảng Use Case Specification trong Mục 2

#### 1. Nhóm Master Data (Mục 2.3):
* **UC-014:** Đổi Source sang `BMWMS.Web/Pages/Categories/Index.cshtml`. Thay bộ lọc bằng thông tin nhóm ngành hàng VLXD, cấu hình thuộc tính động EAV và ĐVT cơ sở.
* **UC-015:** Bổ sung tham chiếu `Products/Create.cshtml`, `Edit.cshtml`. Bổ sung các trường SKU, TrackExpiry, Barcode, Min/Max và quy tắc kế thừa ĐVT từ nhóm hàng.
* **UC-016:** Sửa Normal Flow (bỏ bước tìm kiếm lọc); bổ sung 4 metric cards và 3 tabs (Info, EAV, Location stock).
* **UC-017:** Đổi Source sang `Admin/ProductAttributes/Index.cshtml`. Thay bảng trường sang kiểu dữ liệu EAV (TEXT, NUMBER, SELECT, DATE, Options).
* **UC-018:** Đổi Source sang `Admin/UnitOfMeasures/Index.cshtml`, bổ sung trường `QuantityScale` (0-4).
* **UC-019, 020, 021:** Đổi Source sang `Admin/Suppliers/Index.cshtml`, `Detail.cshtml`, `InboundHistory.cshtml`.

#### 2. Nhóm Warehouse (Mục 2.4):
* **UC-023:** Sửa bảng trường thông tin kho: Mã kho, Tên kho, Địa chỉ, Loại kho, Trạng thái (bỏ Zone/Rack/Bin).
* **UC-025:** Đổi Source sang `StorageLocations/Index.cshtml` (Sơ đồ phân cấp Zone -> Rack -> Bin, MaxWeight, MaxVolume, trạng thái Available/Occupied/Locked).
* **UC-026:** Đổi Source sang `Warehouse/MyTasks.cshtml` (Nhiệm vụ kho được phân công cho nhân viên).

#### 3. Nhóm Inbound & Outbound (Mục 2.6 & 2.8):
* **UC-040 & 041:** Đổi Source sang `Admin/Inbound/Details.cshtml` (nhận hàng và ghi nhận số lô/HSD).
* **UC-042:** Đổi Source sang `Admin/Inbound/Putaway.cshtml` (cất hàng vào vị trí bin).
* **UC-043:** Sửa lại toàn bộ phân hệ Customer Returns (`CustomerReturns/Index.cshtml`, `Create.cshtml`, `Details.cshtml`).
* **UC-050:** Đổi Source sang `OutboundOrders/Edit.cshtml`.
* **UC-052 & 053:** Đổi Source sang `OutboundOrders/Process.cshtml` (màn hình nhặt hàng Picking theo Bin).

#### 4. Nhóm Transfer Order (Mục 2.9):
* **UC-057:** Sửa Actor thành `Warehouse Staff`. Bổ sung form chọn phân cấp Zone->Rack->Bin nguồn/đích và chọn Lot khả dụng.
* **UC-058 & 059:** Bỏ trạng thái `Approved` trong bộ lọc và chi tiết.
* **UC-060:** Xóa bỏ hoàn toàn khỏi tài liệu.
* **UC-061 & 062:** Gộp làm 1 use case *"Execute and complete transfer order"* (Staff bấm thực hiện là chuyển tồn và hoàn tất ngay).
* **Bổ sung:** Use case *"Cancel transfer order"*.

#### 5. Nhóm Stocktake (Mục 2.11):
* **UC-071:** Sửa Actor: Chỉ `Warehouse Staff` được phân công mới có quyền bấm Bắt đầu.
* **UC-072:** Gộp vào UC-068.
* **UC-073:** Đổi Source sang `Stocktake/Details.cshtml` (`_DetailsCountSheet.cshtml`). Số lượng sổ sách là Khả dụng (Available snapshot). Thêm bảng checkbox xác nhận vị trí trống.
* **UC-074:** Xóa bỏ hoàn toàn khỏi tài liệu; xóa trường "Vật tư phát sinh" ở các use case kiểm kho khác.
* **UC-077:** Sửa Business Rule: Từ chối sẽ chuyển trạng thái sang `REJECTED`, mở khóa toàn bộ vị trí và đóng phiên kiểm (không cho đếm lại trên cùng phiên).
* **Bổ sung:** Use case *"Cancel stocktake session"*.

#### 6. Nhóm Dashboard & Reports (Mục 2.10 & 2.12):
* **UC-063 đến 067:** Đổi Source sang `BMWMS.Web/Pages/Inventory/Inventory.cshtml`. Bổ sung 4 thẻ On Hand/Available/Reserved/In Transit, bộ lọc Zone/Rack/Bin và nút Xuất Excel.
* **UC-078 & 079:** Bỏ bộ lọc Từ ngày/Đến ngày/Xuất Excel; mô tả đúng các khối KPI và cảnh báo thời gian thực.
* **UC-080:** Đổi Source sang `Admin/Reports/Inventory.cshtml`. Bỏ bộ lọc Từ ngày/Đến ngày (đây là báo cáo Snapshot thời điểm hiện tại: "Dữ liệu tính đến: CutOffTime"); cập nhật các trường lọc: Sản phẩm, Vị trí kho, Số lô, Chỉ tồn dương, Số dòng/trang.
* **UC-081 & 082:** Đổi Source sang `Admin/Reports/Inbound.cshtml` và `Outbound.cshtml`.
* **UC-083:** Đổi Source sang `Admin/Reports/InOutStock.cshtml` (Báo cáo xuất nhập tồn có lọc khoảng ngày).
* **UC-085:** Đổi Source sang `Admin/Reports/SupplierStatistics.cshtml`.
* **UC-088 & 089:** Đổi Source sang `Admin/Reports/LowStock.cshtml` và `ExpiringLot.cshtml`.
* **UC-084, 086, 087, 090:** Đánh dấu là tính năng tương lai (Out of scope) hoặc lược bỏ.
