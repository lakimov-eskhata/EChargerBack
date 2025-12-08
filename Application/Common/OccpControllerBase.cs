using System.Reflection;
using Application.Common.Models;
using Domain.Entities.Station;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using OCPP.Core.Server;

namespace Application.Common
{
    public abstract class OccpControllerBase
    {
      
        protected virtual string ProtocolVersion { get;  }
        protected IConfiguration Configuration { get; set; }
        protected ChargePointStatus? ChargePointStatus { get; set; }
        protected ILogger Logger { get; set; }
        protected readonly OCPPCoreContext _dbContext;
        
        public OccpControllerBase(IConfiguration config, ILoggerFactory loggerFactory, ChargePointStatus? chargePointStatus, OCPPCoreContext dbContext)
        {
            Configuration = config;

            if (chargePointStatus != null)
            {
                ChargePointStatus = chargePointStatus;
            }
            else
            {
                Logger.LogError("New ControllerBase => empty chargepoint status");
            }
            _dbContext = dbContext;
        }

        /// <summary>
        /// Deserialize and validate JSON message (if schema file exists)
        /// </summary>
        protected T DeserializeMessage<T>(OCPPMessage msg)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            string codeBase = Path.GetDirectoryName(path);

            bool validateMessages = Configuration.GetValue<bool>("ValidateMessages", false);

            string schemaJson = null;
            if (validateMessages && 
                !string.IsNullOrEmpty(codeBase) && 
                Directory.Exists(codeBase))
            {
                string msgTypeName = typeof(T).Name;
                string filename = Path.Combine(codeBase, $"Schema{ProtocolVersion}", $"{msgTypeName}.json");
                if (File.Exists(filename))
                {
                    Logger.LogTrace("DeserializeMessage => Using schema file: {0}", filename);
                    schemaJson = File.ReadAllText(filename);
                }
            }

            JsonTextReader reader = new JsonTextReader(new StringReader(msg.JsonPayload));
            JsonSerializer serializer = new JsonSerializer();

            if (!string.IsNullOrEmpty(schemaJson))
            {
                var validatingReader = new JSchemaValidatingReader(reader);
                validatingReader.Schema = JSchema.Parse(schemaJson);

                IList<string> messages = new List<string>();
                validatingReader.ValidationEventHandler += (o, a) => messages.Add(a.Message);
                T obj = serializer.Deserialize<T>(validatingReader);
                if (messages.Count > 0)
                {
                    foreach (string err in messages)
                    {
                        Logger.LogWarning("DeserializeMessage {0} => Validation error: {1}", msg.Action, err);
                    }
                    throw new FormatException("Message validation failed");
                }
                return obj;
            }
            else
            {
                // Deserialization WITHOUT schema validation
                Logger.LogTrace("DeserializeMessage => Deserialization without schema validation");
                return serializer.Deserialize<T>(reader);
            }
        }


        /// <summary>
        /// Helper function for creating and updating the ConnectorStatus in then database
        /// </summary>
        protected async Task<bool> UpdateConnectorStatus(int connectorId, string status, DateTimeOffset? statusTime, double? meter, DateTimeOffset? meterTime)
        {
            try
            {
                var connectorStatus = await _dbContext.ConnectorStatuses.
                    FirstOrDefaultAsync(x=> ChargePointStatus != null && x.ChargePointId == ChargePointStatus.Id && x.ConnectorId == connectorId);

                if (connectorStatus == null)
                {
                    connectorStatus = new ConnectorStatusEntity
                    {
                        ChargePointId = ChargePointStatus.Id,
                        ConnectorId = connectorId
                    };
                    Logger.LogTrace("UpdateConnectorStatus => Creating new DB-ConnectorStatus: ID={0} / Connector={1}", connectorStatus.ChargePointId, connectorStatus.ConnectorId);

                    await _dbContext.ConnectorStatuses.AddAsync(connectorStatus);
                    await _dbContext.SaveChangesAsync();
                }

                if (!string.IsNullOrEmpty(status))
                {
                    var dbTime = (statusTime ?? DateTimeOffset.UtcNow).DateTime;
                    
                    connectorStatus.LastStatus = status;
                    connectorStatus.LastStatusTime = dbTime;
                    
                    await _dbContext.SaveChangesAsync();
                }

                if (meter.HasValue)
                {
                    var dbTime = (meterTime ?? DateTimeOffset.UtcNow).DateTime;
                    
                    connectorStatus.LastMeter = meter.Value;
                    connectorStatus.LastMeterTime = dbTime;
                    
                    await _dbContext.SaveChangesAsync();
                }
                Logger.LogInformation("UpdateConnectorStatus => Save ConnectorStatus: ID={0} / Connector={1} / Status={2} / Meter={3}", connectorStatus.ChargePointId, connectorId, status, meter);
                return true;
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "UpdateConnectorStatus => Exception writing connector status (ID={0} / Connector={1}): {2}", ChargePointStatus?.Id, connectorId, exp.Message);
            }

