

using Application.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP20;
using LoggerExtensions = Microsoft.Extensions.Logging.LoggerExtensions;

namespace Application.Ocpp20
{
    public partial class ControllerOCPP20
    {
        public void HandleClearChargingProfile(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("ClearChargingProfile answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                ClearChargingProfileResponse clearChargingProfileResponse = DeserializeMessage<ClearChargingProfileResponse>(msgIn);
                Logger.LogInformation("HandleClearChargingProfile => Answer status: {0}", clearChargingProfileResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, clearChargingProfileResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(clearChargingProfileResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleClearChargingProfile => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleClearChargingProfile => Exception: {0}", exp.Message);
            }
        }
    }
}
