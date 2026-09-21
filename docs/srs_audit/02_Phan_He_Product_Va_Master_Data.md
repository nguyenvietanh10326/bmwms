# BÁO CÁO ĐỐI SOÁT PHÂN HỆ 3: DỮ LIỆU NỀN, SẢN PHẨM & THUỘC TÍNH (EAV)
**Phạm vi Use Case:** UC-014 đến UC-022  
**Vị trí trong tài liệu SRS:** Mục 2.3 (Master Data Management)  
**Trạng thái đối soát:** Lệch pha nặng 75% do sao chép nhầm mã nguồn và bộ lọc của sản phẩm vào toàn bộ các use case danh mục khác.

---

## 1. BẢNG TỔNG HỢP TRẠNG THÁI TỪNG USE CASE

| Mã UC | Tên Use Case | Source trong SRS | Source thực tế trong Code | Đánh giá |
| :---: | :--- | :--- | :--- | :---: |
| **UC-014** | Manage product group/category | `BMWMS.Web/Pages/Products/Index.cshtml` | `BMWMS.Web/Pages/Categories/Index.cshtml` | ❌ Sai Source & Bảng trường |
| **UC-015** | Manage product | `BMWMS.Web/Pages/Products/Index.cshtml` | `Products/Index.cshtml`, `Create.cshtml`, `Edit.cshtml` | ⚠️ Thiếu form Create/Edit |
| **UC-016** | View product details | `BMWMS.Web/Pages/Products/Details.cshtml` | `BMWMS.Web/Pages/Products/Details.cshtml` | ❌ Sai Normal Flow & Thiếu Tab |
| **UC-017** | Manage product attributes | `BMWMS.Web/Pages/Products/Index.cshtml` | `BMWMS.Web/Pages/Admin/ProductAttributes/Index.cshtml` | ❌ Sai hoàn toàn Source & Bảng trường |
| **UC-018** | Manage units of measure | `BMWMS.Web/Pages/Products/Index.cshtml` | `BMWMS.Web/Pages/Admin/UnitOfMeasures/Index.cshtml` | ❌ Sai hoàn toàn Source & Bảng trường |
| **UC-019** | Manage suppliers | `BMWMS.Web/Pages/Products/Index.cshtml` | `BMWMS.Web/Pages/Admin/Suppliers/Index.cshtml` | ❌ Sai hoàn toàn Source & Bảng trường |
| **UC-020** | View supplier details | `BMWMS.Web/Pages/Products/Details.cshtml` | `BMWMS.Web/Pages/Admin/Suppliers/Detail.cshtml` | ❌ Sai Source Wireframe |
| **UC-021** | View supplier inbound history | `BMWMS.Web/Pages/Products/Index.cshtml` | `BMWMS.Web/Pages/Admin/Suppliers/InboundHistory.cshtml` | ❌ Sai Source Wireframe |
| **UC-022** | Manage customers | `BMWMS.Web/Pages/Products/Index.cshtml` | `CustomersController.cs` / API | ⚠️ Sai Source Wireframe |

---

## 2. CHI TIẾT TỪNG USE CASE VÀ ĐIỂM CẦN CHỈNH SỬA

### UC-014: Manage product group/category (Quản lý nhóm danh mục hàng VLXD)
* **Lỗi trong SRS:**
  * Source ghi nhầm là: `BMWMS.Web/Pages/Products/Index.cshtml`.
  * Field Description copy bộ lọc của sản phẩm: *Từ khóa, Trạng thái, Nhóm sản phẩm, Phương pháp xuất kho, Số dòng/trang*. (Vô lý vì đây chính là trang quản lý nhóm hàng).
* **Code thực tế:**
  * File giao diện: [`BMWMS.Web/Pages/Categories/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Categories/Index.cshtml) và [`Index.cshtml.cs`](file:///c:/bmwms/BMWMS.Web/Pages/Categories/Index.cshtml.cs).
  * API: [`ProductGroupsController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/ProductGroupsController.cs).
  * Giao diện quản lý 15 nhóm ngành hàng VLXD, cấu hình thuộc tính kỹ thuật động EAV và quy tắc luân chuyển kho:
    * Bộ lọc: *Từ khóa (`Keyword`), Trạng thái (`Status`), Số dòng/trang (`PageSize`)*.
    * Thống kê: *Tổng nhóm hàng, Nhóm đang hoạt động, Tổng số thuộc tính EAV đã cấu hình, Số nhóm áp dụng FEFO*.
    * Thao tác: Modal Thêm nhóm mới (`#createGroupModal`), Modal Chỉnh sửa nhóm (`#editGroupModal`), Nút chuyển trạng thái Bật/Khóa, Nút Xóa, Nút Cấu hình thuộc tính EAV theo nhóm.
