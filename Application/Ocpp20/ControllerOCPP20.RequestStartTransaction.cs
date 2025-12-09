using Application.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP20;
using LoggerExtensions = Microsoft.Extensions.Logging.LoggerExtensions;

namespace Application.Ocpp20
{
    public partial class ControllerOCPP20
    {
        public void HandleRequestStartTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("HandleRequestStartTransaction answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                RequestStartTransactionResponse requestStartTransactionResponse = DeserializeMessage<RequestStartTransactionResponse>(msgIn);
                Logger.LogInformation("HandleRequestStartTransaction => Answer status: {0}", requestStartTransactionResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, requestStartTransactionResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(requestStartTransactionResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleRequestStartTransaction => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleRequestStartTransaction => Exception: {0}", exp.Message);
            }
        }
    }
}
