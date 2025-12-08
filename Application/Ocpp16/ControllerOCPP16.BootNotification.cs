using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public string HandleBootNotification(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;

            try
            {
                Logger.LogTrace("Processing boot notification...");
                var bootNotificationRequest = DeserializeMessage<BootNotificationRequest>(msgIn);
                Logger.LogTrace("BootNotification => Message deserialized");

                var bootNotificationResponse = new BootNotificationResponse
                {
                    CurrentTime = DateTimeOffset.UtcNow,
                    Interval = Configuration.GetValue<int>("HeartBeatInterval", 300), // in seconds
                    Status = ChargePointStatus != null
                        ? BootNotificationResponseStatus.Accepted
                        : BootNotificationResponseStatus.Rejected
                };

                msgOut.JsonPayload = JsonConvert.SerializeObject(bootNotificationResponse);
                Logger.LogTrace("BootNotification => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "BootNotification => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, null, errorCode);
            return errorCode;
        }
    }
}