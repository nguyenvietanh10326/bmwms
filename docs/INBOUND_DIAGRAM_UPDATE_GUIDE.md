# Hướng dẫn cập nhật diagram theo luồng Inbound đã sửa

Tài liệu này mô tả trạng thái code sau đợt hardening luồng Inbound. Khi sửa ảnh trong `BMWMS_Business_Process_Swimlane.drawio` và `BMWMS_All_Use_Case_Sequence_Diagrams.drawio`, dùng đúng thuật ngữ và thứ tự dưới đây.

## 1. Quy ước nghiệp vụ đang áp dụng

- Một hệ thống chỉ có một kho đang hoạt động. Phiếu không yêu cầu người dùng chọn kho.
- Phiếu nhập mua tham chiếu PO; phiếu nhận hàng khách trả tham chiếu SO.
- Không có loại phiếu “Nhập bổ sung cho lệnh thiếu”. Phần PO chưa nhận đạt được lập bằng một phiếu nhập PO thông thường khác.
- Nhân viên kho chỉ xử lý phiếu được phân công; một nhân viên không đồng thời phụ trách phiếu nhập/xuất khác đang hoạt động.
- “Lớp nhận” là định danh kỹ thuật cho từng lần nhận, dùng giữ ngày nhận/HSD và làm nền cho FIFO/FEFO/LIFO. Nó không phải “lô NCC” bắt buộc.
- ProductFixedLocation là vị trí khuyến nghị. Nhân viên được chọn bin hợp lệ khác nhưng phải nhập lý do.
- Kiểm nhận và cất hàng là hai bước của Inbound, không tạo hoặc chuyển trách nhiệm sang Transfer Order.

## 2. State machine cần vẽ

```text
DRAFT
  └─ Xác nhận + đã phân công Staff → READY
READY
  └─ Lưu lần kiểm nhận đầu tiên → RECEIVING
RECEIVING
  ├─ Lưu tiến độ → RECEIVING
  └─ Chốt đủ hoặc chốt thiếu có lý do → RECEIVED
RECEIVED
  ├─ Cất một phần → RECEIVED
  └─ Toàn bộ hàng đạt đã vào bin → PUTAWAY_COMPLETED
DRAFT/READY
  └─ Hủy có lý do → CANCELLED
```

Trong database hiện tại, `READY`, `RECEIVING`, `RECEIVED`, `PUTAWAY_COMPLETED` lần lượt được ánh xạ từ `ASSIGNED`, `IN_PROGRESS`, `COMPLETED` và trạng thái đã post toàn bộ chi tiết GOOD. Diagram chỉ dùng tên nghiệp vụ phía trên, không dùng tên lưu trữ nội bộ.

## 3. Sửa Business Process BP-01 - Nhập mua theo PO

### Lane Purchasing Staff

1. Giữ bước “Tạo Purchase Order”.
2. Giữ bước “Xác nhận PO / gửi thông báo hoặc email cho người có trách nhiệm”. Không vẽ Purchasing Staff tự tạo rồi tự duyệt nếu quy trình nhóm đã tách quyền xác nhận.
3. Đổi bước tạo Inbound thành: **“Tạo phiếu nhập tham chiếu PO; hệ thống giữ phần số lượng dự kiến còn lại”**.
4. Thêm bước: **“Chọn Warehouse Staff đang rảnh và chuyển phiếu sang READY”**.
5. Sau khi nhận thông báo chốt kiểm nhận, nếu PO chưa nhận đủ hàng đạt thì nhánh xử lý là **“Tạo phiếu nhập PO thông thường cho phần PO còn lại”**, không phải “Tạo phiếu nhập bổ sung”.

### Lane BMWMS

1. Sau “Tạo phiếu nhập”, thêm kiểm tra:
   - PO ở trạng thái đủ điều kiện;
   - nhà cung cấp đang hoạt động;
   - số lượng dự kiến không vượt phần còn lại;
   - đơn vị nguyên/thập phân đúng `QuantityScale`;
   - Staff đúng role và không có nhiệm vụ đang hoạt động.
