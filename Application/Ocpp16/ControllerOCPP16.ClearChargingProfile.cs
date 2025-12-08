using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Ocpp16;

public partial class ControllerOCPP16
{
    public void HandleClearChargingProfile(OCPPMessage msgIn, OCPPMessage msgOut)
    {
        Logger.LogInformation("ClearChargingProfile answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

        try
        {
            var clearChargingProfileResponse = DeserializeMessage<ClearChargingProfileResponse>(msgIn);
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