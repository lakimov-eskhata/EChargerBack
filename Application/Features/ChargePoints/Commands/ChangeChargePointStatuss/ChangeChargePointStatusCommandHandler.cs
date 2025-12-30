using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Features.ChargePoints.Commands.ChangeChargePointStatuss;

public class ChangeChargePointStatusCommandHandler : IRequestHandler<ChangeChargePointStatusCommand, ChangeChargePointStatusResponse>
    {
        private readonly IChargePointRepository _chargePointRepository;
        private readonly ILogger<ChangeChargePointStatusCommandHandler> _logger;
        
        public ChangeChargePointStatusCommandHandler(
            IChargePointRepository chargePointRepository,
            ILogger<ChangeChargePointStatusCommandHandler> logger)
        {
            _chargePointRepository = chargePointRepository;
            _logger = logger;
        }
        
        public async Task<ChangeChargePointStatusResponse> Handle(ChangeChargePointStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Changing status for charge point: {ChargePointId} to {IsActive}", 
                request.ChargePointId, request.IsActive);
            
            var chargePoint = await _chargePointRepository.GetByIdAsync(request.ChargePointId);
            if (chargePoint == null)
            {
                throw new KeyNotFoundException($"Charge point with ID {request.ChargePointId} not found");
            }
            
            var newStatus = request.IsActive ? "Active" : "Inactive";
            
            // Обновляем статус
            var success = await _chargePointRepository.UpdateStatusAsync(
                request.ChargePointId, 
                request.IsActive ? "Active" : "Inactive");
            
            if (success)
            {
                _logger.LogInformation("Successfully changed status for charge point: {ChargePointId} to {Status}", 
                    request.ChargePointId, newStatus);
                
                return new ChangeChargePointStatusResponse
                {
                    ChargePointId = request.ChargePointId,
                    IsActive = request.IsActive,
                    Status = newStatus,
                    ChangedAt = DateTime.UtcNow
                };
            }
            else
            {
                throw new InvalidOperationException($"Failed to change status for charge point {request.ChargePointId}");
            }
        }
    }