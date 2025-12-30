using System.Transactions;
using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Features.ChargePoints.Commands.DeleteChargePoints;

public class DeleteChargePointCommandHandler : IRequestHandler<DeleteChargePointCommand, DeleteChargePointResponse>
    {
        private readonly IChargePointRepository _chargePointRepository;
        private readonly ILogger<DeleteChargePointCommandHandler> _logger;
        
        public DeleteChargePointCommandHandler(
            IChargePointRepository chargePointRepository,
            ILogger<DeleteChargePointCommandHandler> logger)
        {
            _chargePointRepository = chargePointRepository;
            _logger = logger;
        }
        
        public async Task<DeleteChargePointResponse> Handle(DeleteChargePointCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting charge point: {ChargePointId}", request.ChargePointId);
            
            var chargePoint = await _chargePointRepository.GetByIdAsync(request.ChargePointId);
            if (chargePoint == null)
            {
                throw new KeyNotFoundException($"Charge point with ID {request.ChargePointId} not found");
            }
            
            // Проверяем, есть ли активные транзакции
            var hasActiveTransactions = chargePoint.Transactions?.Any(t => 
                t.Status == TransactionStatusEnum.Started.Value || t.Status == TransactionStatusEnum.InProgress.Value) ?? false;
            
            if (hasActiveTransactions && !request.ForceDelete)
            {
                throw new InvalidOperationException(
                    $"Cannot delete charge point {request.ChargePointId} because it has active transactions. Use force delete if necessary.");
            }
            
            var success = await _chargePointRepository.DeleteAsync(request.ChargePointId);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted charge point: {ChargePointId}", request.ChargePointId);
                
                return new DeleteChargePointResponse
                {
                    Success = true,
                    Message = $"Charge point {request.ChargePointId} deleted successfully",
                    DeletedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new DeleteChargePointResponse
                {
                    Success = false,
                    Message = $"Failed to delete charge point {request.ChargePointId}",
                    DeletedAt = DateTime.UtcNow
                };
            }
        }
    }