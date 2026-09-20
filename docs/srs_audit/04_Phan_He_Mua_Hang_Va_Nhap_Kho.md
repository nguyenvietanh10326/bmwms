# BÁO CÁO ĐỐI SOÁT PHÂN HỆ 5 & 6: MUA HÀNG (PO) & NHẬP KHO (INBOUND)
**Phạm vi Use Case:** UC-027 đến UC-043  
**Vị trí trong tài liệu SRS:** Mục 2.5 (Purchase Order) và Mục 2.6 (Inbound)  
**Trạng thái đối soát:** Khớp tốt 80%, sai lệch ở các bước Cất hàng (Putaway) và Nhập hàng trả lại (Customer Returns)

---

## 1. BẢNG TỔNG HỢP TRẠNG THÁI TỪNG USE CASE

| Mã UC | Tên Use Case | Source trong SRS | Source thực tế trong Code | Đánh giá |
| :---: | :--- | :--- | :--- | :---: |
| **UC-027** | Create Purchase Order | `PurchaseOrders/Create.cshtml` | `BMWMS.Web/Pages/PurchaseOrders/Create.cshtml` | ✅ Khớp 100% |
| **UC-028** | View Purchase Order list | `PurchaseOrders/Index.cshtml` | `BMWMS.Web/Pages/PurchaseOrders/Index.cshtml` | ✅ Khớp 100% |
| **UC-029** | View Purchase Order details | `PurchaseOrders/Detail.cshtml` | `BMWMS.Web/Pages/PurchaseOrders/Detail.cshtml` | ✅ Khớp 100% |
| **UC-030** | Send PO to supplier | `PurchaseOrders/Detail.cshtml` | `BMWMS.Web/Pages/PurchaseOrders/Detail.cshtml` | ✅ Khớp (Gửi email SMTP) |
| **UC-031** | Cancel Purchase Order | `PurchaseOrders/Detail.cshtml` | `BMWMS.Web/Pages/PurchaseOrders/Detail.cshtml` | ✅ Khớp |
| **UC-032** | Continue partial PO receipt | `PurchaseOrders/Index.cshtml` | `PurchaseOrders/Detail.cshtml` | ⚠️ Nút hành động ở Detail |
| **UC-033** | Close PO with remainder | `PurchaseOrders/Detail.cshtml` | `PurchaseOrders/Detail.cshtml` | ✅ Khớp |
| **UC-034** | View inbound order list | `Admin/Inbound/Index.cshtml` | `BMWMS.Web/Pages/Admin/Inbound/Index.cshtml` | ✅ Khớp |
| **UC-035** | Create inbound order | `Admin/Inbound/Create.cshtml` | `BMWMS.Web/Pages/Admin/Inbound/Create.cshtml` | ✅ Khớp |
| **UC-036** | View inbound order details | `Admin/Inbound/Details.cshtml` | `BMWMS.Web/Pages/Admin/Inbound/Details.cshtml` | ✅ Khớp |
| **UC-037** | Edit inbound order | `Admin/Inbound/Create.cshtml` | `Admin/Inbound/Create.cshtml` (hoặc modal) | ✅ Khớp |
| **UC-038** | Confirm inbound order | `Admin/Inbound/Details.cshtml` | `BMWMS.Web/Pages/Admin/Inbound/Details.cshtml` | ✅ Khớp |
| **UC-039** | Assign inbound processing | `Admin/Inbound/Create.cshtml` | `Admin/Inbound/Details.cshtml` | ⚠️ Gán nhân viên ở Details |
| **UC-040** | Receive goods | `Admin/Inbound/Create.cshtml` | `BMWMS.Web/Pages/Admin/Inbound/Details.cshtml` | ❌ Sai Source Wireframe |
| **UC-041** | Record lot and condition | `Admin/Inbound/Create.cshtml` | `BMWMS.Web/Pages/Admin/Inbound/Details.cshtml` | ❌ Sai Source Wireframe |
| **UC-042** | Put away goods | `Admin/Inbound/Create.cshtml` | `BMWMS.Web/Pages/Admin/Inbound/Putaway.cshtml` | ❌ Sai Source Wireframe |
| **UC-043** | Customer return receipt | `Admin/Inbound/Index.cshtml` | Phân hệ `CustomerReturns/Index, Create, Details` | ❌ Sai phân hệ thực tế |

---

## 2. CHI TIẾT CÁC ĐIỂM SAI LỆCH VÀ HƯỚNG DẪN ĐIỀU CHỈNH

