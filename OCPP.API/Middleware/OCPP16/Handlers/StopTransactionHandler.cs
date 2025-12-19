using Application.Ocpp16.Messages_OCPP16;
using Domain.Entities.Station;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class StopTransactionHandler : IMessageHandler
{
    private readonly ILogger<StopTransactionHandler> _logger;
    private readonly OCPPCoreContext _dbContext;

    public StopTransactionHandler(ILogger<StopTransactionHandler> logger, OCPPCoreContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogTrace("StopTransaction => processing");

        var jsonElem = (System.Text.Json.JsonElement)message;
        var payload = jsonElem[3].GetRawText();
        var req = JsonConvert.DeserializeObject<StopTransactionRequest>(payload);

        var response = new StopTransactionResponse();
        response.IdTagInfo = new Application.Ocpp16.Messages_OCPP16.IdTagInfo();

        try
        {
            string idTag = CleanChargeTagId(req.IdTag, _logger);

            TransactionEntity transaction = null;
            try
            {
                transaction = await _dbContext.Transactions.FirstOrDefaultAsync(x => x.TransactionId == req.TransactionId);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "StopTransaction => Exception reading transaction: {0}", exp.Message);
            }

            if (transaction != null)
            {
                if (string.IsNullOrWhiteSpace(idTag))
                {
                    response.IdTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Accepted;
                }
                else
                {
                    response.IdTagInfo = await InternalAuthorize(idTag, transaction.ConnectorId, transaction?.Uid, transaction?.StartTagId);
                }
            }
            else
            {
                _logger.LogError("StopTransaction => Unknown or not matching transaction: id={0} / chargepoint={1} / tag={2}", req.TransactionId, chargePointId, idTag);
                response.IdTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Invalid;
            }

            // override if start tag equals current idTag
            if (response.IdTagInfo.Status != Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Accepted &&
                transaction != null && !string.IsNullOrEmpty(transaction.StartTagId) &&
                transaction.StartTagId.Equals(idTag, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("StopTransaction => override because startTag matches");
                response.IdTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Accepted;
            }

            if (response.IdTagInfo.Status == Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Accepted)
            {
                if (transaction != null && transaction.ChargePointId == chargePointId && !transaction.StopTime.HasValue)
                {
                    if (transaction.ConnectorId > 0)
                    {
                        await UpdateConnectorStatus(transaction.ConnectorId, null, null, (double)req.MeterStop / 1000.0, req.Timestamp);
                    }

                    bool valid = true;
                    if (!transaction.StartTagId.Equals(idTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var startTag = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == transaction.StartTagId);
                        if (startTag != null)
                        {
                            if (!string.Equals(startTag.ParentTagId, response.IdTagInfo.ParentIdTag, StringComparison.InvariantCultureIgnoreCase))
                            {
                                _logger.LogInformation("StopTransaction => Start-Tag and End-Tag do not match");
                                response.IdTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Invalid;
                                valid = false;
                            }
                        }
                    }

                    if (valid)
                    {
                        transaction.StopTagId = idTag;
                        transaction.MeterStop = (double)req.MeterStop / 1000.0;
                        transaction.StopReason = req.Reason.ToString();
                        transaction.StopTime = req.Timestamp.UtcDateTime;
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogError("StopTransaction => Unknown or not matching transaction");
                    response.IdTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Invalid;
                }
            }

        }
        catch (Exception exp)
        {
            _logger.LogError(exp, "StopTransaction => Exception: {0}", exp.Message);
            response.IdTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Invalid;
        }

        return response;
    }

    private async Task<Application.Ocpp16.Messages_OCPP16.IdTagInfo> InternalAuthorize(string idTag, int connectorId, string transactionUid, string transactionStartId)
    {
        var idTagInfo = new Application.Ocpp16.Messages_OCPP16.IdTagInfo
        {
            ExpiryDate = default,
            ParentIdTag = string.Empty,
            Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Accepted
        };

        try
        {
            var ct = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == idTag);
            if (ct != null)
            {
                if (ct.ExpiryDate.HasValue)
                    idTagInfo.ExpiryDate = ct.ExpiryDate.Value;

                idTagInfo.ParentIdTag = ct.ParentTagId;
                if (ct.Blocked.HasValue && ct.Blocked.Value)
                    idTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Blocked;
                else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                    idTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Expired;
                else
                {
                    idTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Accepted;
                }
            }
            else
            {
                idTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Invalid;
            }
        }
        catch (Exception exp)
        {
            _logger.LogError(exp, "InternalAuthorize => Exception: {0}", exp.Message);
            idTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Invalid;
        }

        return idTagInfo;
    }

    // minimal UpdateConnectorStatus
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

    private static string CleanChargeTagId(string rawChargeTagId, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(rawChargeTagId)) return rawChargeTagId;
        int sep = rawChargeTagId.IndexOf('_');
        if (sep >= 0)
        {
            var id = rawChargeTagId.Substring(0, sep);
            logger.LogTrace("CleanChargeTagId => {0} => {1}", rawChargeTagId, id);
            return id;
        }
        return rawChargeTagId;
    }
}
