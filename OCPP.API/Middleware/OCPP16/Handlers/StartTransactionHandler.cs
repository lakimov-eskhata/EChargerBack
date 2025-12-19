using System.Text.Json.Nodes;
using OCPP.API.Middleware.Common;
using Application.Ocpp16.Messages_OCPP16;
using Application.Common.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class StartTransactionHandler : IMessageHandler
{
    private readonly ILogger<StartTransactionHandler> _logger;
    private readonly OCPPCoreContext _dbContext;
    private readonly IConfiguration _configuration;

    public StartTransactionHandler(ILogger<StartTransactionHandler> logger, OCPPCoreContext dbContext, IConfiguration configuration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogTrace("StartTransaction => processing");

        var jsonElem = (System.Text.Json.JsonElement)message;
        var payload = jsonElem[3].GetRawText();
        var req = JsonConvert.DeserializeObject<StartTransactionRequest>(payload);

        var response = new StartTransactionResponse();

        string idTag = CleanChargeTagId(req.IdTag, _logger);
        bool denyConcurrentTx = _configuration.GetValue<bool>("DenyConcurrentTx", false);

        // Authorize
        var idTagInfo = await InternalAuthorize(idTag, denyConcurrentTx);
        response.IdTagInfo = idTagInfo;

        if (idTagInfo.Status == Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.Accepted)
        {
            try
            {
                var transaction = new Domain.Entities.Station.TransactionEntity
                {
                    ChargePointId = chargePointId,
                    ConnectorId = req.ConnectorId,
                    StartTagId = idTag,
                    StartTime = req.Timestamp.UtcDateTime,
                    MeterStart = (double)req.MeterStart / 1000.0,
                    StartResult = idTagInfo.Status.ToString()
                };

                await _dbContext.Transactions.AddAsync(transaction);
                await _dbContext.SaveChangesAsync();

                response.TransactionId = transaction.TransactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartTransaction => Exception writing transaction: {0}", ex.Message);
            }
        }

        return response;
    }

    // Copied and simplified InternalAuthorize
    private async Task<Application.Ocpp16.Messages_OCPP16.IdTagInfo> InternalAuthorize(string idTag, bool denyConcurrentTx)
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
                    if (denyConcurrentTx)
                    {
                        var tx = await _dbContext.Transactions.Where(t => !t.StopTime.HasValue && t.StartTagId == ct.TagId).OrderByDescending(t => t.TransactionId).FirstOrDefaultAsync();
                        if (tx != null)
                            idTagInfo.Status = Application.Ocpp16.Messages_OCPP16.IdTagInfoStatus.ConcurrentTx;
                    }
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
