# Purchase Order Creation Tasks

- `[x]` 1. **Update API & Business Layer (Supplier part)**
  - `[x]` Add `ProductId` to `SupplierProductDto` in `BMWMS.Business/DTOs/Supplier/SupplierDetailResponseDto.cs`.
  - `[x]` Map `ProductId` in `SupplierService.GetSupplierDetailAsync`.
- `[x]` 2. **Update API & Business Layer (Purchase Order part)**
  - `[x]` Create `PurchaseOrderCreateDto.cs` and `PurchaseOrderDetailCreateDto.cs`.
  - `[x]` Add `CreatePurchaseOrderAsync` to `IPurchaseOrderService.cs`.
  - `[x]` Implement `CreatePurchaseOrderAsync` in `PurchaseOrderService.cs` (Auto-generate PO code, set status DRAFT).
  - `[x]` Add `Create` POST endpoint in `PurchaseOrdersController.cs`.
- `[x]` 3. **Update Web Layer (UI & Logic)**
  - `[x]` Add `ProductId` to `SupplierProductModel` in `BMWMS.Web/Models/SupplierModels.cs`.
  - `[x]` Create `PurchaseOrderCreateRequestModel.cs`.
  - `[x]` Create `BMWMS.Web/Pages/PurchaseOrders/Create.cshtml.cs` (Handle form submit, RBAC).
  - `[x]` Create `BMWMS.Web/Pages/PurchaseOrders/Create.cshtml` (Supplier-first UI, JS fetch products, Dynamic Table).
- `[x]` 4. **Verification**
  - `[x]` Run `dotnet build` to ensure no compile errors.
  - `[x]` Ensure code aligns with the plan (no pricing info, starts as DRAFT).
