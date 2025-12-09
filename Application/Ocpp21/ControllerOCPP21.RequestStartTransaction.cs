
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server.Messages_OCPP21;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP21
    {
        public void HandleRequestStartTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("HandleRequestStartTransaction answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                RequestStartTransactionResponse requestStartTransactionResponse = DeserializeMessage<RequestStartTransactionResponse>(msgIn);
                Logger.LogInformation("HandleRequestStartTransaction => Answer status: {0}", requestStartTransactionResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, requestStartTransactionResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(requestStartTransactionResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleRequestStartTransaction => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleRequestStartTransaction => Exception: {0}", exp.Message);
            }
        }
    }
}
