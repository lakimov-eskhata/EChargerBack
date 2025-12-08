using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public void HandleSetChargingProfile(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("SetChargingProfile answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                SetChargingProfileResponse setChargingProfileResponse = DeserializeMessage<SetChargingProfileResponse>(msgIn);
                Logger.LogInformation("HandleSetChargingProfile => Answer status: {0}", setChargingProfileResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, setChargingProfileResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(setChargingProfileResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleSetChargingProfile => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleSetChargingProfile => Exception: {0}", exp.Message);
            }
        }
    }
}
