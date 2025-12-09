using Application.Common;
using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCPP.Core.Server;
using OCPPMiddleware = Application.Common.Middleware.OCPPMiddleware;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16 : OccpControllerBase
    {
        /// <summary>
        /// Internal string for OCPP protocol version
        /// </summary>
        protected override string ProtocolVersion
        {
            get { return "16"; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ControllerOCPP16(IConfiguration config, ILoggerFactory loggerFactory, ChargePointStatus chargePointStatusStatusStatus, OCPPCoreContext dbContext) :
            base(config, loggerFactory, chargePointStatusStatusStatus, dbContext)
        {
            Logger = loggerFactory.CreateLogger(typeof(ControllerOCPP16));
        }

        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public async Task<OCPPMessage> ProcessRequest(OCPPMessage msgIn, OCPPMiddleware ocppMiddleware)
        {
            OCPPMessage msgOut = new OCPPMessage();
            msgOut.MessageType = "3";
            msgOut.UniqueId = msgIn.UniqueId;

            string errorCode = null;

            switch (msgIn.Action)
            {
                case "BootNotification":
                    errorCode = HandleBootNotification(msgIn, msgOut);
                    break;
                case "Heartbeat":
                    errorCode = HandleHeartBeat(msgIn, msgOut);
                    break;
                case "Authorize":
                    errorCode = await HandleAuthorize(msgIn, msgOut, ocppMiddleware);
                    break;
                case "StartTransaction":
                    errorCode = await HandleStartTransaction(msgIn, msgOut, ocppMiddleware);
                    break;
                case "StopTransaction":
                    errorCode = await HandleStopTransaction(msgIn, msgOut, ocppMiddleware);
                    break;
                case "MeterValues":
                    errorCode = await HandleMeterValues(msgIn, msgOut);
                    break;
                case "StatusNotification":
                    errorCode = await HandleStatusNotification(msgIn, msgOut);
                    break;
                case "DataTransfer":
                    errorCode = HandleDataTransfer(msgIn, msgOut);
                    break;
                default:
                    errorCode = ErrorCodes.NotSupported;
                    WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, errorCode);
                    break;
            }

            if (!string.IsNullOrEmpty(errorCode))
            {
                // Inavlid message type => return type "4" (CALLERROR)
                msgOut.MessageType = "4";
                msgOut.ErrorCode = errorCode;
                Logger.LogDebug("ControllerOCPP16 => Return error code messge: ErrorCode={0}", errorCode);
            }

            return msgOut;
        }


        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public void ProcessAnswer(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            // The response (msgIn) has no action => check action in original request (msgOut)
            switch (msgOut.Action)
            {
                case "Reset":
                    HandleReset(msgIn, msgOut);
                    break;
                case "UnlockConnector":
                    HandleUnlockConnector(msgIn, msgOut);
                    break;
                case "SetChargingProfile":
                    HandleSetChargingProfile(msgIn, msgOut);
                    break;
                case "ClearChargingProfile":
                    HandleClearChargingProfile(msgIn, msgOut);
                    break;
                case "RemoteStartTransaction":
                    HandleRemoteStartTransaction(msgIn, msgOut);
                    break;
                case "RemoteStopTransaction":
                    HandleRemoteStopTransaction(msgIn, msgOut);
                    break;
                default:
                    WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, "Unknown answer");
                    break;
            }
        }

        /// <summary>
        /// Helper function for writing a log entry in database
        /// </summary>
        private void WriteMessageLog(string chargePointId, int? connectorId, string message, string result, string errorCode)
        {
            try
            {
                int dbMessageLog = Configuration.GetValue<int>("DbMessageLog", 0);
                if (dbMessageLog > 0 && !string.IsNullOrWhiteSpace(chargePointId))
                {
                    bool doLog = (dbMessageLog > 1 ||
                                    (message != "BootNotification" &&
                                     message != "Heartbeat" &&
                                     message != "DataTransfer" &&
                                     message != "StatusNotification"));

                    // if (doLog)
                    // {
                    //     MessageLog msgLog = new MessageLog();
                    //     msgLog.ChargePointId = chargePointId;
                    //     msgLog.ConnectorId = connectorId;
                    //     msgLog.LogTime = DateTime.UtcNow;
                    //     msgLog.Message = message;
                    //     msgLog.Result = result;
                    //     msgLog.ErrorCode = errorCode;
                    //     DbContext.MessageLogs.Add(msgLog);
                    //     Logger.LogTrace("MessageLog => Writing entry '{0}'", message);
                    //     DbContext.SaveChanges();
                    //     /*
                    //      * Problem with async operation and ID generation (conflict with EF tracking)
                    //     _ = DbContext.SaveChangesAsync().ContinueWith(task =>
                    //     {
                    //         if (task.IsFaulted && task.Exception != null)
                    //         {
                    //             foreach (var exp in task.Exception.InnerExceptions)
                    //             {
                    //                 Logger.LogError(exp, "ControllerOCPP16.WriteMessageLog=> Error writing message async to DB: '{0}'", message);
                    //             }
                    //         }
                    //     });
                    //     */
                    // }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "MessageLog => Error writing entry '{0}'", message);
            }
        }
    }
}
