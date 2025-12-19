using Application.Common.Interfaces;
using Application.Common.Models;
using OCPP.API.Middleware.Common;
using OCPP.Core.Server.Messages_OCPP20;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCPP.API.Middleware.OCPP16;

namespace OCPP.API.Middleware.OCPP20.Handlers;

public class TransactionEventHandler : IMessageHandler
{
    private readonly ILogger<TransactionEventHandler> _logger;
    private readonly OCPPCoreContext _dbContext;

    public TransactionEventHandler(ILogger<TransactionEventHandler> logger, OCPPCoreContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogTrace("TransactionEvent => processing...");

        var jsonElem = ExtractParams(message);
        if (jsonElem.ValueKind == System.Text.Json.JsonValueKind.Undefined)
            throw new InvalidOperationException("Invalid message format for TransactionEvent");

        var payload = jsonElem.GetRawText();
        var req = JsonConvert.DeserializeObject<TransactionEventRequest>(payload);

        var resp = new TransactionEventResponse
        {
            IdTokenInfo = new IdTokenInfoType(),
            CustomData = new CustomDataType { VendorId = "DefaultVendor" }
        };

        int connectorId = (req?.Evse != null) ? req.Evse.Id : 0;
        string msgLogText = string.Empty;

        try
        {
            string idTag = CleanChargeTagId(req?.IdToken?.IdToken);

            double currentChargeKW = -1;
            double meterKWH = -1;
            DateTimeOffset? meterTime = null;
            double stateOfCharge = -1;

            if (req?.MeterValue != null)
            {
                GetMeterValues(req.MeterValue, out meterKWH, out currentChargeKW, out stateOfCharge, out meterTime);
                msgLogText = $"Meter (kWh): {meterKWH}";
                if (currentChargeKW >= 0) msgLogText += $" | Charge (kW): {currentChargeKW}";
                if (stateOfCharge >= 0) msgLogText += $" | SoC (%): {stateOfCharge}";
            }

            if (!meterTime.HasValue) meterTime = DateTime.UtcNow;

            if (connectorId > 0 && meterKWH >= 0)
            {
                await UpdateConnectorStatus(connectorId, null, null, meterKWH, meterTime);
                // memory update skipped
            }

            if (req.EventType == TransactionEventEnumType.Started)
            {
                bool denyConcurrentTx = false; // could be read from config
                resp.IdTokenInfo = await InternalAuthorize(req?.IdToken?.IdToken, AuthAction.StartTransaction, denyConcurrentTx);

                if (resp.IdTokenInfo.Status == AuthorizationStatusEnumType.Accepted)
                {
                    await UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), meterTime, null, null);
                    try
                    {
                        var transaction = new Domain.Entities.Station.TransactionEntity
                        {
                            Uid = req.TransactionInfo.TransactionId,
                            ChargePointId = chargePointId,
                            ConnectorId = connectorId,
                            StartTagId = idTag,
                            StartTime = req.Timestamp.UtcDateTime,
                            MeterStart = meterKWH,
                            StartResult = req.TriggerReason.ToString()
                        };
                        await _dbContext.Transactions.AddAsync(transaction);
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "StartTransaction => Exception writing transaction: {0}", ex.Message);
                    }
                }

                msgLogText = $"StartTx => {resp.IdTokenInfo?.Status} | {msgLogText}";
            }
            else if (req.EventType == TransactionEventEnumType.Updated)
            {
                var transaction = await _dbContext.Transactions.Where(t => t.Uid == req.TransactionInfo.TransactionId).OrderByDescending(t => t.TransactionId).FirstOrDefaultAsync();
                if (transaction != null && !transaction.StopTime.HasValue)
                {
                    if (meterKWH >= 0)
                    {
                        transaction.MeterStop = meterKWH;
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogError("UpdateTransaction => Unknown or not matching transaction: uid='{0}' / chargepoint='{1}'", req.TransactionInfo?.TransactionId, chargePointId);
                }

                msgLogText = $"UpdateTx => {msgLogText}";
            }
            else if (req.EventType == TransactionEventEnumType.Ended)
            {
                var transaction = await _dbContext.Transactions.Where(t => t.Uid == req.TransactionInfo.TransactionId).OrderByDescending(t => t.TransactionId).FirstOrDefaultAsync();
                if (transaction != null && !transaction.StopTime.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(idTag))
                    {
                        resp.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                    }
                    else
                    {
                        resp.IdTokenInfo = await InternalAuthorize(req?.IdToken?.IdToken, AuthAction.StopTransaction, false);
                    }

                    if (resp.IdTokenInfo.Status != AuthorizationStatusEnumType.Accepted && transaction != null && !string.IsNullOrEmpty(transaction.StartTagId) && transaction.StartTagId.Equals(idTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.LogInformation("EndTransaction => override because startTag matches");
                        resp.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                    }

                    if (resp.IdTokenInfo.Status == AuthorizationStatusEnumType.Accepted)
                    {
                        transaction.StopTime = req.Timestamp.UtcDateTime;
                        transaction.MeterStop = meterKWH;
                        transaction.StopTagId = idTag;
                        transaction.StopReason = req.TriggerReason.ToString();
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogError("EndTransaction => Unknown or not matching transaction: uid='{0}' / chargepoint='{1}'", req.TransactionInfo?.TransactionId, chargePointId);
                }

                msgLogText = $"EndTx => {resp.IdTokenInfo?.Status} | {msgLogText}";
            }

            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TransactionEvent => Exception: {0}", ex.Message);
            throw;
        }
    }

    private static System.Text.Json.JsonElement ExtractParams(object message)
    {
        if (message is System.Text.Json.JsonElement je)
        {
            if (je.ValueKind == System.Text.Json.JsonValueKind.Array && je.GetArrayLength() > 2)
                return je[2];
            return je;
        }

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
                        currentChargeKW = currentChargeKW / 1000.0;
                    }
                }
                else if (sampleValue.Measurand == MeasurandEnumType.Energy_Active_Import_Register || sampleValue.Measurand == MeasurandEnumType.Missing || sampleValue.Measurand == null)
                {
                    meterKWH = sampleValue.Value;
                    if (sampleValue.UnitOfMeasure?.Unit == "Wh" || sampleValue.UnitOfMeasure?.Unit == "VAh" || sampleValue.UnitOfMeasure?.Unit == "varh" || sampleValue.UnitOfMeasure == null)
                    {
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

    private async Task<IdTokenInfoType> InternalAuthorize(string idTag, AuthAction authAction, bool denyConcurrentTx)
    {
        var idTokenInfo = new IdTokenInfoType();

        try
        {
            var ct = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == idTag);
            if (ct != null)
            {
                if (!string.IsNullOrWhiteSpace(ct.ParentTagId))
                {
                    idTokenInfo.GroupIdToken = new IdTokenType { IdToken = ct.ParentTagId };
                }
                if (ct.Blocked.HasValue && ct.Blocked.Value)
                    idTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                    idTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                else
                {
                    idTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                    if (denyConcurrentTx)
                    {
                        var tx = await _dbContext.Transactions.Where(t => !t.StopTime.HasValue && t.StartTagId == ct.TagId).OrderByDescending(t => t.TransactionId).FirstOrDefaultAsync();
                        if (tx != null) idTokenInfo.Status = AuthorizationStatusEnumType.ConcurrentTx;
                    }
                }
            }
            else
            {
                idTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
            }
        }
        catch (Exception exp)
        {
            _logger.LogError(exp, "InternalAuthorize => Exception: {0}", exp.Message);
            idTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
        }

        return idTokenInfo;
    }
}

