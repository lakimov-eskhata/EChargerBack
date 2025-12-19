using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server;
using OCPPMiddleware = Application.Common.Middleware.OCPPMiddleware;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public async Task<string> HandleAuthorize(OCPPMessage msgIn, OCPPMessage msgOut, OCPPMiddleware ocppMiddleware)
        {
            string errorCode = null;
            var authorizeResponse = new AuthorizeResponse();

            string idTag = null;
            try
            {
                Logger.LogTrace("Processing authorize request...");
                AuthorizeRequest authorizeRequest = DeserializeMessage<AuthorizeRequest>(msgIn);
                Logger.LogTrace("Authorize => Message deserialized");
                idTag = CleanChargeTagId(authorizeRequest.IdTag, Logger);

                authorizeResponse.IdTagInfo = await InternalAuthorize(idTag, ocppMiddleware, 0, AuthAction.Authorize, string.Empty, string.Empty, false);

                msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                Logger.LogTrace("Authorize => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Authorize => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTagInfo?.Status}", errorCode);
            return errorCode;
        }

        /// <summary>
        /// Authorization logic for reuseability
        /// </summary>
        public async Task<IdTagInfo> InternalAuthorize(string idTag, OCPPMiddleware ocppMiddleware, int connectorId, AuthAction authAction, string transactionUid, string transactionStartId, bool denyConcurrentTx)
        {
            var idTagInfo = new IdTagInfo
            {
                ExpiryDate = default,
                ParentIdTag = null,
                Status = IdTagInfoStatus.Accepted
            };
            idTagInfo.ParentIdTag = string.Empty;
            idTagInfo.ExpiryDate = MaxExpiryDate;
            try
            {
                var ct = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == idTag);
                if (ct != null)
                {
                    if (ct.ExpiryDate.HasValue)
                    {
                        idTagInfo.ExpiryDate = ct.ExpiryDate.Value;
                    }

                    idTagInfo.ParentIdTag = ct.ParentTagId;
                    if (ct.Blocked.HasValue && ct.Blocked.Value)
                    {
                        idTagInfo.Status = IdTagInfoStatus.Blocked;
                    }
                    else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                    {
                        idTagInfo.Status = IdTagInfoStatus.Expired;
                    }
                    else
                    {
                        idTagInfo.Status = IdTagInfoStatus.Accepted;

                        if (denyConcurrentTx)
                        {
                            // Check that no open transaction with this idTag exists
                            var tx = await _dbContext.Transactions
                                .Where(t => !t.StopTime.HasValue && t.StartTagId == ct.TagId)
                                .OrderByDescending(t => t.TransactionId)
                                .FirstOrDefaultAsync();

                            if (tx != null)
                            {
                                idTagInfo.Status = IdTagInfoStatus.ConcurrentTx;
                            }
                        }
                    }
                }
                else
                {
                    idTagInfo.Status = IdTagInfoStatus.Invalid;
                }

                Logger.LogInformation("InternalAuthorize => DB-Auth : Action={0}, Tag='{1}' => Status: {2}", authAction, idTag, idTagInfo.Status);
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "InternalAuthorize => Exception reading charge tag (action={0}, tag={1}): {2}", authAction, idTag, exp.Message);
                idTagInfo.Status = IdTagInfoStatus.Invalid;
            }


            return idTagInfo;
        }
    }
}