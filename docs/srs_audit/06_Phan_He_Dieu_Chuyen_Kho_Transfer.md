# BÁO CÁO ĐỐI SOÁT PHÂN HỆ 9: ĐIỀU CHUYỂN KHO (TRANSFER ORDER)
**Phạm vi Use Case:** UC-057 đến UC-062  
**Vị trí trong tài liệu SRS:** Mục 2.9 (Transfer Order)  
**Trạng thái đối soát:** **Lệch pha nghiêm trọng 80% do Code đã chuyển sang cơ chế Thực hiện trực tiếp (Direct Execution)**

---

## 1. BẢNG TỔNG HỢP TRẠNG THÁI TỪNG USE CASE

| Mã UC | Tên Use Case | Trạng thái trong SRS | Thực tế trong Code hiện tại | Đánh giá & Hướng xử lý |
| :---: | :--- | :--- | :--- | :--- |
| **UC-057** | Create transfer order | WM & WS tạo phiếu; vị trí chung chung | Chỉ `WAREHOUSE_STAFF` tạo; chọn phân cấp Zone->Rack->Bin và Lot khả dụng | ⚠️ Sửa Actor & Form chọn Bin |
| **UC-058** | View transfer order list | Dropdown có: Draft, Approved, Assigned, Completed, Cancelled | Chỉ có: DRAFT, ASSIGNED (chỉ xem), COMPLETED, CANCELLED | ⚠️ Bỏ trạng thái `Approved` |
| **UC-059** | View transfer order details | Hiển thị nút Phê duyệt / Xác nhận / Hoàn thành | Nút Sửa (`DRAFT`), Hủy (`DRAFT`), Thực hiện (`DRAFT`) | ⚠️ Sửa các nút hành động |
| **UC-060** | Approve transfer order | Quản lý kho duyệt phiếu chuyển | **ĐÃ XÓA BỎ HOÀN TOÀN TRONG CODE** | ❌ **Xóa bỏ UC-060** khỏi SRS |
| **UC-061** | Confirm transfer execution | Nhân viên xác nhận thực hiện | Nhân viên bấm "Thực Hiện" -> Di chuyển hàng và Complete luôn | ⚠️ Gộp với UC-062 |
| **UC-062** | Complete transfer order | Use case riêng để hoàn thành | Không có bước riêng, tự động chuyển sang COMPLETED tại UC-061 | ⚠️ Gộp vào UC-061 |
| *(Mới)* | Cancel transfer order | Chưa có trong tài liệu SRS | API `POST api/transfers/{id}/cancel` + Modal Hủy trên giao diện | ❌ **Bổ sung Use Case mới** |

---

## 2. NGUYÊN NHÂN LỆCH PHA CỐT LÕI VỀ KIẾN TRÚC