2. Khi phiếu còn DRAFT hoặc đang xử lý, số dự kiến giữ chỗ cho PO. Khi phiếu đã RECEIVED, chỉ số hàng GOOD thực tế được tính là đã nhận đạt.
3. Sau lần lưu kiểm nhận đầu tiên: đổi trạng thái `READY → RECEIVING`, ghi audit log; chưa cộng tồn tại bin.
4. Khi chốt kiểm nhận: kiểm tra mọi dòng có kết quả hoặc có quyết định chốt thiếu kèm lý do; đổi `RECEIVING → RECEIVED`; cập nhật PO thành `PARTIALLY_RECEIVED` hoặc `COMPLETED`; ghi audit log.
5. Khi xác nhận putaway: kiểm tra bin, khu/kệ đang hoạt động, quyền putaway, số lượng còn chờ và lý do nếu bỏ qua vị trí khuyến nghị; post giao dịch `INBOUND` vào đúng bin; ghi audit log.

### Lane Warehouse Staff

Thay cụm “Receive/Inspect → Goods accepted? → Confirm receipt → Put away” bằng các bước chi tiết:

1. **Mở phiếu được phân công (READY).**
2. **Kiểm đếm từng mặt hàng**: thực giao, đạt, hỏng, chờ xử lý, HSD nếu FEFO/quản lý HSD, ghi chú tình trạng.
3. Gateway **“Thực giao = Đạt + Hỏng + Chờ xử lý?”**. Nếu không, hệ thống không cho lưu.
4. Gateway **“Hỏng hoặc chờ xử lý > 0?”**. Nếu có, bắt buộc lý do và ghi vào QUARANTINE; nếu không, hàng GOOD chờ cất tại RECEIVING/STAGING.
5. **Lưu tiến độ kiểm nhận** (có thể lặp nhiều lần).
6. Gateway **“Mọi dòng đã đủ?”**. Nếu chưa đủ, Staff phải chọn **“Chốt thiếu”** và nhập lý do cho từng dòng.
7. **Hoàn tất kiểm nhận (RECEIVED).**
8. **Cất hàng thực tế; tìm/chọn Khu → Kệ → Bin; chia nhiều bin nếu cần.**
9. Gateway **“Bin khác vị trí khuyến nghị?”**. Nếu có, nhập lý do.
10. **Xác nhận số lượng và vị trí thực tế**. Lặp đến khi toàn bộ GOOD đã cất, sau đó `PUTAWAY_COMPLETED`.

## 4. Sửa Business Process BP-02 - Khách trả hàng theo SO

1. Giữ bước Sales Staff xác minh SO đã xuất và số lượng còn có thể trả.
2. Bước tạo phiếu ghi rõ: **“Tạo Inbound loại SALES_RETURN tham chiếu SO”**.
3. Dùng cùng state machine và màn kiểm nhận như PO, nhưng mọi hàng trả đều phải qua phân loại chất lượng.
4. Gateway chất lượng:
   - dùng lại được → GOOD tại RECEIVING/STAGING, sau đó putaway vào bin;
   - hỏng/chưa quyết định → DAMAGED/QUARANTINED tại QUARANTINE;
   - không tự động cộng hàng hỏng/chờ xử lý vào tồn khả dụng.
5. Mỗi lần nhận hàng trả tạo một lớp nhận mới để không trộn tuổi tồn với lần bán/nhập cũ.

## 5. Sửa Sequence Diagram UC22 - Kiểm nhận hàng

### Participants

Đổi hoặc bổ sung participants theo thứ tự:

```text
Warehouse Staff → Receipt UI → InboundsController → InboundService
→ QuantityRules / Assignment validation → BMWMS DB → AuditLog
```

### Main flow

