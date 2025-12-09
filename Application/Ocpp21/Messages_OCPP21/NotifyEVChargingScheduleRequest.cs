
namespace OCPP.Core.Server.Messages_OCPP21
{
#pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class NotifyEVChargingScheduleRequest
    {
        /// <summary>Periods contained in the charging profile are relative to this point in time.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("timeBase", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public System.DateTimeOffset TimeBase { get; set; }

        [Newtonsoft.Json.JsonProperty("chargingSchedule", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public ChargingScheduleType ChargingSchedule { get; set; } = new ChargingScheduleType();

        /// <summary>The charging schedule contained in this notification applies to an EVSE. EvseId must be &amp;gt; 0.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("evseId", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
        public int EvseId { get; set; }

        /// <summary>*(2.1)* Id  of the _chargingSchedule_ that EV selected from the provided ChargingProfile.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("selectedChargingScheduleId", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int? SelectedChargingScheduleId { get; set; }

        /// <summary>*(2.1)* True when power tolerance is accepted by EV.
        /// This value is taken from EVPowerProfile.PowerToleranceAcceptance in the ISO 15118-20 PowerDeliverReq message..
        /// </summary>
        [Newtonsoft.Json.JsonProperty("powerToleranceAcceptance", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? PowerToleranceAcceptance { get; set; }

        [Newtonsoft.Json.JsonProperty("customData", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public CustomDataType? CustomData { get; set; }
    }
}