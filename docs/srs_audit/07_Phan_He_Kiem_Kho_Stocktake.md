# BÁO CÁO ĐỐI SOÁT PHÂN HỆ 11: KIỂM KHO (STOCKTAKE)
**Phạm vi Use Case:** UC-068 đến UC-077  
**Vị trí trong tài liệu SRS:** Mục 2.11 (Stocktake)  
**Trạng thái đối soát:** **Lệch pha 60% do Code đã bỏ chức năng Hàng phát sinh, chuyển sang Snapshot Khả dụng và cơ chế Reject mở khóa đóng phiên.**

---

## 1. BẢNG TỔNG HỢP TRẠNG THÁI TỪNG USE CASE

| Mã UC | Tên Use Case | Trạng thái trong SRS | Thực tế trong Code hiện tại | Đánh giá & Hướng xử lý |
| :---: | :--- | :--- | :--- | :--- |
| **UC-068** | Create stocktake session | Quản lý kho tạo phiếu | Tạo phiếu kèm chọn phạm vi (Toàn kho/Khu vực) và phân công Staff | ✅ Khớp 95% |
| **UC-069** | View stocktake list | Danh sách phiên kiểm | Danh sách + bộ lọc Từ khóa, Kho, Trạng thái | ✅ Khớp 100% |
| **UC-070** | View stocktake details | Chi tiết phiên kiểm | Hiển thị 5 Tab: Tổng quan, Vị trí, Bảng đếm, Chênh lệch, Thao tác | ⚠️ Bổ sung các Tab |
| **UC-071** | Start stocktake | Quản lý kho bắt đầu kiểm | **CHỈ Nhân viên kho được gán mới có quyền bắt đầu** | ❌ **Sai Actor phân quyền** |
| **UC-072** | Assign stocktake | Use case độc lập để gán Staff | Phân công nhân viên là trường bắt buộc ngay tại form tạo phiếu UC-068 | ⚠️ Gộp vào UC-068 |
| **UC-073** | Enter count results | Ghi nhầm Source `Create.cshtml` | Màn hình `Stocktake/Details.cshtml` (`_DetailsCountSheet.cshtml`) | ❌ Sai Source & Bảng trường |
| **UC-074** | Record unexpected items | Ghi nhận hàng phát sinh | **ĐÃ BỊ XÓA BỎ HOÀN TOÀN TRONG CODE** | ❌ **Xóa bỏ UC-074** khỏi SRS |
| **UC-075** | Submit stocktake result | Gửi kết quả kiểm đếm | Nhân viên bấm "Hoàn tất và gửi" -> Chuyển sang `PENDING_APPROVAL` | ✅ Khớp 100% |
| **UC-076** | Approve stocktake result | Quản lý duyệt kết quả | Quản lý duyệt -> Cân chỉnh chênh lệch tồn kho -> Mở khóa vị trí | ✅ Khớp 100% |
| **UC-077** | Reject stocktake result | Từ chối trả về cho đếm lại | **Từ chối chuyển sang `REJECTED`, mở khóa vị trí và ĐÓNG LUÔN PHIÊN** | ❌ **Sai Business Rule** |
| *(Mới)* | Cancel stocktake session | Chưa có trong tài liệu SRS | API `POST api/stocktakes/{id}/cancel` dành cho Quản lý kho | ❌ **Bổ sung Use Case mới** |

---

## 2. NGUYÊN NHÂN LỆCH PHA CỐT LÕI VỀ KIẾN TRÚC

