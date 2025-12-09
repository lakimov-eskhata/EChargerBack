
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP21;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP21
    {
        public string HandleClearedChargingLimit(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;

            Logger.LogTrace("Processing ClearedChargingLimit...");
            ClearedChargingLimitResponse clearedChargingLimitResponse = new ClearedChargingLimitResponse();
            clearedChargingLimitResponse.CustomData = new CustomDataType();
            clearedChargingLimitResponse.CustomData.VendorId = VendorId;

            string source = null;
            int connectorId = 0;

            try
            {
                ClearedChargingLimitRequest clearedChargingLimitRequest = DeserializeMessage<ClearedChargingLimitRequest>(msgIn);
                Logger.LogTrace("ClearedChargingLimit => Message deserialized");

                if (ChargePointStatus != null)
                {
                    // Known charge station
                    source = clearedChargingLimitRequest.ChargingLimitSource.ToString();
                    if (clearedChargingLimitRequest.EvseId.HasValue) connectorId = clearedChargingLimitRequest.EvseId.Value;
                    Logger.LogInformation("ClearedChargingLimit => Source={0}", source);
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(clearedChargingLimitResponse);
                Logger.LogTrace("ClearedChargingLimit => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ClearedChargingLimit => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            WriteMessageLog(ChargePointStatus.Id, connectorId, msgIn.Action, source, errorCode);
            return errorCode;
        }
    }
}