* **Cần chỉnh sửa trong SRS:**
  * Đổi Source Wireframe sang: `BMWMS.Web/Pages/Categories/Index.cshtml`.
  * Bảng trường: Mã nhóm (`GroupCode`), Tên nhóm (`GroupName`), Mô tả (`Description`), Đơn vị tính cơ sở (`UnitOfMeasureId`), Trạng thái (`Status`).
  * Bổ sung quy tắc: Mỗi nhóm sản phẩm có **1 Đơn vị tính cơ sở thống nhất** và **tập thuộc tính kỹ thuật động riêng**.

---

### UC-015: Manage product (Quản lý danh mục hàng hóa & vật tư)
* **Lỗi trong SRS:** Chỉ liệt kê các trường tìm kiếm trên danh sách, không có trường nào của form khai báo hàng hóa.
* **Code thực tế:**
  * Danh sách: [`Products/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Products/Index.cshtml) (Lọc: Từ khóa, Nhóm danh mục, Luân chuyển FIFO/FEFO, Trạng thái Active/Inactive).
  * Khai báo mới: [`BMWMS.Web/Pages/Products/Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Products/Create.cshtml).
  * Chỉnh sửa: [`BMWMS.Web/Pages/Products/Edit.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Products/Edit.cshtml).
  * Các trường nghiệp vụ thực tế:
    * *Mã SKU (`ProductCode`):* Bắt buộc, duy nhất, in hoa không dấu.
    * *Nhóm danh mục VLXD (`ProductGroupId`):* Bắt buộc. **Đơn vị tính cơ sở (UOM) tự động kế thừa từ nhóm này, người dùng không chọn riêng ĐVT**.
    * *Tên hàng hóa / vật tư (`ProductName`):* Bắt buộc.
    * *Mô tả & Quy cách đóng gói (`Description`)*.
    * *Quy tắc luân chuyển kho (`RotationMethod`):* FIFO (Nhập trước Xuất trước) hoặc FEFO (Hạn trước Xuất trước).
    * *Theo dõi hạn sử dụng (`TrackExpiry`):* Checkbox boolean. Bắt buộc nhập HSD khi nhập kho nếu tích chọn.
    * *Mã vạch (`Barcode`)*.
    * *Định mức tồn kho:* Mức tồn tối thiểu (`MinStockLevel`) và Tối đa (`MaxStockLevel`).
    * *Trọng lượng (`Weight`)* và *Thể tích (`Volume`)* để tính sức chứa kho.
    * *Nhà cung cấp mặc định (`DefaultSupplierId`)*.
    * *Giá trị thuộc tính động EAV (`ProductAttributeValues`)*: Hiển thị động theo nhóm hàng đã chọn.
* **Cần chỉnh sửa trong SRS:**
  * Bổ sung Source cho Create/Edit.
  * Bổ sung bảng Field Description cho form Tạo/Sửa sản phẩm; ghi rõ quy tắc kế thừa ĐVT từ nhóm hàng.

---

### UC-016: View product details (Xem chi tiết sản phẩm)
* **Lỗi trong SRS:**
  * Normal Flow mô tả sai thành quy trình tìm kiếm lọc danh sách (*1. Actor opens screen... 2. System displays filters... 3. Actor enters criteria...*).
  * Field Description chỉ có 3 trường: Mã, Tên, Trạng thái.
* **Code thực tế:**
  * File giao diện: [`BMWMS.Web/Pages/Products/Details.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Products/Details.cshtml).
  * Màn hình xem chi tiết toàn diện gồm:
    * **Header & Badge:** Tên hàng hóa, Mã SKU, Huy hiệu trạng thái (Đang kinh doanh / Ngừng kinh doanh).
    * **4 Thẻ số liệu (Metrics Strip):** Tổng tồn thực tế (On-Hand), Nhóm danh mục, Quy tắc xuất kho (FIFO/FEFO), Theo dõi HSD (TrackExpiry).
    * **3 Tab nội dung:**
      * *Tab 1 (Thông tin & Quy cách):* Mô tả, Barcode, Kích thước, Trọng lượng, Mức tồn Min/Max, ĐVT cơ sở, Nhà cung cấp mặc định.
      * *Tab 2 (Thuộc tính kỹ thuật):* Bảng các thuộc tính động EAV đã nhập.
      * *Tab 3 (Tồn kho theo từng vị trí):* Bảng chi tiết từng Kho, Zone, Rack, Bin, Số lô, Hạn dùng, Số lượng On-Hand, Reserved, Available.
