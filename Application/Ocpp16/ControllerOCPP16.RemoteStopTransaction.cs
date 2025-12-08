using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public void HandleRemoteStopTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("RemoteStopTransaction answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                RemoteStopTransactionResponse remoteStopTransactionResponse = DeserializeMessage<RemoteStopTransactionResponse>(msgIn);
                Logger.LogInformation("HandleRemoteStopTransaction => Answer status: {0}", remoteStopTransactionResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, remoteStopTransactionResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(remoteStopTransactionResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleRemoteStopTransaction => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleRemoteStopTransaction => Exception: {0}", exp.Message);
            }
        }
    }
}
