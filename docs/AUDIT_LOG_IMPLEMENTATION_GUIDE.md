# BMWMS — Kế hoạch và hướng dẫn tích hợp Audit Log

## 1. Mục tiêu

Audit Log là nhật ký bất biến ở tầng ứng dụng, trả lời được bốn câu hỏi:

1. Ai thực hiện hành động?
2. Hành động gì, thuộc phân hệ nào?
3. Tác động lên đối tượng và bản ghi nào?
4. Dữ liệu nghiệp vụ nào thay đổi trước và sau?

Audit Log không thay thế `InventoryTransactions`. `InventoryTransactions` là sổ biến động số lượng tồn; `AuditLogs` là lịch sử thao tác người dùng và thay đổi trạng thái/dữ liệu nghiệp vụ.

## 2. Phạm vi baseline đã triển khai

- Chỉ `SYSTEM_ADMIN` được truy cập danh sách và chi tiết toàn hệ thống.
- Danh sách có phân trang và lọc theo từ khóa, phân hệ, hành động, loại đối tượng, ID đối tượng, người thực hiện và khoảng ngày.
- Chi tiết hiển thị metadata, snapshot trước/sau và bảng so sánh từng trường.
- Không có API hoặc nút sửa/xóa audit log.
- Các trường mật khẩu, hash, token, OTP, secret, API key, authorization và cookie được che tại `AuditLogService` trước khi ghi.
- Dữ liệu cũ không đúng JSON không làm hỏng màn chi tiết; màn hình chỉ báo snapshot không đọc được.
- Tài khoản và nhà cung cấp đã được chuyển sang writer chung làm ví dụ tích hợp.

## 3. Business Rules

| ID | Quy tắc |
|---|---|
| BR-AUD-01 | Mọi create/update/delete/confirm/cancel/assign/approve/reject/post/override quan trọng phải có audit event. |
| BR-AUD-02 | `UserID` là người thực hiện, không phải chủ thể bị thay đổi. |
| BR-AUD-03 | `EntityName` dùng hằng số chuẩn; `EntityID` dùng ID hoặc mã nghiệp vụ ổn định. |
| BR-AUD-04 | `ActionType` viết hoa dạng `VERB_ENTITY`, tối đa 50 ký tự. |
| BR-AUD-05 | Chỉ ghi snapshot các trường nghiệp vụ cần truy vết; không serialize toàn bộ EF entity/navigation graph. |
| BR-AUD-06 | Không ghi password, hash, token, OTP, cookie, secret hoặc nội dung credential. |
| BR-AUD-07 | Sự kiện và thay đổi nghiệp vụ phải cùng `SaveChanges`/transaction khi có thể. |
| BR-AUD-08 | Thời gian lưu UTC; UI và bộ lọc ngày dùng múi giờ Việt Nam (UTC+7). |
| BR-AUD-09 | Audit log chỉ append; sửa sai nghiệp vụ phải tạo event/giao dịch bù, không sửa lịch sử. |
| BR-AUD-10 | Danh sách luôn sắp xếp `CreatedAt DESC, AuditLogID DESC` để phân trang ổn định. |
| BR-AUD-11 | Không đưa exception stack trace hoặc dữ liệu request thô vào snapshot. |
| BR-AUD-12 | Quyền xem audit log độc lập với quyền tạo event; module nghiệp vụ không được tự đọc lịch sử toàn hệ thống. |

## 4. Kiến trúc dùng chung

```text
Module Service
  └─ IAuditLogService
       ├─ StageAsync(event)  -> add vào DbContext, chờ SaveChanges nghiệp vụ
       ├─ RecordAsync(event) -> ghi ngay cho sự kiện độc lập
       ├─ GetAuditLogsAsync(filter)
       └─ GetAuditLogAsync(id)
            └─ IAuditLogRepository -> dbo.AuditLogs

SYSTEM_ADMIN
  └─ /Admin/AuditLogs
       └─ AuditLogApiService -> /api/audit-logs
```

Các thành phần chính:

- `BMWMS.Business/DTOs/Audit/AuditCatalog.cs`: danh sách module, action, entity và nhãn tiếng Việt.
- `BMWMS.Business/Interfaces/IAuditLogService.cs`: contract mọi module dùng.
- `BMWMS.Business/Services/AuditLogService.cs`: validation, serialize, che dữ liệu nhạy cảm, diff snapshot.
- `BMWMS.Repository/Interfaces/IAuditLogRepository.cs`: truy vấn và append log.
- `BMWMS.API/Controllers/AuditLogsController.cs`: API chỉ dành cho System Admin.
- `BMWMS.Web/Pages/Admin/AuditLogs`: danh sách và chi tiết.

## 5. Cách tích hợp vào module khác

### 5.1 Inject writer

```csharp
private readonly IAuditLogService _auditLogService;

public ProductService(
    IProductRepository productRepository,
    IAuditLogService auditLogService)
{
    _productRepository = productRepository;
    _auditLogService = auditLogService;
}
```

### 5.2 Create — ưu tiên StageAsync

Khi repository sắp gọi `SaveChangesAsync`, stage log trước để product và log được commit cùng lần. Với create chưa có numeric ID, dùng mã nghiệp vụ ổn định.

