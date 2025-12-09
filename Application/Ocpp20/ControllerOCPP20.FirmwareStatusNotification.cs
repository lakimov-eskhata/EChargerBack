

using Application.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP20;
using LoggerExtensions = Microsoft.Extensions.Logging.LoggerExtensions;

namespace Application.Ocpp20
{
    public partial class ControllerOCPP20
    {
        public string HandleFirmwareStatusNotification(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;

            Logger.LogTrace("Processing FirmwareStatusNotification...");
            FirmwareStatusNotificationResponse firmwareStatusNotificationResponse = new FirmwareStatusNotificationResponse();
            firmwareStatusNotificationResponse.CustomData = new CustomDataType();
            firmwareStatusNotificationResponse.CustomData.VendorId = ControllerOCPP20.VendorId;

            string status = null;

            try
            {
                FirmwareStatusNotificationRequest firmwareStatusNotificationRequest = DeserializeMessage<FirmwareStatusNotificationRequest>(msgIn);
                Logger.LogTrace("FirmwareStatusNotification => Message deserialized");


                if (ChargePointStatus != null)
                {
                    // Known charge station
                    status = firmwareStatusNotificationRequest.Status.ToString();
                    Logger.LogInformation("FirmwareStatusNotification => Status={0}", status);
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(firmwareStatusNotificationResponse);
                Logger.LogTrace("FirmwareStatusNotification => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "FirmwareStatusNotification => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, status, errorCode);
            return errorCode;
        }
    }
}
