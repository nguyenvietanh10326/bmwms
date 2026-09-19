using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Repository.Interfaces.StockOperations;

namespace BMWMS.Business.Services.StockOperations
{
    public class TransferCancellationService : ITransferCancellationService
    {
        private readonly ITransferRepository _repository;
        private readonly IAuditLogService _auditLogService;

        public TransferCancellationService(ITransferRepository repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<TransferResultDto> CancelTransferAsync(
            long actorId,
            long transferOrderId,
            string? notes,
            bool isManager)
        {
            var order = await _repository.CancelOrderAsync(transferOrderId, actorId, notes, isManager);
            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = actorId,
                ActionType = "CANCEL_TRANSFER",
                EntityName = "TransferOrder",
                EntityId = order.TransferOrderId.ToString()
            });
            return new TransferResultDto { Success = true, Message = "Hủy phiếu thành công." };
        }
    }
}
