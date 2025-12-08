using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Domain.Entities.Station;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public async Task<string> HandleStartTransaction(OCPPMessage msgIn, OCPPMessage msgOut, OCPPMiddleware ocppMiddleware)
        {
            string errorCode = null;
            StartTransactionResponse startTransactionResponse = new StartTransactionResponse();

            int connectorId = -1;
            bool denyConcurrentTx = Configuration.GetValue<bool>("DenyConcurrentTx", false);

            try
            {
                Logger.LogTrace("Processing startTransaction request...");
                StartTransactionRequest startTransactionRequest = DeserializeMessage<StartTransactionRequest>(msgIn);
                Logger.LogTrace("StartTransaction => Message deserialized");

                string idTag = CleanChargeTagId(startTransactionRequest.IdTag, Logger);

                startTransactionResponse.IdTagInfo = await InternalAuthorize(idTag, ocppMiddleware, startTransactionRequest.ConnectorId, AuthAction.StartTransaction, string.Empty, string.Empty, denyConcurrentTx);

                if (connectorId > 0)
                {
                    // Update meter value in db connector status 
                    UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), startTransactionRequest.Timestamp, (double)startTransactionRequest.MeterStart / 1000, startTransactionRequest.Timestamp);
                    UpdateMemoryConnectorStatus(connectorId, (double)startTransactionRequest.MeterStart / 1000, startTransactionRequest.Timestamp, null, null);
                }

                if (startTransactionResponse.IdTagInfo.Status == IdTagInfoStatus.Accepted)
                {
                    try
                    {
                        var transaction = new TransactionEntity
                        {
                            ChargePointId = ChargePointStatus?.Id,
                            ConnectorId = startTransactionRequest.ConnectorId,
                            StartTagId = idTag,
                            StartTime = startTransactionRequest.Timestamp.UtcDateTime,
                            MeterStart = (double)startTransactionRequest.MeterStart / 1000, // Meter value here is always Wh
                            StartResult = startTransactionResponse.IdTagInfo.Status.ToString()
                        };
                        
                        await _dbContext.Transactions.AddAsync(transaction);
                        await _dbContext.SaveChangesAsync();

                        // Return DB-ID as transaction ID
                        startTransactionResponse.TransactionId = transaction.TransactionId;
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StartTransaction => Exception writing transaction: chargepoint={0} / tag={1}", ChargePointStatus?.Id, idTag);
                        errorCode = ErrorCodes.InternalError;
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(startTransactionResponse);
                Logger.LogTrace("StartTransaction => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "StartTransaction => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, connectorId, msgIn.Action, startTransactionResponse.IdTagInfo?.Status.ToString(), errorCode);
            return errorCode;
        }
    }
}
