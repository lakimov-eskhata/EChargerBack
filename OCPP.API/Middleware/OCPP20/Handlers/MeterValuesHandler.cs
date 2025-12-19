using OCPP.API.Middleware.Common;
using OCPP.Core.Server.Messages_OCPP20;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCPP.API.Middleware.OCPP16;

namespace OCPP.API.Middleware.OCPP20.Handlers;

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
        _logger.LogTrace("[OCPP2.0] MeterValues handler processing...");

        var jsonElem = ExtractParams(message);
        if (jsonElem.ValueKind == System.Text.Json.JsonValueKind.Undefined)
            throw new InvalidOperationException("Invalid message format for MeterValues");

        string payload = jsonElem.GetRawText();
        var meterValueRequest = JsonConvert.DeserializeObject<MeterValuesRequest>(payload);

        var resp = new MeterValuesResponse
        {
            CustomData = new CustomDataType { VendorId = "DefaultVendor" }
        };

        int connectorId = meterValueRequest.EvseId;
        string msgMeterValue = string.Empty;

        try
        {
            double currentChargeKW = -1;
            double meterKWH = -1;
            DateTimeOffset? meterTime = null;
            double stateOfCharge = -1;

            GetMeterValues(meterValueRequest.MeterValue, out meterKWH, out currentChargeKW, out stateOfCharge, out meterTime);

            if (!meterTime.HasValue) meterTime = DateTime.UtcNow;

            if (connectorId > 0)
            {
                msgMeterValue = $"Meter (kWh): {meterKWH}";
                if (meterKWH >= 0)
                {
                    await UpdateConnectorStatus(connectorId, null, null, meterKWH, meterTime);
                    // skip in-memory update
                }
            }

            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MeterValues(2.0) => Exception: {0}", ex.Message);
            throw;
        }
    }

    private static System.Text.Json.JsonElement ExtractParams(object message)
    {
        if (message is System.Text.Json.JsonElement je)
        {
            // if it's an array (old 1.6 style), params at index 2 or 3
            if (je.ValueKind == System.Text.Json.JsonValueKind.Array && je.GetArrayLength() > 2)
                return je[2];
            return je;
        }

        // fallback: try reflection to get 'Params' property (JSON-RPC parsed object)
        var t = message?.GetType();
        if (t != null)
        {
            var prop = t.GetProperty("Params");
            if (prop != null)
            {
                var val = prop.GetValue(message);
                if (val is System.Text.Json.JsonElement el) return el;
            }
        }

        return new System.Text.Json.JsonElement();
    }

    private void GetMeterValues(ICollection<MeterValueType> meterValues, out double meterKWH, out double currentChargeKW, out double stateOfCharge, out DateTimeOffset? meterTime)
    {
        currentChargeKW = -1;
        meterKWH = -1;
        meterTime = null;
        stateOfCharge = -1;

        foreach (MeterValueType meterValue in meterValues)
        {
            foreach (SampledValueType sampleValue in meterValue.SampledValue)
            {
                _logger.LogTrace("GetMeterValues => Context={0} / Value={1} / Unit={2} / Measurand={3}", sampleValue.Context, sampleValue.Value, sampleValue.UnitOfMeasure?.Unit, sampleValue.Measurand);

                if (sampleValue.Measurand == MeasurandEnumType.Power_Active_Import)
                {
                    currentChargeKW = sampleValue.Value;
                    if (sampleValue.UnitOfMeasure?.Unit == "W" || sampleValue.UnitOfMeasure?.Unit == "VA" || sampleValue.UnitOfMeasure?.Unit == "var" || sampleValue.UnitOfMeasure == null)
                    {
                        _logger.LogTrace("GetMeterValues => Charging '{0:0.0}' W", currentChargeKW);
                        currentChargeKW = currentChargeKW / 1000.0;
                    }
                }
                else if (sampleValue.Measurand == MeasurandEnumType.Energy_Active_Import_Register || sampleValue.Measurand == MeasurandEnumType.Missing || sampleValue.Measurand == null)
                {
                    meterKWH = sampleValue.Value;
                    if (sampleValue.UnitOfMeasure?.Unit == "Wh" || sampleValue.UnitOfMeasure?.Unit == "VAh" || sampleValue.UnitOfMeasure?.Unit == "varh" || sampleValue.UnitOfMeasure == null)
                    {
                        _logger.LogTrace("GetMeterValues => Value: '{0:0.0}' Wh", meterKWH);
                        meterKWH = meterKWH / 1000.0;
                    }
                    meterTime = meterValue.Timestamp;
                }
                else if (sampleValue.Measurand == MeasurandEnumType.SoC)
                {
                    stateOfCharge = sampleValue.Value;
                }
            }
        }
    }

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
