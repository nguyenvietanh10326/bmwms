using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Business.Interfaces;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Repository.Interfaces.StockOperations;

namespace BMWMS.Business.Services.StockOperations
{
    public class TransferLookupService : ITransferLookupService
    {
        private readonly ITransferRepository _repository;
        private readonly ICapacityEvaluationService _capacityService;

        public TransferLookupService(ITransferRepository repository, ICapacityEvaluationService capacityService)
        {
            _repository = repository;
            _capacityService = capacityService;
        }

        public async Task<List<ZoneOptionDto>> GetZonesAsync(long warehouseId = 1, long? productId = null)
        {
            var zones = await _repository.GetZonesByWarehouseAsync(warehouseId, productId);
            return zones.Select(z => new ZoneOptionDto { ZoneId = z.ZoneId, ZoneCode = z.ZoneCode, ZoneName = z.ZoneName }).ToList();
        }

        public async Task<List<RackOptionDto>> GetRacksAsync(long warehouseId, long? zoneId = null, long? productId = null)
        {
            var racks = await _repository.GetRacksByZoneAsync(warehouseId, zoneId, productId);
            return racks.Select(r => new RackOptionDto { RackId = r.RackId, RackCode = r.RackCode, RackName = r.RackName, ZoneId = r.ZoneId }).ToList();
        }

        public async Task<List<LocationOptionDto>> GetLocationsAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null, long? productId = null)
        {
            var locs = await _repository.GetActiveLocationsByWarehouseAsync(warehouseId, zoneId, rackId, productId);
            return locs.Select(l => new LocationOptionDto
            {
                LocationId = l.StorageLocationId,
                LocationCode = l.LocationCode,
                LocationName = l.LocationName ?? string.Empty,
                ZoneId = l.StorageRack?.ZoneId,
                ZoneCode = l.StorageRack?.WarehouseZone?.ZoneCode ?? string.Empty,
                RackId = l.RackId,
                RackCode = l.StorageRack?.RackCode ?? string.Empty,
                IsPutawayAllowed = l.IsPutawayAllowed,
                IsPickable = l.IsPickable,
                Status = l.Status
            }).ToList();
        }

        public async Task<List<TransferInventoryItemDto>> GetLocationInventoryAsync(long locationId)
        {
            var invs = await _repository.GetInventoriesByLocationAsync(locationId);
            return invs.Select(i => new TransferInventoryItemDto
            {
                InventoryId = i.InventoryId,
                ProductId = i.ProductId,
                ProductCode = i.Product?.ProductCode ?? string.Empty,
                ProductName = i.Product?.ProductName ?? string.Empty,
                UnitName = i.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = i.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                ProductLotId = i.ProductLotId,
                LotNumber = i.ProductLot?.LotNumber ?? string.Empty,
                ExpiryDate = i.ProductLot?.ExpiryDate,
                OnHandQuantity = i.OnHandQuantity,
                ReservedQuantity = i.ReservedQuantity,
                AvailableQuantity = i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)
            }).ToList();
        }

        public async Task<BinCapacityCheckDto> ValidateDestinationAsync(long locationId, long productId, decimal requestedQuantity)
        {
            var allocation = new CapacityAllocationDto
            {
                StorageLocationId = locationId,
                ProductId = productId,
                Quantity = requestedQuantity
            };
            
            var evaluations = await _capacityService.EvaluateAsync(new List<CapacityAllocationDto> { allocation }, acquireLocationLocks: false);
            var result = evaluations.Values.FirstOrDefault();
            
            if (result == null)
                return new BinCapacityCheckDto { IsValid = true, CapacityStatus = "UNKNOWN", Message = "Không xác định được sức chứa." };

            return new BinCapacityCheckDto
            {
                IsValid = result.OverallStatus != CapacityEvaluationStatuses.Exceeded,
                CapacityStatus = result.OverallStatus.ToString(),
                Message = result.OverallStatus == CapacityEvaluationStatuses.Exceeded ? "Vượt quá sức chứa." : "Đủ sức chứa.",
                CurrentQuantity = result.CurrentQuantity,
                ProjectedQuantity = result.ProjectedQuantity,
                MaxCapacityQuantity = result.MaxCapacityQuantity,
                CurrentWeightKg = result.CurrentWeightKg,
                ProjectedWeightKg = result.ProjectedWeightKg,
                MaxWeightKg = result.MaxWeightKg,
                CurrentVolumeM3 = result.CurrentVolumeM3,
                ProjectedVolumeM3 = result.ProjectedVolumeM3,
                MaxVolumeM3 = result.MaxVolumeM3
            };
        }

        public async Task<List<StaffOptionDto>> GetStaffUsersAsync()
        {
            var staff = await _repository.GetStaffUsersAsync();
            return staff.Select(s => new StaffOptionDto
            {
                UserId = s.UserId,
                Username = s.Username,
                FullName = s.FullName,
                RoleCode = s.Role.RoleCode,
                RoleName = s.Role.RoleName
            }).ToList();
        }

        public Task<List<TransferUseCaseDto>> GetUseCasesAsync()
        {
            // Just returning some static data for UI
            return Task.FromResult(new List<TransferUseCaseDto>());
        }
    }
}

