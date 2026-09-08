$files = @{
    "D:\bmwms\BMWMS.Web\Pages\Stocktake\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Stocktake\Count.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Stocktake\Details.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Stocktake\Create.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER"
    
    "D:\bmwms\BMWMS.Web\Pages\Transfer\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Transfer\Details.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Transfer\BinTransfer.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Transfer\Create.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER"
    
    "D:\bmwms\BMWMS.Web\Pages\OutboundOrders\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\OutboundOrders\Details.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\OutboundOrders\Create.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF"
    
    "D:\bmwms\BMWMS.Web\Pages\SalesOrders\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\SalesOrders\Detail.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF"
    
    "D:\bmwms\BMWMS.Web\Pages\PurchaseOrders\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\PurchaseOrders\Detail.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF"
    
    "D:\bmwms\BMWMS.Web\Pages\Inventory\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Inventory\Inventory.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF"
    
    "D:\bmwms\BMWMS.Web\Pages\Products\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Products\Details.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF"
    "D:\bmwms\BMWMS.Web\Pages\Products\Create.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER"
    "D:\bmwms\BMWMS.Web\Pages\Products\Edit.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER"
    
    "D:\bmwms\BMWMS.Web\Pages\Categories\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF"
    
    "D:\bmwms\BMWMS.Web\Pages\StorageLocations\Index.cshtml.cs" = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF"
}

foreach ($key in $files.Keys) {
    if (Test-Path $key) {
        $content = Get-Content $key -Raw
        $roles = $files[$key]
        
        if ($content -notmatch "\[Authorize\(") {
            if ($content -notmatch "using Microsoft.AspNetCore.Authorization;") {
                $content = "using Microsoft.AspNetCore.Authorization;`r`n" + $content
            }
            
            $content = $content -replace "public class (\w+Model\s*:\s*PageModel)", "[Authorize(Roles = `"$roles`")]`r`n    public class `$1"
            
            Set-Content -Path $key -Value $content -Encoding UTF8
            Write-Output "Updated: $key"
        } else {
            Write-Output "Skipped (already has Authorize): $key"
        }
    } else {
        Write-Output "File not found: $key"
    }
}