Trong commit `22ef71e` và script SQL [`docs/migrations/20260919_transfer_direct_execution.sql`](file:///c:/bmwms/docs/migrations/20260919_transfer_direct_execution.sql):
* **Hệ thống cũ:** Lập phiếu (`DRAFT`) -> Quản lý duyệt (`APPROVED`) -> Hệ thống giữ tồn (`RESERVE`) tại vị trí nguồn -> Nhân viên thực hiện -> Chuyển sang `COMPLETED`.
* **Hệ thống mới (Direct Execution):**
  > *"Chuyển đổi Transfer sang luồng thực hiện trực tiếp, không giữ tồn. Bỏ bước duyệt; đã giải phóng hold cũ và đưa phiếu về chờ thực hiện."*
* File `TransferApprovalService.cs` đã bị xóa hoàn toàn khỏi backend. Toàn bộ các phiếu `APPROVED` cũ đã được cập nhật về `DRAFT`.
* Do đó, tài liệu SRS hiện tại đang đặc tả theo quy trình 3 bước cũ đã lỗi thời.

---

## 3. CHI TIẾT TỪNG USE CASE VÀ HƯỚNG DẪN ĐIỀU CHỈNH

### UC-057: Create transfer order (Tạo phiếu chuyển kho)
* **Lỗi phân quyền trong SRS:**
  * SRS Permission Matrix ghi cả Quản lý kho (*WM*) và Nhân viên kho (*WS*).
  * Thực tế API [`TransferOrdersController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/StockOperations/TransferOrdersController.cs) dòng 45:
    ```csharp
    [HttpPost("create")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateTransferOrderDto dto)
    ```
  * Trên giao diện [`BMWMS.Web/Pages/Transfer/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Transfer/Index.cshtml):
    ```html
    @if (Model.IsStaff) {
        <a href="/Transfer/Create" class="btn btn-primary">Tạo phiếu mới</a>
    }
    ```
    Quản lý kho không tạo phiếu trực tiếp.
* **Cấu trúc form tạo phiếu thực tế ([`Transfer/Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Transfer/Create.cshtml)):**
  * *Thông tin chung:* Kho hàng (mặc định cùng một kho), Ngày dự kiến hoàn thành (`dueDate`), Ghi chú chung (`notes`).
  * *Bảng chi tiết hàng hóa (chọn phân cấp):*
    * **Vị trí nguồn:** Chọn Zone nguồn -> Rack nguồn -> Bin nguồn.
    * **Hàng hóa:** Sau khi chọn Bin nguồn, hệ thống tự động tải danh sách các sản phẩm và số lô đang có tồn khả dụng tại Bin đó.
    * **Tồn khả dụng:** Hệ thống tự động hiển thị số lượng khả dụng tại Bin nguồn để người dùng không nhập vượt quá.
    * **Vị trí đích:** Chọn Zone đích -> Rack đích -> Bin đích (Bin đích bắt buộc phải khác Bin nguồn).
    * **Số lượng chuyển:** Nhập số lượng cần chuyển (bắt buộc $\le$ tồn khả dụng).
  * *Trạng thái sau khi tạo:* `DRAFT` (Chờ thực hiện).
* **Cần sửa trong SRS:** Đổi Actor thành `Warehouse Staff`. Bổ sung luồng chọn phân cấp Zone/Rack/Bin và chọn Lot khả dụng.

---

### UC-060: Approve transfer order (Phê duyệt chuyển kho)
* **Lỗi trong SRS:** UC-060 yêu cầu Quản lý kho duyệt phiếu chuyển để chuyển trạng thái sang `APPROVED`.
* **Code thực tế:**
  * **HOÀN TOÀN KHÔNG CÒN TRONG CODE.**
  * Không có endpoint `POST api/transfers/{id}/approve`.
  * Không có nút Duyệt trên màn hình [`Transfer/Details.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Transfer/Details.cshtml).
* **Cần sửa trong SRS:** **XÓA BỎ UC-060 KHỎI SRS** để bảo đảm tài liệu khớp với mã nguồn thực tế.

---

### UC-061 & UC-062: Confirm transfer execution & Complete transfer order
* **Lỗi trong SRS:** Tách làm 2 use case trung gian (Xác nhận thực hiện -> Hoàn thành).
* **Code thực tế:**
  * Khi nhân viên kho bấm nút "Thực hiện" trên [`Transfer/Details.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Transfer/Details.cshtml), hệ thống gọi `POST api/transfers/{id}/confirm` (gắn `[Authorize(Roles = "WAREHOUSE_STAFF")]`):
    1. Kiểm tra sức chứa tại các Bin đích (`EnsureTransferCapacityDecision`).
    2. Trừ tồn kho tại Bin nguồn (`TRANSFER_OUT`) và cộng tồn kho tại Bin đích (`TRANSFER_IN`).
    3. Cập nhật trạng thái phiếu thẳng sang **`COMPLETED`**.
  * Toàn bộ diễn ra trong **1 Transaction duy nhất (Atomic Operation)**.
* **Cần sửa trong SRS:**
  * Gộp UC-061 và UC-062 thành 1 use case duy nhất: *"Execute and complete transfer order"* (Thực hiện và hoàn tất chuyển kho).
  * Phân quyền thực hiện: Chỉ `Warehouse Staff`.

---

### Use Case BỊ THIẾU: Cancel transfer order (Hủy phiếu chuyển kho)
* **Code thực tế:**
  * Endpoint: `POST api/transfers/{id}/cancel` trong [`TransferActionsController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/StockOperations/TransferActionsController.cs).
  * Giao diện: Nút "Hủy" trên [`Transfer/Details.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Transfer/Details.cshtml) mở Modal yêu cầu nhập Lý do hủy (`notes`).
  * Quyền thực hiện: Cả `WAREHOUSE_STAFF` và `WAREHOUSE_MANAGER` đều có thể hủy phiếu khi đang ở trạng thái `DRAFT`. Phiếu chuyển sang `CANCELLED`.
* **Cần sửa trong SRS:** Bổ sung Use Case *"Cancel transfer order"*.
