

using Application.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP20;

namespace Application.Ocpp20
{
    public partial class ControllerOCPP20
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
