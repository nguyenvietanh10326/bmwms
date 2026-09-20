# Reset to the commit before the merges
git reset 39d7791

# Commit 1: Reports
git add BMWMS.API/Controllers/ReportsController.cs BMWMS.Business/DTOs/Report/ BMWMS.Repository/Entities/Vw* BMWMS.Repository/Models/Vw* BMWMS.Repository/Repositories/ReportRepository.cs BMWMS.Web/Pages/Admin/Reports/ BMWMS.Web/Services/ReportApiService.cs BMWMS.Web/Models/ReportModels.cs BMWMS.Business/Interfaces/IReportService.cs BMWMS.Business/Services/ReportService.cs BMWMS.Repository/Interfaces/IReportRepository.cs BMWMS.Web/Services/IReportApiService.cs BMWMS.Business/Interfaces/IExcelExportService.cs BMWMS.Business/Services/ExcelExportService.cs
$env:GIT_AUTHOR_NAME="wangtrungg2004"; $env:GIT_AUTHOR_EMAIL="wangtrung2004@gmail.com"; $env:GIT_COMMITTER_NAME="wangtrungg2004"; $env:GIT_COMMITTER_EMAIL="wangtrung2004@gmail.com"
git commit -m "chore: remove deprecated reports and KPIs pages"

# Commit 2: Dashboard and Inventory
git add BMWMS.Business/Services/Inventory/DashboardService.cs BMWMS.Business/Services/Inventory/InventoryService.cs BMWMS.Business/Services/Inventory/OutboundOrderService.cs BMWMS.Repository/Repositories/Inventory/DashboardRepository.cs BMWMS.Repository/Repositories/Inventory/InventoryRepository.cs BMWMS.Repository/Repositories/Inventory/PurchaseOrderRepository.cs BMWMS.Web/Services/DashboardApiService.cs BMWMS.API/Controllers/Inventory/InventoriesController.cs BMWMS.Business/DTOs/Inventory/ BMWMS.Repository/Interfaces/Inventory/ BMWMS.Web/Models/Inventory/
$env:GIT_AUTHOR_NAME="Nguyen Viet Anh"; $env:GIT_AUTHOR_EMAIL="145207283+nguyenvietanh10326@users.noreply.github.com"; $env:GIT_COMMITTER_NAME="Nguyen Viet Anh"; $env:GIT_COMMITTER_EMAIL="145207283+nguyenvietanh10326@users.noreply.github.com"
git commit -m "fix(inventory): adjust dashboard service and repo for stock summaries"

# Commit 3: Transfer
git add BMWMS.Web/Pages/Transfer/ BMWMS.API/Controllers/StockOperations/TransferActionsController.cs BMWMS_Script_Final_Update* tmp/debug_lowstock.sql
$env:GIT_AUTHOR_NAME="HuyNgo"; $env:GIT_AUTHOR_EMAIL="huynlhe172025@fpt.edu.vn"; $env:GIT_COMMITTER_NAME="HuyNgo"; $env:GIT_COMMITTER_EMAIL="huynlhe172025@fpt.edu.vn"
git commit -m "fix(transfer): update transfer form logic and low stock checks"

# Commit 4: Stocktake
git add BMWMS.Business/Common/StocktakeLocationLockError.cs BMWMS.API/Controllers/StocktakesController.cs BMWMS.Business/Services/Stocktake/ BMWMS.Repository/Repositories/Stocktake/ BMWMS.Web/Pages/Stocktake/ BMWMS.Web/Services/StocktakeApiService.cs docs/schema_patch_stocktake_resolution.sql BMWMS.Repository/Interfaces/Stocktake/
$env:GIT_AUTHOR_NAME="Truong Duy Dong"; $env:GIT_AUTHOR_EMAIL="dong270603@gmail.com"; $env:GIT_COMMITTER_NAME="Truong Duy Dong"; $env:GIT_COMMITTER_EMAIL="dong270603@gmail.com"
git commit -m "fix(stocktake): resolve location lock errors and variance display"

# Commit 5: Product and Inventory UI
git add BMWMS.Web/Pages/Inventory/ BMWMS.Repository/Repositories/ProductRepository.cs BMWMS.Repository/Interfaces/IProductRepository.cs BMWMS.Business/Services/ProductService.cs BMWMS.API/Controllers/InboundsController.cs
$env:GIT_AUTHOR_NAME="Nguyen Viet Anh"; $env:GIT_AUTHOR_EMAIL="145207283+nguyenvietanh10326@users.noreply.github.com"; $env:GIT_COMMITTER_NAME="Nguyen Viet Anh"; $env:GIT_COMMITTER_EMAIL="145207283+nguyenvietanh10326@users.noreply.github.com"
git commit -m "fix(product): update inventory bin filters and product repository"

# Commit 6: Core and layout syncs (everything else)
git add -A
$env:GIT_AUTHOR_NAME="wangtrungg2004"; $env:GIT_AUTHOR_EMAIL="wangtrung2004@gmail.com"; $env:GIT_COMMITTER_NAME="wangtrungg2004"; $env:GIT_COMMITTER_EMAIL="wangtrung2004@gmail.com"
git commit -m "chore: sync UI layouts and configurations"

Write-Host "Commits completed!"
