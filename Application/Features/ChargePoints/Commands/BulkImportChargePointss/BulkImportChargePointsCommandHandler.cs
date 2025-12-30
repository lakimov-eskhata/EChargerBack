using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.Station;
using Microsoft.Extensions.Logging;

namespace Application.Features.ChargePoints.Commands.BulkImportChargePointss;

public class BulkImportChargePointsCommandHandler : IRequestHandler<BulkImportChargePointsCommand, BulkImportChargePointsResponse>
    {
        private readonly IChargePointRepository _chargePointRepository;
        private readonly ILogger<BulkImportChargePointsCommandHandler> _logger;
        
        public BulkImportChargePointsCommandHandler(
            IChargePointRepository chargePointRepository,
            ILogger<BulkImportChargePointsCommandHandler> logger)
        {
            _chargePointRepository = chargePointRepository;
            _logger = logger;
        }
        
        public async Task<BulkImportChargePointsResponse> Handle(BulkImportChargePointsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bulk importing {Count} charge points", request.ChargePoints.Count);
            
            var results = new List<BulkImportChargePointsResponse.ImportResult>();
            var successfullyImported = 0;
            
            foreach (var chargePointCommand in request.ChargePoints)
            {
                try
                {
                    // Проверяем существование станции
                    var existingChargePoint = await _chargePointRepository.GetByIdAsync(chargePointCommand.ChargePointId);
                    
                    if (existingChargePoint != null && !request.OverrideExisting)
                    {
                        results.Add(new BulkImportChargePointsResponse.ImportResult
                        {
                            ChargePointId = chargePointCommand.ChargePointId,
                            Success = false,
                            ErrorMessage = "Charge point already exists"
                        });
                        continue;
                    }
                    
                    // Создаем или обновляем станцию
                    var chargePoint = new ChargePointEntity()
                    {
                        ChargePointId = chargePointCommand.ChargePointId,
                        Name = chargePointCommand.Name,
                        ProtocolVersion = chargePointCommand.ProtocolVersion,
                        Vendor = chargePointCommand.Vendor,
                        Model = chargePointCommand.Model,
                        SerialNumber = chargePointCommand.SerialNumber,
                        FirmwareVersion = chargePointCommand.FirmwareVersion,
                        ConnectorCount = chargePointCommand.ConnectorCount,
                        HeartbeatInterval = chargePointCommand.HeartbeatInterval,
                        Iccid = chargePointCommand.Iccid,
                        Imsi = chargePointCommand.Imsi,
                        MeterType = chargePointCommand.MeterType,
                        MeterSerialNumber = chargePointCommand.MeterSerialNumber,
                        Status = "Offline",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    if (existingChargePoint != null && request.OverrideExisting)
                    {
                        chargePoint.Id = existingChargePoint.Id;
                        await _chargePointRepository.UpdateAsync(chargePoint);
                    }
                    else
                    {
                        await _chargePointRepository.CreateAsync(chargePoint);
                    }
                    
                    results.Add(new BulkImportChargePointsResponse.ImportResult
                    {
                        ChargePointId = chargePointCommand.ChargePointId,
                        Success = true
                    });
                    
                    successfullyImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing charge point: {ChargePointId}", chargePointCommand.ChargePointId);
                    
                    results.Add(new BulkImportChargePointsResponse.ImportResult
                    {
                        ChargePointId = chargePointCommand.ChargePointId,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
            
            _logger.LogInformation("Bulk import completed. Successfully imported: {Success}/{Total}", 
                successfullyImported, request.ChargePoints.Count);
            
            return new BulkImportChargePointsResponse
            {
                TotalProcessed = request.ChargePoints.Count,
                SuccessfullyImported = successfullyImported,
                Failed = request.ChargePoints.Count - successfullyImported,
                Results = results
            };
        }
    }