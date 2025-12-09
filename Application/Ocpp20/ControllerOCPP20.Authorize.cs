using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Middleware;
using Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP20;

namespace Application.Ocpp20
{
    public partial class ControllerOCPP20
    {
        public async Task<string> HandleAuthorize(OCPPMessage msgIn, OCPPMessage msgOut, Application.Common.Middleware.OCPPMiddleware ocppMiddleware)
        {
            string errorCode = null;
            AuthorizeResponse authorizeResponse = new AuthorizeResponse();

            string idTag = null;
            try
            {
                Logger.LogTrace("Processing authorize request...");
                AuthorizeRequest authorizeRequest = DeserializeMessage<AuthorizeRequest>(msgIn);
                Logger.LogTrace("Authorize => Message deserialized");
                idTag = CleanChargeTagId(authorizeRequest.IdToken?.IdToken, Logger);

                authorizeResponse.IdTokenInfo = await InternalAuthorize(idTag);

                authorizeResponse.CustomData = new CustomDataType();
                authorizeResponse.CustomData.VendorId = VendorId;

                msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                Logger.LogTrace("Authorize => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Authorize => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTokenInfo?.Status}", errorCode);
            return errorCode;
        }

        /// <summary>
        /// Authorization logic for reuseability
        /// </summary>
        internal async Task<IdTokenInfoType> InternalAuthorize(string idTag)
        {
            var idTagInfo = new IdTokenInfoType();

            try
            {
                var ct = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == idTag);
                if (ct != null)
                {
                    if (!string.IsNullOrEmpty(ct.ParentTagId))
                    {
                        idTagInfo.GroupIdToken = new IdTokenType
                        {
                            IdToken = ct.ParentTagId
                        };
                    }

                    if (ct.Blocked.HasValue && ct.Blocked.Value)
                    {
                        idTagInfo.Status = AuthorizationStatusEnumType.Blocked;
                    }
                    else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                    {
                        idTagInfo.Status = AuthorizationStatusEnumType.Expired;
                    }
                    else
                    {
                        idTagInfo.Status = AuthorizationStatusEnumType.Accepted;
                    }
                }
                else
                {
                    idTagInfo.Status = AuthorizationStatusEnumType.Invalid;
                }

                Logger.LogInformation("Authorize => Status: {0}", idTagInfo.Status);
            }
            catch (Exception exp)
            {
                Logger.LogError("Authorize => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                idTagInfo.Status = AuthorizationStatusEnumType.Invalid;
            }

            return idTagInfo;
        }
    }
}