            return false;
        }

        /// <summary>
        /// Set/Update in memory connector status with meter (and more) values
        /// </summary>
        protected void UpdateMemoryConnectorStatus(int connectorId, double meterKWH, DateTimeOffset meterTime, double? currentChargeKW, double? stateOfCharge)
        {
            // Values <1 have no meaning => null
            if (currentChargeKW.HasValue && currentChargeKW < 0) currentChargeKW = null;
            if (stateOfCharge.HasValue && stateOfCharge < 0) stateOfCharge = null;

            OnlineConnectorStatus ocs = null;
            bool isNew = false;
            if (ChargePointStatus.OnlineConnectors.ContainsKey(connectorId))
            {
                ocs = ChargePointStatus.OnlineConnectors[connectorId];
            }
            else
            {
                ocs = new OnlineConnectorStatus();
                isNew = true; // append later when all values are correct
            }

            ocs.ChargeRateKW = currentChargeKW;
            if (meterKWH >= 0 && !currentChargeKW.HasValue &&
                ocs.MeterKWH.HasValue && ocs.MeterKWH <= meterKWH &&
                ocs.MeterValueDate < meterTime)
            {
                try
                {
                    // Chargepoint sends no power (kW) => calculate from meter and time (from last sample)
                    double diffMeter = meterKWH - ocs.MeterKWH.Value;
                    ocs.ChargeRateKW = diffMeter / ((meterTime.Subtract(ocs.MeterValueDate).TotalSeconds) / (60 * 60));
                    Logger.LogDebug("MeterValues => Calculated power for ChargePoint={0} / Connector={1} / Power: {2}kW", ChargePointStatus?.Id, connectorId, ocs.ChargeRateKW);
                }
                catch (Exception exp)
                {
                    Logger.LogWarning("MeterValues => Error calculating power for ChargePoint={0} / Connector={1}: {2}", ChargePointStatus?.Id, connectorId, exp.ToString());
                }
            }
            ocs.MeterKWH = meterKWH;
            ocs.MeterValueDate = meterTime;
            ocs.SoC = stateOfCharge;

            if (isNew)
            {
                if (ChargePointStatus.OnlineConnectors.TryAdd(connectorId, ocs))
                {
                    Logger.LogTrace("MeterValues => Set OnlineConnectorStatus for ChargePoint={0} / Connector={1} / meterKWH: {2}", ChargePointStatus?.Id, connectorId, meterKWH);
                }
                else
                {
                    Logger.LogError("MeterValues => Error adding new OnlineConnectorStatus for ChargePoint={0} / Connector={1} / meterKWH: {2}", ChargePointStatus?.Id, connectorId, meterKWH);
                }
            }
        }

        /// <summary>
        /// Clean charge tag Id from possible suffix ("..._abc")
        /// </summary>
        protected static string CleanChargeTagId(string rawChargeTagId, ILogger logger)
        {
            string idTag = rawChargeTagId;

            // KEBA adds the serial to the idTag ("<idTag>_<serial>") => cut off suffix
            if (!string.IsNullOrWhiteSpace(rawChargeTagId))
            {
                int sep = rawChargeTagId.IndexOf('_');
                if (sep >= 0)
                {
                    idTag = rawChargeTagId.Substring(0, sep);
                    logger.LogTrace("CleanChargeTagId => Charge tag '{0}' => '{1}'", rawChargeTagId, idTag);
                }
            }

            return idTag;
        }

        /// <summary>
        /// Return UtcNow + 1 year
        /// </summary>
        protected static DateTimeOffset MaxExpiryDate
        {
            get
            {
                return DateTime.UtcNow.Date.AddYears(1);
            }
        }
    }
}
