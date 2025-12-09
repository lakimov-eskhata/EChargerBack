using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Ocpp16.Messages_OCPP16;
using Domain.Entities.Station;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server;
using OCPPMiddleware = Application.Common.Middleware.OCPPMiddleware;

namespace Application.Ocpp16
{
    public partial class ControllerOCPP16
    {
        public async Task<string> HandleStopTransaction(OCPPMessage msgIn, OCPPMessage msgOut, OCPPMiddleware ocppMiddleware)
        {
            string errorCode = null;
            StopTransactionResponse stopTransactionResponse = new StopTransactionResponse();
            stopTransactionResponse.IdTagInfo = new IdTagInfo();

            try
            {
                Logger.LogTrace("Processing stopTransaction request...");
                var stopTransactionRequest = DeserializeMessage<StopTransactionRequest>(msgIn);
                Logger.LogTrace("StopTransaction => Message deserialized");

                var idTag = CleanChargeTagId(stopTransactionRequest.IdTag, Logger);

                TransactionEntity? transaction = null;
                try
                {
                    transaction = await _dbContext.Transactions.FirstOrDefaultAsync(x=>x.TransactionId == stopTransactionRequest.TransactionId);
                }
                catch (Exception exp)
                {
                    Logger.LogError(exp, "StopTransaction => Exception reading transaction: transactionId={0} / chargepoint={1}", stopTransactionRequest.TransactionId, ChargePointStatus?.Id);
                    errorCode = ErrorCodes.InternalError;
                }

                if (transaction != null)
                {
                    // Transaction found => check charge tag (the start tag and the car itself can also stop the transaction)

                    if (string.IsNullOrWhiteSpace(idTag))
                    {
                        // no RFID-Tag => accept stop request (can happen when the car stops the charging process)
                        stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                        Logger.LogInformation("StopTransaction => no charge tag => Status: {0}", stopTransactionResponse.IdTagInfo.Status);
                    }
                    else
                    {
                        stopTransactionResponse.IdTagInfo = await InternalAuthorize(idTag, ocppMiddleware, transaction.ConnectorId, AuthAction.StopTransaction, transaction?.Uid, transaction?.StartTagId, false);
                    }
                }
                else
                {
                    // Error unknown transaction id
                    Logger.LogError("StopTransaction => Unknown or not matching transaction: id={0} / chargepoint={1} / tag={2}", stopTransactionRequest.TransactionId, ChargePointStatus?.Id, idTag);
                    WriteMessageLog(ChargePointStatus?.Id, transaction?.ConnectorId, msgIn.Action, string.Format("UnknownTransaction:ID={0}/Meter={1}", stopTransactionRequest.TransactionId, stopTransactionRequest.MeterStop), errorCode);
                    errorCode = ErrorCodes.PropertyConstraintViolation;
                }


                // But...
                // The charge tag which has started the transaction should always be able to stop the transaction.
                // (The owner needs to release his car :-) and the car can always forcingly stop the transaction)
                // => if status!=accepted check if it was the starting tag
                if (stopTransactionResponse.IdTagInfo.Status != IdTagInfoStatus.Accepted &&
                    transaction != null && !string.IsNullOrEmpty(transaction.StartTagId) &&
                    transaction.StartTagId.Equals(idTag, StringComparison.InvariantCultureIgnoreCase)) 
                {
                    // Override => allow the StartTagId to also stop the transaction
                    Logger.LogInformation("StopTransaction => RFID-tag='{0}' NOT accepted => override to ALLOWED because it is the start tag", idTag);
                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                }
                

                // General authorization done. Now check the result and update the transaction
                if (stopTransactionResponse.IdTagInfo.Status == IdTagInfoStatus.Accepted)
                {
                    try
                    {
                        if (transaction != null &&
                            transaction.ChargePointId == ChargePointStatus.Id &&
                            !transaction.StopTime.HasValue)
                        {
                            if (transaction.ConnectorId > 0)
                            {
                                // Update meter value in db connector status 
                                await UpdateConnectorStatus(transaction.ConnectorId, null, null, (double)stopTransactionRequest.MeterStop / 1000, stopTransactionRequest.Timestamp);
                                UpdateMemoryConnectorStatus(transaction.ConnectorId, (double)stopTransactionRequest.MeterStop / 1000, stopTransactionRequest.Timestamp, null, null);
                            }

                            // check current tag against start tag => same tag or identical group?
                            bool valid = true;
                            if (!transaction.StartTagId.Equals(idTag, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // tags are different => identical group?
                                var startTag = await _dbContext.ChargeTags.AsNoTracking().FirstOrDefaultAsync(x=>x.TagId == transaction.StartTagId);
                                if (startTag != null)
                                {
                                    if (!string.Equals(startTag.ParentTagId, stopTransactionResponse.IdTagInfo.ParentIdTag, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        Logger.LogInformation("StopTransaction => Start-Tag ('{0}') and End-Tag ('{1}') do not match: Invalid!", transaction.StartTagId, idTag);
                                        stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                                        valid = false;
                                    }
                                    else
                                    {
                                        Logger.LogInformation("StopTransaction => Different RFID-Tags but matching group ('{0}')", stopTransactionResponse.IdTagInfo.ParentIdTag);
                                    }
                                }
                                else
                                {
                                    Logger.LogError("StopTransaction => Start-Tag not found: '{0}'", transaction.StartTagId);
                                    // assume "valid" and allow to end the transaction
                                }
                            }

                            if (valid)
                            {
                                transaction.StopTagId = idTag;
                                transaction.MeterStop =  (double)stopTransactionRequest.MeterStop / 1000; // Meter value here is always Wh
                                transaction.StopReason = stopTransactionRequest.Reason.ToString();
                                transaction.StopTime = stopTransactionRequest.Timestamp.UtcDateTime;
                                await _dbContext.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            Logger.LogError("StopTransaction => Unknown or not matching transaction: id={0} / chargepoint={1} / tag={2}", stopTransactionRequest.TransactionId, ChargePointStatus?.Id, idTag);
                            WriteMessageLog(ChargePointStatus?.Id, transaction?.ConnectorId, msgIn.Action, string.Format("UnknownTransaction:ID={0}/Meter={1}", stopTransactionRequest.TransactionId, stopTransactionRequest.MeterStop), errorCode);
                            errorCode = ErrorCodes.PropertyConstraintViolation;
                        }
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StopTransaction => Exception writing transaction: chargepoint={0} / tag={1}", ChargePointStatus?.Id, idTag);
                        errorCode = ErrorCodes.InternalError;
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(stopTransactionResponse);
                Logger.LogTrace("StopTransaction => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "StopTransaction => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, stopTransactionResponse.IdTagInfo?.Status.ToString(), errorCode);
            return errorCode;
        }
    }
}