1. `GET /Admin/Inbound/Receipt/{id}`.
2. `GET /api/inbounds/{id}`; API kiểm tra Staff chính là người được phân công.
3. UI hiển thị Expected, Delivered đã lưu, Remaining, QuantityScale, RotationMethod và yêu cầu HSD.
4. Staff nhập một hoặc nhiều dòng.
5. UI kiểm tra sơ bộ `Delivered = Accepted + Damaged + Hold`.
6. `POST /api/inbounds/{id}/receive-batch` với các số lượng nullable; dòng để trống được bỏ qua.
7. Service kiểm tra actor, state, sản phẩm, số lượng còn lại, precision, HSD FEFO, ghi chú hỏng/hold.
8. Service tạo lớp nhận riêng, chi tiết GOOD tại RECEIVING/STAGING và DAMAGED/HOLD tại QUARANTINE.
9. Service đổi `READY → RECEIVING`, stage audit log và commit transaction.
10. UI trả thông báo “Đã lưu tiến độ; tồn tại bin chưa thay đổi”.
11. Khi chốt: `POST /api/inbounds/{id}/complete-receipt` với `Decisions[{ itemId, closeAsShort, reason }]`.
12. Service từ chối nếu còn dòng thiếu mà không có quyết định/lý do.
13. Service tính `Shortage = Expected - Delivered`, chuyển trạng thái sang `RECEIVED`, cập nhật PO theo tổng GOOD, ghi audit và thông báo.

### Alternative/error flows phải thêm

- `403`: Staff không được phân công.
- `409`: trạng thái phiếu không cho thao tác hoặc xung đột lưu.
- `422`: sai precision, tổng phân loại lệch, vượt số còn lại, thiếu HSD hoặc thiếu lý do.
- `500`: lỗi không xác định; không hiển thị inner exception/database detail cho người dùng.

Xóa các message cũ “LotNumber required cho mọi sản phẩm” và “lưu receipt đồng thời update inventory/bin”. Code hiện tại chỉ tăng tồn bin ở bước putaway.

## 6. Sửa Sequence Diagram UC23 - Cất hàng và xác nhận vị trí

### Participants

```text
Warehouse Staff → Putaway UI → InboundsController → InboundService
→ StorageLocation / ProductFixedLocation → InventoryTransactions → AuditLog
```

### Main flow

1. `GET /Admin/Inbound/Putaway/{id}?itemId=...&lotId=...`.
2. API chỉ cho mở khi phiếu ở `RECEIVED`, còn chi tiết GOOD chưa post và đúng Staff được phân công.
3. Service trả danh sách bin theo cấu trúc Khu/Kệ/Bin, trạng thái, tồn hiện có và cờ khuyến nghị.
4. Staff tìm kiếm/lọc vị trí, nhập số lượng; có thể thêm nhiều dòng để chia nhiều bin.
5. Nếu chọn bin không được khuyến nghị, Staff nhập `OverrideReason`.
6. `POST /api/inbounds/{id}/putaway`.
7. Service kiểm tra đúng kho, LocationType=BIN, `IsPutawayAllowed`, trạng thái khu/kệ/bin, precision và tổng không vượt số GOOD còn lại.
8. Service tách receipt detail khi chia nhiều bin và tạo InventoryTransaction `INBOUND` cho từng phân bổ.
9. Trigger cập nhật Inventory tại đúng bin; fixed location không còn là ràng buộc cứng.
10. Stage audit `PUTAWAY_INBOUND`, commit.
11. Nếu còn GOOD chưa cất: giữ `RECEIVED`; nếu hết: hiển thị `PUTAWAY_COMPLETED`.

## 7. Điểm chuyển tiếp cần ghi chú trong đặc tả kỹ thuật

Đợt code này vẫn dùng `ProductLots` như bảng lớp nhận kỹ thuật và vẫn post tồn lần đầu tại bin khi putaway. Trong đợt refactor dữ liệu theo “Đặc tả nghiệp vụ chốt và kế hoạch thay đổi hệ thống”, nên thay bằng `ReceiptStockLayer` riêng và ledger cặp `PUTAWAY_OUT/PUTAWAY_IN` nếu nhóm quyết định quản lý tồn vật lý tại RECEIVING. Khi refactor đó được triển khai, UC22/UC23 phải bổ sung `InventoryPostingService` và hai bút toán; không đưa các bút toán này vào diagram “as-is” trước khi code/database thực sự hỗ trợ.
