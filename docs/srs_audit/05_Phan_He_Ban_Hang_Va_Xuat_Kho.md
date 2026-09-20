# BÁO CÁO ĐỐI SOÁT PHÂN HỆ 7 & 8: BÁN HÀNG (SO) & XUẤT KHO (OUTBOUND)
**Phạm vi Use Case:** UC-044 đến UC-056  
**Vị trí trong tài liệu SRS:** Mục 2.7 (Sales Order) và Mục 2.8 (Outbound)  
**Trạng thái đối soát:** Khớp tốt 85%, sai lệch ở màn hình Chỉnh sửa và Nhặt hàng (Picking)

---

## 1. BẢNG TỔNG HỢP TRẠNG THÁI TỪNG USE CASE

| Mã UC | Tên Use Case | Source trong SRS | Source thực tế trong Code | Đánh giá |
| :---: | :--- | :--- | :--- | :---: |
| **UC-044** | Create Sales Order | `SalerOrder/Create.cshtml` | `BMWMS.Web/Pages/SalerOrder/Create.cshtml` | ✅ Khớp 100% |
| **UC-045** | View Sales Order list | `SalerOrder/Index.cshtml` | `BMWMS.Web/Pages/SalerOrder/Index.cshtml` | ✅ Khớp 100% |
| **UC-046** | View Sales Order details | `SalerOrder/Details.cshtml` | `BMWMS.Web/Pages/SalerOrder/Details.cshtml` | ✅ Khớp 100% |
| **UC-047** | Create outbound order | `OutboundOrders/Create.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Create.cshtml` | ✅ Khớp 100% |
| **UC-048** | View outbound order list | `OutboundOrders/Index.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Index.cshtml` | ✅ Khớp 100% |
| **UC-049** | View outbound order details | `OutboundOrders/Details.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Details.cshtml` | ✅ Khớp 100% |
| **UC-050** | Edit outbound order | `OutboundOrders/Create.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Edit.cshtml` | ❌ Sai Source Wireframe |
| **UC-051** | Approve outbound order | `OutboundOrders/Details.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Details.cshtml` | ✅ Khớp |
| **UC-052** | Start outbound processing | `OutboundOrders/Details.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Process.cshtml` | ❌ Sai Source Wireframe |
| **UC-053** | Pick goods | `OutboundOrders/Index.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Process.cshtml` | ❌ Sai hoàn toàn Source |
| **UC-054** | Complete outbound order | `OutboundOrders/Details.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Details.cshtml` | ✅ Khớp |
| **UC-055** | Cancel outbound order | `OutboundOrders/Details.cshtml` | `OutboundOrders/Details.cshtml` / `Orders/CancelApproved.cshtml` | ✅ Khớp |
| **UC-056** | Close sales remainder | `OutboundOrders/Details.cshtml` | `BMWMS.Web/Pages/OutboundOrders/Details.cshtml` | ✅ Khớp |

---

## 2. CHI TIẾT CÁC ĐIỂM SAI LỆCH VÀ HƯỚNG DẪN ĐIỀU CHỈNH

### Nhóm Bán hàng (Sales Order: UC-044 đến UC-046)
* **Quy trình hoạt động trong Code:**
  * Nhân viên bán hàng (`SALES_STAFF`) tạo đơn đặt hàng tại [`BMWMS.Web/Pages/SalerOrder/Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/SalerOrder/Create.cshtml).
  * API: [`SalesOrdersController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/Inventory/SalesOrdersController.cs).
  * **Quy tắc giữ tồn tự động (Automatic Inventory Reservation):** Khi đơn bán hàng được xác nhận (`CONFIRMED`), hệ thống tự động kiểm tra tồn khả dụng (`Available = OnHand - Reserved`) và tạo bản ghi giữ tồn trong bảng `InventoryReservations` theo nguyên tắc:
    * Sản phẩm có `RotationMethod = 'FEFO'`: Ưu tiên giữ lô có hạn sử dụng gần nhất trước.
    * Sản phẩm có `RotationMethod = 'FIFO'`: Ưu tiên giữ lô nhập kho trước.
  * Nếu không đủ hàng khả dụng, hệ thống thông báo lỗi thiếu hàng và không cho xác nhận đơn.
* **Đánh giá SRS:** Đặc tả cơ bản khớp tốt.

---

### Nhóm Xuất kho (Outbound: UC-047 đến UC-056)
1. **UC-050 (Edit outbound order):**
   * *Lỗi trong SRS:* Source ghi nhầm là `OutboundOrders/Create.cshtml`.
   * *Code thực tế:* Hệ thống có trang riêng: **[`BMWMS.Web/Pages/OutboundOrders/Edit.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/OutboundOrders/Edit.cshtml)** và [`Edit.cshtml.cs`](file:///c:/bmwms/BMWMS.Web/Pages/OutboundOrders/Edit.cshtml.cs) cho phép chỉnh sửa phiếu xuất khi còn ở trạng thái `DRAFT`.
   * *Cần sửa:* Đổi Source sang `OutboundOrders/Edit.cshtml`.

2. **UC-052 (Start outbound processing) & UC-053 (Pick goods - Lấy hàng):**
   * *Lỗi trong SRS:* UC-052 ghi Source là `Details.cshtml`, UC-053 ghi nhầm sang `Index.cshtml`!
   * *Code thực tế:* Trong code có màn hình chuyên dụng cho nhân viên kho đi lấy hàng là **[`BMWMS.Web/Pages/OutboundOrders/Process.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/OutboundOrders/Process.cshtml)** và [`Process.cshtml.cs`](file:///c:/bmwms/BMWMS.Web/Pages/OutboundOrders/Process.cshtml.cs):
     * Màn hình này hiển thị **Danh sách nhặt hàng (Pick List)** được tối ưu theo vị trí: Ô vị trí (Bin) cần đến lấy -> Lô hàng cần lấy -> Số lượng cần lấy.
     * Nhân viên quét mã hoặc tích chọn xác nhận số lượng thực tế đã nhặt vào xe đẩy / pallet.
     * Khi hoàn tất nhặt hàng, trạng thái chuyển sang `PICKED` để chuyển tiếp sang đóng gói và xuất kho.
   * *Cần sửa:* Đổi Source Wireframe của cả UC-052 và UC-053 sang `BMWMS.Web/Pages/OutboundOrders/Process.cshtml`.

3. **UC-055 (Cancel outbound order) & Hủy đơn đã duyệt:**
   * Trong code có hỗ trợ thêm trang chuyên biệt hủy đơn xuất đã duyệt: [`BMWMS.Web/Pages/Orders/CancelApproved.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Orders/CancelApproved.cshtml).
   * Khi hủy đơn đã duyệt, hệ thống tự động giải phóng lượng hàng đang giữ (`RELEASE_RESERVATION`) để trả lại số lượng khả dụng cho kho. Cần bổ sung ghi chú quy tắc này vào SRS.
