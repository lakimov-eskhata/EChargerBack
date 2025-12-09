using Application.Common.Interfaces;
using Application.Common.Models;
using ChargeTag = Domain.Entities.Station.ChargeTagEntity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP21;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP21
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

                authorizeResponse.CustomData = new CustomDataType();
                authorizeResponse.CustomData.VendorId = VendorId;

                authorizeResponse.IdTokenInfo = new IdTokenInfoType();


                try
                {
                    var ct = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == idTag);
                    if (ct != null)
                    {
                        if (!string.IsNullOrEmpty(ct.ParentTagId))
                        {
                            authorizeResponse.IdTokenInfo.GroupIdToken = new IdTokenType();
                            authorizeResponse.IdTokenInfo.GroupIdToken.IdToken = ct.ParentTagId;
                        }

                        if (ct.Blocked.HasValue && ct.Blocked.Value)
                        {
                            authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                        }
                        else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                        {
                            authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                        }
                        else
                        {
                            authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                        }
                    }
                    else
                    {
                        authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    }

                    Logger.LogInformation("Authorize => Status: {0}", authorizeResponse.IdTokenInfo.Status);
                }
                catch (Exception exp)
                {
                    Logger.LogError(exp, "Authorize => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                    authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                }


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
            IdTokenInfoType idTagInfo = new IdTokenInfoType();


            try
            {
                ChargeTag ct = await _dbContext.ChargeTags.FirstOrDefaultAsync(x => x.TagId == idTag);
                if (ct != null)
                {
                    if (!string.IsNullOrEmpty(ct.ParentTagId))
                    {
                        idTagInfo.GroupIdToken = new IdTokenType();
                        idTagInfo.GroupIdToken.IdToken = ct.ParentTagId;
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
                Logger.LogError(exp, "Authorize => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                idTagInfo.Status = AuthorizationStatusEnumType.Invalid;
            }


            return idTagInfo;
        }
    }
}