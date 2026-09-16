using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Business.Interfaces;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Repository.Interfaces.StockOperations;

namespace BMWMS.Business.Services.StockOperations
{
    public class TransferApprovalService : ITransferApprovalService
    {
        private readonly ITransferRepository _repository;
        private readonly IAuditLogService _auditLogService;

        public TransferApprovalService(ITransferRepository repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<TransferResultDto> ApproveTransferAsync(long managerId, ApproveTransferDto dto)
        {
            // Note: DB logic for RESERVE inventory transaction is inside the repository method
            var order = await _repository.ApproveOrderAsync(dto.TransferOrderId, managerId, dto.Notes);
            await _auditLogService.RecordAsync(new AuditEventDto { UserId = managerId, ActionType = "APPROVE_TRANSFER", EntityName = "TransferOrder", EntityId = order.TransferOrderId.ToString() });
            return new TransferResultDto { Success = true, Message = "Duyệt phiếu thành công." };
        }

        public async Task<TransferResultDto> CancelTransferAsync(long actorId, long transferOrderId, string? notes, bool isManager)
        {
            var order = await _repository.CancelOrderAsync(transferOrderId, actorId, notes, isManager);
            await _auditLogService.RecordAsync(new AuditEventDto { UserId = actorId, ActionType = "CANCEL_TRANSFER", EntityName = "TransferOrder", EntityId = order.TransferOrderId.ToString() });
            return new TransferResultDto { Success = true, Message = "Hủy phiếu thành công." };
        }
    }
}