* **Cần chỉnh sửa trong SRS:**
  * Viết lại Normal Flow: Từ danh sách -> bấm Chi tiết -> xem 3 tab và 4 metric cards -> bấm Chỉnh sửa hoặc Quay lại.
  * Cập nhật đầy đủ các trường của 3 tab vào bảng Field Description.

---

### UC-017: Manage product attributes (Quản lý thông số kỹ thuật động - EAV)
* **Lỗi trong SRS:**
  * Source ghi nhầm là: `BMWMS.Web/Pages/Products/Index.cshtml`.
  * Field Description bị copy nhầm thành bộ lọc sản phẩm.
* **Code thực tế:**
  * Màn hình thực tế: [`BMWMS.Web/Pages/Admin/ProductAttributes/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/ProductAttributes/Index.cshtml), [`Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/ProductAttributes/Create.cshtml), [`Edit.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/ProductAttributes/Edit.cshtml).
  * Controller: [`ProductAttributesController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/Inventory/ProductAttributesController.cs).
  * Các trường thực tế:
    * *Mã thuộc tính (`AttributeCode`)*.
    * *Tên thuộc tính (`AttributeName`)*.
    * *Kiểu dữ liệu (`DataType`):* TEXT, NUMBER, SELECT, DATE.
    * *Đơn vị đo (`Unit`)*.
    * *Bắt buộc nhập (`IsRequired`)*.
    * *Tùy chọn (`Options`):* Danh sách giá trị cho kiểu SELECT.
    * *Trạng thái (`Status`):* ACTIVE, INACTIVE.
* **Cần chỉnh sửa trong SRS:**
  * Đổi Source Wireframe sang: `BMWMS.Web/Pages/Admin/ProductAttributes/Index.cshtml`.
  * Thay toàn bộ bảng Field Description sang các trường của thuộc tính động EAV.

---

### UC-018: Manage units of measure (Quản lý đơn vị tính - UOM)
* **Lỗi trong SRS:** Source ghi nhầm là `Products/Index.cshtml` và copy bộ lọc sản phẩm.
* **Code thực tế:**
  * Giao diện: [`BMWMS.Web/Pages/Admin/UnitOfMeasures/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/UnitOfMeasures/Index.cshtml), [`Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/UnitOfMeasures/Create.cshtml), [`Edit.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/UnitOfMeasures/Edit.cshtml).
  * Controller: [`UnitOfMeasuresController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/Inventory/UnitOfMeasuresController.cs).
  * Các trường thực tế: *Mã ĐVT (`UnitCode`), Tên ĐVT (`UnitName`), Số chữ số thập phân (`QuantityScale`: từ 0 đến 4 chữ số), Trạng thái (`Status`)*.
* **Cần chỉnh sửa trong SRS:** Đổi Source sang `Admin/UnitOfMeasures/Index.cshtml` và sửa lại các trường thực tế.

---

### UC-019, UC-020, UC-021: Phân hệ Nhà cung cấp (Suppliers)
* **Lỗi trong SRS:**
  * UC-019 (Manage suppliers) và UC-021 (Inbound history) trỏ nhầm vào `Products/Index.cshtml`.
  * UC-020 (View supplier details) trỏ nhầm vào `Products/Details.cshtml`.
* **Code thực tế:**
  * UC-019: [`BMWMS.Web/Pages/Admin/Suppliers/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Suppliers/Index.cshtml) kèm Create/Edit (Quản lý NCC, Mã số thuế, Người liên hệ, Email, Điện thoại, Địa chỉ, Danh sách hàng hóa cung cấp).
  * UC-020: [`BMWMS.Web/Pages/Admin/Suppliers/Detail.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Suppliers/Detail.cshtml) (Chi tiết NCC, thông tin liên hệ và danh mục hàng hóa cung cấp).
  * UC-021: [`BMWMS.Web/Pages/Admin/Suppliers/InboundHistory.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Suppliers/InboundHistory.cshtml) (Lịch sử các lần nhập hàng từ nhà cung cấp).
* **Cần chỉnh sửa trong SRS:** Đổi toàn bộ Source Wireframe và Field Description sang các trang NCC tương ứng.
