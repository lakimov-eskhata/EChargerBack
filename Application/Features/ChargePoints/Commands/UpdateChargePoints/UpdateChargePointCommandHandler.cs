using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Features.ChargePoints.Commands.UpdateChargePoints;

public class UpdateChargePointCommandHandler : IRequestHandler<UpdateChargePointCommand, UpdateChargePointResponse>
    {
        private readonly IChargePointRepository _chargePointRepository;
        private readonly ILogger<UpdateChargePointCommandHandler> _logger;
        
        public UpdateChargePointCommandHandler(
            IChargePointRepository chargePointRepository,
            ILogger<UpdateChargePointCommandHandler> logger)
        {
            _chargePointRepository = chargePointRepository;
            _logger = logger;
        }
        
        public async Task<UpdateChargePointResponse> Handle(UpdateChargePointCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating charge point: {ChargePointId}", request.ChargePointId);
            
            var chargePoint = await _chargePointRepository.GetByIdAsync(request.ChargePointId);
            if (chargePoint == null)
            {
                throw new KeyNotFoundException($"Charge point with ID {request.ChargePointId} not found");
            }
            
            // Обновляем только указанные поля
            if (!string.IsNullOrEmpty(request.Name))
                chargePoint.Name = request.Name;
            
            if (!string.IsNullOrEmpty(request.ProtocolVersion))
                chargePoint.ProtocolVersion = request.ProtocolVersion;
            
            if (request.Vendor != null)
                chargePoint.Vendor = request.Vendor;
            
            if (request.Model != null)
                chargePoint.Model = request.Model;
            
            if (request.SerialNumber != null)
                chargePoint.SerialNumber = request.SerialNumber;
            
            if (request.FirmwareVersion != null)
                chargePoint.FirmwareVersion = request.FirmwareVersion;
            
            if (request.ConnectorCount.HasValue)
                chargePoint.ConnectorCount = request.ConnectorCount;
            
            if (request.HeartbeatInterval.HasValue)
                chargePoint.HeartbeatInterval = request.HeartbeatInterval;
            
            if (request.Iccid != null)
                chargePoint.Iccid = request.Iccid;
            
            if (request.Imsi != null)
                chargePoint.Imsi = request.Imsi;
            
            if (request.MeterType != null)
                chargePoint.MeterType = request.MeterType;
            
            if (request.MeterSerialNumber != null)
                chargePoint.MeterSerialNumber = request.MeterSerialNumber;
            
            chargePoint.UpdatedAt = DateTime.UtcNow;
            
            await _chargePointRepository.UpdateAsync(chargePoint);
            
            _logger.LogInformation("Successfully updated charge point: {ChargePointId}", request.ChargePointId);
            
            return new UpdateChargePointResponse
            {
                ChargePointId = chargePoint.ChargePointId,
                Name = chargePoint.Name,
                UpdatedAt = chargePoint.UpdatedAt
            };
        }
    }