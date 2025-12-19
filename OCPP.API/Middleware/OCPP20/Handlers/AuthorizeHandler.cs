// placeholder file replaced earlier; will overwrite with full logic
using OCPP.API.Middleware.Common;
using OCPP.Core.Server.Messages_OCPP20;
using Newtonsoft.Json;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using OCPP.API.Middleware.OCPP16;

namespace OCPP.API.Middleware.OCPP20.Handlers;

public class AuthorizeHandler : IMessageHandler
{
    private readonly ILogger<AuthorizeHandler> _logger;
    private readonly OCPPCoreContext _dbContext;

    public AuthorizeHandler(ILogger<AuthorizeHandler> logger, OCPPCoreContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogTrace("Authorize (2.0) => processing");
        var json = (System.Text.Json.JsonElement)message;
        string payload = string.Empty;
        if (json.ValueKind == System.Text.Json.JsonValueKind.Object && json.TryGetProperty("params", out var p))
            payload = p.GetRawText();
        else if (json.ValueKind == System.Text.Json.JsonValueKind.Array && json.GetArrayLength() > 2)
            payload = json[2].GetRawText();

        try
        {
            var req = JsonConvert.DeserializeObject<AuthorizeRequest>(payload);
            var resp = new AuthorizeResponse();
            var idToken = req?.IdToken?.IdToken;
            var clean = CleanChargeTagId(idToken, _logger);

            resp.IdTokenInfo = await InternalAuthorize(clean);
            resp.CustomData = new CustomDataType { VendorId = "DefaultVendor" };

            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authorize(2.0) => Exception: {0}", ex.Message);
            return new { error = "FormationViolation" };
        }
    }

    private async Task<AuthorizationStatusEnumType> MapInternalStatusToEnum(string idTag)
    {
        return AuthorizationStatusEnumType.Accepted;
    }

    private async Task<OCPP.Core.Server.Messages_OCPP20.IdTokenInfoType> InternalAuthorize(string idTag)
    {
        var idTokenInfo = new OCPP.Core.Server.Messages_OCPP20.IdTokenInfoType();
        try
        {
            var ct = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == idTag);
            if (ct != null)
            {
                if (!string.IsNullOrEmpty(ct.ParentTagId))
                {
                    idTokenInfo.GroupIdToken = new OCPP.Core.Server.Messages_OCPP20.IdTokenType { IdToken = ct.ParentTagId };
                }

                if (ct.Blocked.HasValue && ct.Blocked.Value)
                    idTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                    idTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                else
                    idTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
            }
            else
            {
                idTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
            }
        }
        catch (Exception exp)
        {
            _logger.LogError(exp, "InternalAuthorize(2.0) => Exception: {0}", exp.Message);
            idTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
        }

        return idTokenInfo;
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

