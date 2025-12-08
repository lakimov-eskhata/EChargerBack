using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public void HandleReset(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("Reset answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                ResetResponse resetResponse = DeserializeMessage<ResetResponse>(msgIn);
                Logger.LogInformation("Reset => Answer status: {0}", resetResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, resetResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(resetResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleReset => API response: {0}" , apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleReset => Exception: {0}", exp.Message);
            }
        }
    }
}