```csharp
var product = new Product
{
    ProductCode = dto.ProductCode,
    ProductName = dto.ProductName,
    ProductGroupId = dto.ProductGroupId,
    Status = "ACTIVE"
};

await _auditLogService.StageAsync(new AuditEventDto
{
    UserId = actorUserId,
    ActionType = "CREATE_PRODUCT",
    EntityName = AuditEntities.Product,
    EntityId = product.ProductCode,
    NewValues = new
    {
        product.ProductCode,
        product.ProductName,
        product.ProductGroupId,
        product.Status
    },
    IpAddress = ipAddress
});

await _productRepository.AddAsync(product); // SaveChanges lưu cả Product và AuditLog
```

### 5.3 Update — chụp old snapshot trước khi gán giá trị mới

```csharp
var oldValues = new
{
    product.ProductName,
    product.ProductGroupId,
    product.Status
};

product.ProductName = dto.ProductName;
product.ProductGroupId = dto.ProductGroupId;
product.Status = dto.Status;

await _auditLogService.StageAsync(new AuditEventDto
{
    UserId = actorUserId,
    ActionType = "UPDATE_PRODUCT",
    EntityName = AuditEntities.Product,
    EntityId = product.ProductId.ToString(),
    OldValues = oldValues,
    NewValues = new
    {
        product.ProductName,
        product.ProductGroupId,
        product.Status
    },
    IpAddress = ipAddress
});

await _productRepository.UpdateAsync(product);
```

### 5.4 Khi nào dùng RecordAsync

Dùng `RecordAsync` cho sự kiện độc lập không còn một `SaveChanges` nghiệp vụ phía sau, ví dụ đăng nhập, đăng xuất hoặc export report. Không dùng `RecordAsync` sau một update quan trọng nếu có thể stage event trước `SaveChanges`.

```csharp
await _auditLogService.RecordAsync(new AuditEventDto
{
    UserId = actorUserId,
    ActionType = "EXPORT_INVENTORY_REPORT",
    EntityName = "Report",
    EntityId = exportJobId,
    NewValues = new { filter.FromDate, filter.ToDate, RowCount = rows.Count },
    IpAddress = ipAddress
});
```

### 5.5 Deep-link từ màn module

Màn chi tiết Product, PO, SO, Inbound hoặc Outbound có thể thêm liên kết:

```text
/Admin/AuditLogs?Filter.ModuleCode=PRODUCT&Filter.EntityName=Product&Filter.EntityId=125
```

Không tạo lại màn log riêng cho từng module. Mọi module dùng chung màn này và truyền bộ lọc qua query string.

## 6. Quy ước module và entity

Khi thêm entity mới:

1. Thêm hằng số vào `AuditEntities` nếu dùng nhiều nơi.
2. Gán entity vào đúng module trong `AuditCatalog.ModuleEntities`.
3. Thêm nhãn tiếng Việt trong `AuditCatalog.GetEntityName`.
4. Dùng action nhất quán, ví dụ `CREATE_PRODUCT`, `UPDATE_PRODUCT`, `CONFIRM_PURCHASE_ORDER`, `ASSIGN_INBOUND`, `PICK_OUTBOUND`.

Không dùng chuỗi mơ hồ như `SAVE`, `PROCESS`, `DO_ACTION` hoặc dùng tên màn hình làm action.

## 7. API

| Method | Endpoint | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/audit-logs` | SYSTEM_ADMIN | Danh sách, lọc và phân trang |
| GET | `/api/audit-logs/{id}` | SYSTEM_ADMIN | Chi tiết và diff snapshot |
| GET | `/api/audit-logs/options` | SYSTEM_ADMIN | Danh sách module/action/entity/actor cho bộ lọc |

Không cung cấp POST/PUT/DELETE audit log ra public API. Module ghi log qua Business Service nội bộ.

## 8. Database và triển khai

Bảng hiện tại được giữ nguyên để tương thích dữ liệu cũ. Chạy patch sau trên database đã tồn tại:

```text
docs/schema_patch_audit_log_indexes.sql
```

Patch chỉ bổ sung các index phục vụ sort/lọc theo thời gian, user, entity và action. `Database_v3.sql` đã chứa cùng cấu hình cho database tạo mới.

## 9. Acceptance checklist cho pull request của module

- [ ] Ghi đúng actor, không lấy target user làm actor.
- [ ] Có action/entity chuẩn và EntityID ổn định.
- [ ] Có old/new snapshot cho update hoặc change status.
- [ ] Không có password/token/hash/OTP/secret trong source snapshot.
- [ ] Dùng `StageAsync` trước `SaveChanges` nếu mutation và audit phải nguyên tử.
- [ ] Thao tác thất bại không tạo log thành công giả.
- [ ] Link lọc từ màn module mở đúng lịch sử entity.
- [ ] System Admin xem được bản ghi ở danh sách và trang chi tiết.
- [ ] User ngoài System Admin nhận 403 từ API và không thấy menu.
- [ ] Phân trang ổn định khi hai event có cùng giây.

## 10. Hạng mục kế tiếp sau baseline

- Chốt chính sách lưu trữ/archiving trước khi áp dụng job xóa dữ liệu.
- Thêm `CorrelationId`/`RequestId` nếu cần gom nhiều event của cùng một command.
- Chuẩn hóa Unit of Work cho các service đang gọi nhiều `SaveChanges` để business mutation và audit luôn cùng transaction.
- Bổ sung module-scoped permission nếu Warehouse Manager cần xem riêng log vận hành; không mở toàn bộ log tài khoản/bảo mật.
- Bổ sung kiểm thử integration với SQL Server cho concurrent write, time range và index plan khi dữ liệu lớn.
