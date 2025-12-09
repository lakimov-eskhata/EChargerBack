

using Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP20;
using LoggerExtensions = Microsoft.Extensions.Logging.LoggerExtensions;

namespace Application.Ocpp20
{
    public partial class ControllerOCPP20
    {
        public string HandleBootNotification(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            string bootReason = null;
            try
            {
                Logger.LogTrace("Processing boot notification...");
                BootNotificationRequest bootNotificationRequest = DeserializeMessage<BootNotificationRequest>(msgIn);
                Logger.LogTrace("BootNotification => Message deserialized");

                bootReason = bootNotificationRequest?.Reason.ToString();
                Logger.LogInformation("BootNotification => Reason={0}", bootReason);

                BootNotificationResponse bootNotificationResponse = new BootNotificationResponse();
                bootNotificationResponse.CurrentTime = DateTimeOffset.UtcNow;
                bootNotificationResponse.Interval = ConfigurationBinder.GetValue<int>(Configuration, "HeartBeatInterval", 300);  // in seconds

                bootNotificationResponse.CustomData = new CustomDataType();
                bootNotificationResponse.CustomData.VendorId = ControllerOCPP20.VendorId;

                if (ChargePointStatus != null)
                {
                    // Known charge station => accept
                    bootNotificationResponse.Status = RegistrationStatusEnumType.Accepted;
                }
                else
                {
                    // Unknown charge station => reject
                    bootNotificationResponse.Status = RegistrationStatusEnumType.Rejected;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(bootNotificationResponse);
                Logger.LogTrace("BootNotification => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "BootNotification => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, bootReason, errorCode);
            return errorCode;
        }
    }
}