Trong commit `22ef71e` và script SQL [`docs/migrations/20260919_stocktake_available_snapshot.sql`](file:///c:/bmwms/docs/migrations/20260919_stocktake_available_snapshot.sql):
1. **Bỏ chức năng hàng phát sinh (Unexpected Items):**
   * Trong thực tế vận hành VLXD, việc cho phép nhân viên tự thêm sản phẩm/lô mới bất kỳ vào ô kiểm đếm gây rủi ro sai lệch dữ liệu vị trí. Do đó commit `22ef71e` đã xóa toàn bộ `UnexpectedStocktakeItemDto`, `IsUnexpected` và endpoint `POST api/stocktakes/{id}/unexpected-items`.
   * Phiên kiểm chỉ kiểm đếm đúng các sản phẩm/lô thuộc phạm vi vị trí đã đóng băng. Nếu một vị trí không có hàng, nhân viên tích chọn **"Xác nhận vị trí trống"** (`ConfirmedEmptyLocationIds`).
2. **Chuyển số lượng đối soát sang Số lượng Khả dụng (Available Snapshot):**
   * Số lượng sổ sách (`BookQuantity`) tại thời điểm bấm bắt đầu được tính bằng **Tồn khả dụng sẵn sàng xuất bán (`Available = OnHand - Reserved`)**, không dùng On-hand thô (để không bị lệch với các đơn hàng đã được giữ tồn cho khách).
3. **Cơ chế Khóa & Mở khóa vị trí (Location Locks):**
   * Khi phiên bắt đầu (`IN_PROGRESS`), toàn bộ các Bin trong phạm vi kiểm bị khóa (`LOCKED`), chặn mọi thao tác nhập/xuất/chuyển kho tại các Bin này.
   * Vị trí chỉ được giải phóng (`ReleaseLocationLocksAsync`) khi:
     - Quản lý **Duyệt (`APPROVE`)** -> Cân chỉnh tồn và mở khóa.
     - Quản lý **Từ chối (`REJECT`)** -> Đóng phiên ở trạng thái `REJECTED` và mở khóa toàn bộ (không cho đếm lại trên cùng phiên này).
     - Quản lý **Hủy (`CANCEL`)** -> Đóng phiên ở trạng thái `CANCELLED` và mở khóa.

---

## 3. CHI TIẾT TỪNG USE CASE VÀ HƯỚNG DẪN ĐIỀU CHỈNH

### UC-071: Start stocktake (Bắt đầu kiểm kho)
* **Lỗi phân quyền trong SRS:**
  * Bảng Permission Matrix trong SRS đánh dấu Quản lý kho (*WM: ✓, WS: —*).
* **Code thực tế:**
  * Controller [`StocktakesController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/StocktakesController.cs) dòng 78:
    ```csharp
    [HttpPost("{id:long}/start")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> StartStocktake(long id)
    ```
  * Service [`StocktakeService.cs`](file:///c:/bmwms/BMWMS.Business/Services/Stocktake/StocktakeService.cs):
    ```csharp
    if (session.AssignedToUserId != startedByUserId)
        throw new UnauthorizedAccessException("Chỉ nhân viên được giao mới có thể bắt đầu phiếu kiểm kho.");
    ```
  * **Chỉ nhân viên kho được phân công phụ trách phiếu mới có quyền bấm Bắt đầu kiểm**.
* **Cần sửa trong SRS:** Đổi Actor sang `Warehouse Staff` (được phân công).

---

### UC-073: Enter count results (Nhập số lượng kiểm đếm)
* **Lỗi trong SRS:**
  * Source ghi nhầm là: `BMWMS.Web/Pages/Stocktake/Create.cshtml`.
  * Field Description liệt kê các trường tạo phiếu (*Mã phiên, Kho, Ngày, Nhân viên, Ghi chú, Phạm vi...*).
* **Code thực tế:**
  * Màn hình thực tế là: [`BMWMS.Web/Pages/Stocktake/Details.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Stocktake/Details.cshtml) kết hợp partial [`_DetailsCountSheet.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Stocktake/_DetailsCountSheet.cshtml).
  * Cấu trúc bảng đếm thực tế:
    * *Vị trí:* Mã vị trí (`LocationCode`) kèm đường dẫn Zone/Rack.
    * *Sản phẩm:* Mã sản phẩm (`ProductCode`), Tên sản phẩm, Đơn vị tính.
    * *Lô:* Số lô (`LotNumber`).
    * *Số lượng khả dụng theo sổ (`BookQuantity`):* Số lượng snapshot khả dụng tại thời điểm bắt đầu (Read-only).
    * *Số lượng khả dụng thực đếm (`CountedQuantity`):* Ô input nhập số thực đếm (hỗ trợ số thập phân theo `QuantityScale` của ĐVT).
    * *Ghi chú (`Notes`):* Nhập giải trình/tình trạng hàng.
    * *Bảng xác nhận vị trí trống (`ConfirmedEmptyLocationIds`):* Checkbox xác nhận các Bin trong phạm vi không có hàng.
    * *Nút hành động:* "Lưu nháp" (`SaveCounts`) và "Hoàn tất và gửi" (`Submit`).
* **Cần sửa trong SRS:**
  * Đổi Source sang `Stocktake/Details.cshtml`.
  * Thay toàn bộ Field Description sang bảng nhập số đếm chi tiết như trên.

---

### UC-074: Record unexpected items (Ghi nhận vật tư phát sinh)
* **Code thực tế:** **ĐÃ BỊ XÓA BỎ HOÀN TOÀN KHỎI HỆ THỐNG.**
* **Cần sửa trong SRS:**
  * **XÓA BỎ UC-074 KHỎI SRS**.
  * Xóa bỏ cột/trường "Vật tư phát sinh" trong các đặc tả UC-070, UC-071, UC-075, UC-076, UC-077.

---

### UC-077: Reject stocktake result (Từ chối kết quả kiểm kho)
* **Lỗi trong SRS:** Business Rule ghi: *"rejection returns the session for correction"* (từ chối trả phiên về cho nhân viên đếm lại).
* **Code thực tế:**
  * Tại [`StocktakeRepository.cs`](file:///c:/bmwms/BMWMS.Repository/Repositories/Stocktake/StocktakeRepository.cs) (`RejectSessionAsync`):
    * Khi Quản lý từ chối kết quả kiểm, trạng thái session chuyển vĩnh viễn sang **`REJECTED`**.
    * Hệ thống gọi `ReleaseLocationLocksAsync` để **mở khóa ngay lập tức toàn bộ các vị trí kho**.
    * **Phiên kiểm kết thúc tại đây, không cho đếm lại**. Nếu muốn kiểm lại cần tạo phiên kiểm mới.
* **Cần sửa trong SRS:** Sửa Postconditions và Business Rule của UC-077: Từ chối sẽ chuyển trạng thái sang `REJECTED`, giải phóng toàn bộ khóa vị trí và đóng phiên kiểm.
