using System.Threading.Tasks;
using Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPPMiddleware = Application.Common.Middleware.OCPPMiddleware;
using Application.Common.Interfaces;
using Infrastructure;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.EntityFrameworkCore;

namespace OCPP.API.Middleware.OCPP16.Handlers
{
    public class AuthorizeHandler
    {
        private readonly ILogger<AuthorizeHandler> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly OCPPCoreContext _dbContext;

        public AuthorizeHandler(ILogger<AuthorizeHandler> logger, ILoggerFactory loggerFactory, IConfiguration configuration, OCPPCoreContext dbContext)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _configuration = configuration;
            _dbContext = dbContext;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogTrace("Processing authorize request (handler)...");

            var jsonElem = (System.Text.Json.JsonElement)message;
            var payload = jsonElem[3].GetRawText();
            var authorizeRequest = JsonConvert.DeserializeObject<AuthorizeRequest>(payload);

            string idTag = CleanChargeTagId(authorizeRequest.IdTag, _logger);
            var response = new AuthorizeResponse();

            response.IdTagInfo = await InternalAuthorize(idTag);

            return response;
        }

        private async Task<IdTagInfo> InternalAuthorize(string idTag)
        {
            var idTagInfo = new IdTagInfo
            {
                ExpiryDate = default,
                ParentIdTag = string.Empty,
                Status = IdTagInfoStatus.Accepted
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
                        idTagInfo.Status = IdTagInfoStatus.Blocked;
                    else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                        idTagInfo.Status = IdTagInfoStatus.Expired;
                    else idTagInfo.Status = IdTagInfoStatus.Accepted;
                }
                else
                {
                    idTagInfo.Status = IdTagInfoStatus.Invalid;
                }
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "InternalAuthorize => Exception reading charge tag: {0}", exp.Message);
                idTagInfo.Status = IdTagInfoStatus.Invalid;
            }

            return idTagInfo;
        }

        // Локальная копия CleanChargeTagId
        private static string CleanChargeTagId(string rawChargeTagId, ILogger logger)
        {
            string idTag = rawChargeTagId;

            if (!string.IsNullOrWhiteSpace(rawChargeTagId))
            {
                int sep = rawChargeTagId.IndexOf('_');
                if (sep >= 0)
                {
                    idTag = rawChargeTagId.Substring(0, sep);
                    logger.LogTrace("CleanChargeTagId => Charge tag '{0}' => '{1}'", rawChargeTagId, idTag);
                }
            }

            return idTag;
        }
    }
}
