using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public string HandleHeartBeat(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;

            Logger.LogTrace("Processing heartbeat...");
            HeartbeatResponse heartbeatResponse = new HeartbeatResponse();
            heartbeatResponse.CurrentTime = DateTimeOffset.UtcNow;

            msgOut.JsonPayload = JsonConvert.SerializeObject(heartbeatResponse);
            Logger.LogTrace("Heartbeat => Response serialized");

            WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, null, errorCode);
            return errorCode;
        }
    }
}
