# BÁO CÁO ĐỐI SOÁT PHÂN HỆ 4: KHO HÀNG, VỊ TRÍ & NHIỆM VỤ KHO
**Phạm vi Use Case:** UC-023 đến UC-026  
**Vị trí trong tài liệu SRS:** Mục 2.4 (Warehouse and Storage)  
**Trạng thái đối soát:** Lệch 60% do trỏ nhầm tất cả về `Warehouse/Index.cshtml` và trộn lẫn giữa quản lý thông tin kho với sơ đồ vị trí và nhiệm vụ kho.

---

## 1. BẢNG TỔNG HỢP TRẠNG THÁI TỪNG USE CASE

| Mã UC | Tên Use Case | Source trong SRS | Source thực tế trong Code | Đánh giá |
| :---: | :--- | :--- | :--- | :---: |
| **UC-023** | Manage warehouses | `BMWMS.Web/Pages/Warehouse/Index.cshtml` | `Warehouse/Index.cshtml`, `Create.cshtml`, `Edit.cshtml` | ⚠️ Lẫn vị trí lưu trữ |
| **UC-024** | View warehouse details | `BMWMS.Web/Pages/Warehouse/Details.cshtml` | `BMWMS.Web/Pages/Warehouse/Details.cshtml` | ✅ Khớp 90% |
| **UC-025** | Manage storage locations | `BMWMS.Web/Pages/Warehouse/Index.cshtml` | `BMWMS.Web/Pages/StorageLocations/Index.cshtml` | ❌ Sai Source Wireframe |
| **UC-026** | View warehouse tasks | `BMWMS.Web/Pages/Warehouse/Index.cshtml` | `BMWMS.Web/Pages/Warehouse/MyTasks.cshtml` | ❌ Sai Source Wireframe |

---

## 2. CHI TIẾT TỪNG USE CASE VÀ ĐIỂM CẦN CHỈNH SỬA

### UC-023: Manage warehouses (Quản lý kho hàng)
* **Lỗi trong SRS:**
  * Field Description ghi các trường: *Từ khóa, Zone, Rack, Trạng thái Bin, Chế độ xem*. (Đây là các trường của Vị trí lưu trữ, không phải của Kho).
* **Code thực tế:**
  * File danh sách: [`BMWMS.Web/Pages/Warehouse/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Warehouse/Index.cshtml).
  * Khai báo mới: [`BMWMS.Web/Pages/Warehouse/Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Warehouse/Create.cshtml).
  * Chỉnh sửa: [`BMWMS.Web/Pages/Warehouse/Edit.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Warehouse/Edit.cshtml).
  * API: [`WarehousesController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/WarehousesController.cs).
  * Các trường thực tế khi quản lý kho:
    * *Mã kho (`WarehouseCode`)*
    * *Tên kho (`WarehouseName`)*
    * *Địa chỉ (`Address`)*
    * *Loại kho (`WarehouseType`: Kho tổng, Kho nhánh, Kho phụ...)*
    * *Thủ kho / Người phụ trách*
    * *Trạng thái (`Status`: ACTIVE, INACTIVE)*
* **Cần chỉnh sửa trong SRS:** Sửa bảng Field Description: Bỏ Zone/Rack/Bin, thay bằng các trường thông tin kho hàng thực tế ở trên.

---

### UC-025: Manage storage locations (Quản lý vị trí lưu trữ trong kho)
* **Lỗi trong SRS:** Source ghi nhầm là `Warehouse/Index.cshtml`.
* **Code thực tế:**
  * File giao diện: [`BMWMS.Web/Pages/StorageLocations/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/StorageLocations/Index.cshtml) và [`Index.cshtml.cs`](file:///c:/bmwms/BMWMS.Web/Pages/StorageLocations/Index.cshtml.cs).
  * API: [`StorageLocationsController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/Inventory/StorageLocationsController.cs).
  * Trang này quản lý **Sơ đồ cấu trúc vị trí lưu trữ phân cấp (3 tầng)**:
    1. **Khu vực (Zone):** Mã khu vực, Tên khu vực, Phân loại tính chất (Khu khô ráo, Khu ngoài trời, Khu xi măng...).
    2. **Dãy/Kệ (Storage Rack):** Mã dãy kệ, Chiều dài/rộng/cao.
    3. **Ô vị trí (Bin / Storage Location):** Mã vị trí (LocationCode), Tải trọng tối đa (`MaxWeight`), Thể tích tối đa (`MaxVolume`), Trạng thái vị trí (`Status`: AVAILABLE, OCCUPIED, LOCKED).
    4. *Cơ chế khóa vị trí:* Tự động khóa khi có phiên kiểm kho (`Stocktake`) đang hoạt động để tránh xuất nhập sai lệch.
* **Cần chỉnh sửa trong SRS:**
  * Đổi Source Wireframe sang: `BMWMS.Web/Pages/StorageLocations/Index.cshtml`.
  * Cập nhật mô tả cấu trúc phân cấp Zone -> Rack -> Bin và giới hạn tải trọng MaxWeight / MaxVolume.

---

### UC-026: View warehouse tasks (Xem nhiệm vụ nhân viên kho)
* **Lỗi trong SRS:** Source ghi nhầm là `Warehouse/Index.cshtml` và chép bảng lọc vị trí.
* **Code thực tế:**
  * File giao diện: [`BMWMS.Web/Pages/Warehouse/MyTasks.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Warehouse/MyTasks.cshtml) và [`MyTasks.cshtml.cs`](file:///c:/bmwms/BMWMS.Web/Pages/Warehouse/MyTasks.cshtml.cs).
  * Đây là màn hình dành riêng cho **Nhân viên kho (Warehouse Staff)** xem toàn bộ công việc được phân công cho cá nhân mình:
    * *Nhiệm vụ Nhận hàng & Cất hàng Inbound* (Lệnh nhập kho được gán).
    * *Nhiệm vụ Nhặt hàng Outbound Picking* (Lệnh xuất kho được gán).
    * *Nhiệm vụ Điều chuyển nội bộ Transfer* (Lệnh chuyển kho).
    * *Nhiệm vụ Kiểm đếm Stocktake* (Phiên kiểm kho được gán phụ trách đếm).
* **Cần chỉnh sửa trong SRS:**
  * Đổi Source Wireframe sang: `BMWMS.Web/Pages/Warehouse/MyTasks.cshtml`.
  * Sửa lại Field Description và luồng thực hiện phản ánh đúng danh sách nhiệm vụ được gán cho nhân viên kho.