### Nhóm Mua hàng (Purchase Order: UC-027 đến UC-033)
* **Quy trình hoạt động trong Code:**
  1. `PURCHASING_STAFF` tạo đơn mua hàng ([`PurchaseOrders/Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/PurchaseOrders/Create.cshtml)), trạng thái ban đầu là `DRAFT`.
  2. Khi gửi NCC ([`PurchaseOrders/Detail.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/PurchaseOrders/Detail.cshtml)), hệ thống gửi email qua SMTP và chuyển trạng thái sang `ORDERED` (hoặc `CONFIRMED`).
  3. Khi hàng về, tạo phiếu nhập kho Inbound liên kết PO. Nếu nhận đủ -> PO chuyển sang `RECEIVED`. Nếu nhận thiếu (nhận từng phần) -> PO chuyển sang `PARTIALLY_RECEIVED`.
  4. **UC-032:** Nhân viên mở trang chi tiết PO (`PurchaseOrders/Detail.cshtml`), bấm nút "Tạo lệnh nhập tiếp" để tiếp tục nhận đợt sau.
  5. **UC-033:** Nếu nhà cung cấp không còn giao tiếp, nhân viên bấm "Đóng đơn tồn dư" (`Close PO Remainder`), PO chuyển sang `CLOSED`.
* **Cần chỉnh sửa trong SRS:** Sửa Source của UC-032 từ `Index.cshtml` thành `PurchaseOrders/Detail.cshtml`.

---

### Nhóm Nhập kho (Inbound: UC-034 đến UC-043)
1. **UC-040 (Receive goods) & UC-041 (Record lot and condition):**
   * *Lỗi trong SRS:* Trỏ nhầm vào `Admin/Inbound/Create.cshtml`.
   * *Code thực tế:* Thao tác nhận hàng diễn ra tại **[`BMWMS.Web/Pages/Admin/Inbound/Details.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Inbound/Details.cshtml)**:
     * Kiểm đếm số lượng thực nhận (`ReceivedQuantity`) và số lượng từ chối (`RejectedQuantity`) kèm lý do từ chối.
     * Khai báo Số lô (`LotNumber`), Ngày sản xuất (`ManufactureDate`), Hạn sử dụng (`ExpiryDate` - bắt buộc nếu sản phẩm có `TrackExpiry = true`).
     * Đính kèm hình ảnh biên bản nghiệm thu / phiếu giao hàng.
   * *Cần sửa:* Đổi Source Wireframe sang `Admin/Inbound/Details.cshtml`.

2. **UC-042 (Put away goods - Cất hàng vào vị trí lưu trữ):**
   * *Lỗi trong SRS:* Trỏ nhầm vào `Admin/Inbound/Create.cshtml`.
   * *Code thực tế:* Trong code có màn hình cất hàng riêng là **[`BMWMS.Web/Pages/Admin/Inbound/Putaway.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Inbound/Putaway.cshtml)** và API `PUT api/inbounds/{id}/putaway`:
     * Hệ thống kiểm tra sức chứa vị trí (`CapacityEvaluationService`), gợi ý hoặc cho phép nhân viên chọn Bin đích.
     * Khi bấm xác nhận cất hàng -> Ghi nhận giao dịch `PUT_AWAY` trong `InventoryTransactions` và tăng On-hand tại vị trí Bin đó.
   * *Cần sửa:* Đổi Source Wireframe sang `BMWMS.Web/Pages/Admin/Inbound/Putaway.cshtml`.

3. **UC-043 (Process customer return receipt - Nhập hàng khách trả lại):**
   * *Lỗi trong SRS:* Chỉ trỏ chung vào `Admin/Inbound/Index.cshtml`.
   * *Code thực tế:* Hệ thống xây dựng hẳn **một phân hệ độc lập hoàn chỉnh** cho Khách hàng trả hàng:
     * Màn hình danh sách đơn trả: [`BMWMS.Web/Pages/CustomerReturns/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/CustomerReturns/Index.cshtml).
     * Màn hình tạo đơn khách trả: [`BMWMS.Web/Pages/CustomerReturns/Create.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/CustomerReturns/Create.cshtml).
     * Màn hình chi tiết & duyệt đơn trả: [`BMWMS.Web/Pages/CustomerReturns/Details.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/CustomerReturns/Details.cshtml).
     * Màn hình duyệt cho phép trả: [`BMWMS.Web/Pages/Admin/Inbound/AuthorizeReturn.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Inbound/AuthorizeReturn.cshtml).
     * Màn hình lập phiếu nhập hàng trả vào kho: [`BMWMS.Web/Pages/Admin/Inbound/ReturnReceipt.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Inbound/ReturnReceipt.cshtml).
     * Controller API: [`CustomerReturnsController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/CustomerReturnsController.cs).
   * *Cần sửa:* Viết lại đặc tả UC-043 phản ánh đúng phân hệ Customer Returns với các màn hình thực tế trên.
