
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
        public string HandleDataTransfer(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            DataTransferResponse dataTransferResponse = new DataTransferResponse();

            try
            {
                Logger.LogTrace("Processing data transfer...");
                DataTransferRequest dataTransferRequest = DeserializeMessage<DataTransferRequest>(msgIn);
                Logger.LogTrace("DataTransfer => Message deserialized");

                if (ChargePointStatus != null)
                {
                    // Known charge station
                    WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, string.Format("VendorId={0} / MessageId={1} / Data={2}", dataTransferRequest.VendorId, dataTransferRequest.MessageId, dataTransferRequest.Data), errorCode);
                    dataTransferResponse.Status = DataTransferStatusEnumType.Accepted;
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(dataTransferResponse);
                Logger.LogTrace("DataTransfer => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "DataTransfer => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            return errorCode;
        }
    }
}
