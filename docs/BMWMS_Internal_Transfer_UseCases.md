# Nghiep vu chuyen kho noi bo

Tai lieu nay mo ta lai luong chuyen kho noi bo theo diagram BP-04 va cac use case/API da implement.

## Luong nghiep vu chinh

| Buoc | Actor | Trang thai truoc | Hanh dong | Trang thai sau | Ket qua du lieu |
|---|---|---|---|---|---|
| 1 | Warehouse Staff/Manager | - | Tao phieu chuyen kho | DRAFT | Tao `TransferOrders` va `TransferOrderDetails`, chua cap nhat ton kho |
| 2 | Warehouse Manager/Admin | DRAFT | Sua phieu chuyen kho | DRAFT | Cap nhat assign staff, due date, notes va cac dong hang |
| 3 | Warehouse Manager/Admin | DRAFT | Phe duyet phieu | ASSIGNED | Ghi nguoi duyet/thoi gian duyet, phieu cho xuat |
| 4 | Warehouse Manager/Admin | DRAFT | Tu choi phieu | CANCELLED | Phieu bi huy, khong cho thuc hien |
| 5 | Warehouse Staff/Manager | ASSIGNED | Xac nhan xuat kho nguon | IN_PROGRESS | Ghi `MovedQuantity`, hang dang di chuyen, chua post ton kho |
| 6 | Warehouse Staff/Manager | IN_PROGRESS, chua post ton kho | Xac nhan nhap kho dich | COMPLETED | Tao `TRANSFER_OUT` va `TRANSFER_IN`, ton kho nguon/dich duoc cap nhat va phieu hoan tat |

> Ghi chu: Status `APPROVED` cu van duoc ho tro nhu alias cua `ASSIGNED` de tuong thich du lieu cu.

## Use case

| Ma | Ten use case | Actor chinh | Dieu kien dau vao | Endpoint | Ket qua |
|---|---|---|---|---|---|
| TC-01 | Tao phieu chuyen kho | Warehouse Staff/Manager | Co vi tri nguon/dich hop le, cung warehouse, ton kho nguon du so luong | `POST /api/transfers/create` | Phieu moi o `DRAFT` |
| TC-02 | Sua phieu truoc phe duyet | Warehouse Manager/Admin | Phieu `DRAFT`, ton kho va vi tri van hop le | `PUT /api/transfers/{id}` | Phieu van o `DRAFT` voi du lieu moi |
| TC-03 | Phe duyet phieu | Warehouse Manager/Admin | Phieu `DRAFT`, ton kho va vi tri van hop le | `POST /api/transfers/{id}/approve` | Phieu sang `ASSIGNED` |
| TC-04 | Tu choi phieu | Warehouse Manager/Admin | Phieu chua hoan thanh | `POST /api/transfers/{id}/reject` | Phieu sang `CANCELLED` |
| TC-05 | Xac nhan xuat | Warehouse Staff/Manager | Phieu `ASSIGNED`/`APPROVED`, ton kho nguon du so luong | `POST /api/transfers/{id}/issue` | Phieu sang `IN_PROGRESS`, ghi so luong da xuat |
| TC-06 | Xac nhan nhap | Warehouse Staff/Manager | Phieu `IN_PROGRESS`, chua co giao dich transfer posted | `POST /api/transfers/{id}/receive` | Tao `TRANSFER_OUT`/`TRANSFER_IN` va phieu sang `COMPLETED` |

## Bang du lieu phieu chuyen kho

Danh sach phieu chuyen kho tra ve qua `GET /api/transfers` da duoc bo sung cac cot van hanh sau:

| Cot/field | Y nghia | Nguon du lieu |
|---|---|---|
| `TransferOrderNumber` | Ma phieu chuyen kho | `TransferOrders.TransferOrderNumber` |
| `WarehouseName` | Kho chua cac vi tri nguon/dich | `Warehouses.WarehouseName` |
| `SourceLocationSummary` | Tom tat vi tri nguon | Tong hop tu `TransferOrderDetails.SourceLocation` |
| `DestinationLocationSummary` | Tom tat vi tri dich | Tong hop tu `TransferOrderDetails.DestLocation` |
| `TotalItems` | So dong hang trong phieu | Count `TransferOrderDetails` |
| `TotalRequestedQuantity` | Tong so luong yeu cau | Sum `RequestedQuantity` |
| `TotalMovedQuantity` | Tong so luong da xuat/di chuyen | Sum `MovedQuantity` |
| `ProgressLabel` | Nhan tien do hien tai | Suy ra tu `Status` va `InventoryPosted` |
| `NextAction` | Hanh dong ke tiep nguoi dung can lam | Suy ra tu `Status` va `InventoryPosted` |
| `InventoryPosted` | Da cap nhat ton kho hay chua | Kiem tra giao dich `TRANSFER_OUT`/`TRANSFER_IN` tren cac detail |
| `Status` | Trang thai ky thuat cua phieu | `DRAFT`, `ASSIGNED`, `IN_PROGRESS`, `COMPLETED`, `CANCELLED` |

## Mapping trang thai hien thi

| Status | `InventoryPosted` | Hien thi | Hanh dong tiep |
|---|---:|---|---|
| DRAFT | false | Cho phe duyet | Quan ly phe duyet hoac tu choi |
| ASSIGNED/APPROVED | false | Da duyet - Cho xuat | Nhan vien xac nhan xuat |
| IN_PROGRESS | false | Da xuat - Dang di chuyen | Nhan vien xac nhan nhap |
| IN_PROGRESS | true | Da nhap - Hoan thanh | Khong con hanh dong |
| COMPLETED | true | Hoan thanh | Khong con hanh dong |
| CANCELLED | false | Da huy | Khong con hanh dong |
