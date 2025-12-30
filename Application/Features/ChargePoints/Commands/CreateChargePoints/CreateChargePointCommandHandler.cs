using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.Station;
using Microsoft.Extensions.Logging;

namespace Application.Features.ChargePoints.Commands.CreateChargePoints;

 public class CreateChargePointCommandHandler : IRequestHandler<CreateChargePointCommand, CreateChargePointResponse>
    {
        private readonly IChargePointRepository _chargePointRepository;
        private readonly ILogger<CreateChargePointCommandHandler> _logger;
        
        public CreateChargePointCommandHandler(
            IChargePointRepository chargePointRepository,
            ILogger<CreateChargePointCommandHandler> logger)
        {
            _chargePointRepository = chargePointRepository;
            _logger = logger;
        }
        
        public async Task<CreateChargePointResponse> Handle(CreateChargePointCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating charge point: {ChargePointId}", request.ChargePointId);
            
            // Проверяем, существует ли уже станция с таким ID
            var existingChargePoint = await _chargePointRepository.GetByIdAsync(request.ChargePointId);
            if (existingChargePoint != null)
            {
                throw new InvalidOperationException($"Charge point with ID {request.ChargePointId} already exists");
            }
            
            // Создаем новую зарядную станцию
            var chargePoint = new ChargePointEntity()
            {
                ChargePointId = request.ChargePointId,
                Name = request.Name,
                ProtocolVersion = request.ProtocolVersion,
                Vendor = request.Vendor,
                Model = request.Model,
                SerialNumber = request.SerialNumber,
                FirmwareVersion = request.FirmwareVersion,
                ConnectorCount = request.ConnectorCount,
                HeartbeatInterval = request.HeartbeatInterval,
                Iccid = request.Iccid,
                Imsi = request.Imsi,
                MeterType = request.MeterType,
                MeterSerialNumber = request.MeterSerialNumber,
                Status = "Offline",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Если указано количество коннекторов, создаем их
            if (request.ConnectorCount.HasValue && request.ConnectorCount.Value > 0)
            {
                chargePoint.Connectors = new List<ConnectorEntity>();
                for (int i = 1; i <= request.ConnectorCount.Value; i++)
                {
                    chargePoint.Connectors.Add(new ConnectorEntity()
                    {
                        ConnectorId = i,
                        Status = "Available",
                        StatusTimestamp = DateTime.UtcNow
                    });
                }
            }
            
            var createdChargePoint = await _chargePointRepository.CreateAsync(chargePoint);
            
            _logger.LogInformation("Successfully created charge point: {ChargePointId}", request.ChargePointId);
            
            return new CreateChargePointResponse
            {
                Id = createdChargePoint.Id,
                ChargePointId = createdChargePoint.ChargePointId,
                Name = createdChargePoint.Name,
                ProtocolVersion = createdChargePoint.ProtocolVersion,
                CreatedAt = createdChargePoint.CreatedAt
            };
        }
    }