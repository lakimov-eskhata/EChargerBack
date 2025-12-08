using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Ocpp16;

public partial class ControllerOCPP16
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
                dataTransferResponse.Status = DataTransferResponseStatus.Accepted;
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