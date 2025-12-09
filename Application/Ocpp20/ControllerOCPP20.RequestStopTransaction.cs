using Application.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP20;

namespace Application.Ocpp20
{
    public partial class ControllerOCPP20
    {
        public void HandleRequestStopTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("RequestStopTransaction answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                RequestStopTransactionResponse requestStopTransactionResponse = DeserializeMessage<RequestStopTransactionResponse>(msgIn);
                Logger.LogInformation("HandleRequestStopTransaction => Answer status: {0}", requestStopTransactionResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, requestStopTransactionResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(requestStopTransactionResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleRequestStopTransaction => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleRequestStopTransaction => Exception: {0}", exp.Message);
            }
        }
    }
}
