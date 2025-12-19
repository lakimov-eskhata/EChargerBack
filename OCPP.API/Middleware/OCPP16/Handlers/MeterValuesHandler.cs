using OCPP.API.Middleware.Common;
using System.Globalization;
using Application.Ocpp16.Messages_OCPP16;
using Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Infrastructure;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class MeterValuesHandler : IMessageHandler
{
    private readonly ILogger<MeterValuesHandler> _logger;
    private readonly OCPPCoreContext _dbContext;

    public MeterValuesHandler(ILogger<MeterValuesHandler> logger, OCPPCoreContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogTrace("Processing meter values (handler)...");

        try
        {
            // message is JsonElement array -> payload at index 3
            var jsonElem = (System.Text.Json.JsonElement)message;
            if (jsonElem.ValueKind != System.Text.Json.JsonValueKind.Array || jsonElem.GetArrayLength() < 4)
                throw new InvalidOperationException("Invalid MeterValues message format");

            var payload = jsonElem[3].GetRawText();
            var meterValueRequest = JsonConvert.DeserializeObject<MeterValuesRequest>(payload);

            int connectorId = meterValueRequest.ConnectorId;

            double currentChargeKW = -1;
            double meterKWH = -1;
            DateTimeOffset meterTime = DateTime.UtcNow;
            double stateOfCharge = -1;

            foreach (var mv in meterValueRequest.MeterValue)
            {
                foreach (var sv in mv.SampledValue)
                {
                    _logger.LogTrace("MeterValues => Value={0} / Unit={1} / Measurand={2}", sv.Value, sv.Unit, sv.Measurand);

                    if (sv.Measurand == SampledValueMeasurand.Power_Active_Import)
                    {
                        if (double.TryParse(sv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out currentChargeKW))
                        {
                            if (sv.Unit == SampledValueUnit.W || sv.Unit == SampledValueUnit.VA || sv.Unit == SampledValueUnit.Var || sv.Unit == null)
                            {
                                currentChargeKW = currentChargeKW / 1000.0; // W -> kW
                            }
                        }
                    }
                    else if (sv.Measurand == SampledValueMeasurand.Energy_Active_Import_Register || sv.Measurand == null)
                    {
                        if (double.TryParse(sv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out meterKWH))
                        {
                            if (sv.Unit == SampledValueUnit.Wh || sv.Unit == SampledValueUnit.Varh || sv.Unit == null)
                            {
                                meterKWH = meterKWH / 1000.0; // Wh -> kWh
                            }
                            meterTime = mv.Timestamp;
                        }
                    }
                    else if (sv.Measurand == SampledValueMeasurand.SoC)
                    {
                        if (double.TryParse(sv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out stateOfCharge))
                        {
                            // ok
                        }
                    }
                }
            }

            if (connectorId > 0 && meterKWH >= 0)
            {
                await UpdateConnectorStatus(connectorId, null, null, meterKWH, meterTime);
                // in-memory status update skipped (middleware holds that), so skip UpdateMemoryConnectorStatus
            }

            // return empty response payload
            return new Application.Ocpp16.Messages_OCPP16.MeterValuesResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MeterValues handler => Exception: {0}", ex.Message);
            throw;
        }
    }

    // minimal UpdateConnectorStatus adapted from OccpControllerBase
    private async Task<bool> UpdateConnectorStatus(int connectorId, string status, DateTimeOffset? statusTime, double? meter, DateTimeOffset? meterTime)
    {
        try
        {
            var connectorStatus = await _dbContext.ConnectorStatuses.FirstOrDefaultAsync(x => x.ConnectorId == connectorId);
            if (connectorStatus == null)
            {
                connectorStatus = new Domain.Entities.Station.ConnectorStatusEntity
                {
                    ChargePointId = "",
                    ConnectorId = connectorId
                };
                await _dbContext.ConnectorStatuses.AddAsync(connectorStatus);
            }

            if (!string.IsNullOrEmpty(status))
            {
                var dbTime = (statusTime ?? DateTimeOffset.UtcNow).DateTime;
                connectorStatus.LastStatus = status;
                connectorStatus.LastStatusTime = dbTime;
            }

            if (meter.HasValue)
            {
                var dbTime = (meterTime ?? DateTimeOffset.UtcNow).DateTime;
                connectorStatus.LastMeter = meter.Value;
                connectorStatus.LastMeterTime = dbTime;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception exp)
        {
            _logger.LogError(exp, "UpdateConnectorStatus => Exception: {0}", exp.Message);
            return false;
        }
    }
}
