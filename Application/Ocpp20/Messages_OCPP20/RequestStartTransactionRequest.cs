
namespace OCPP.Core.Server.Messages_OCPP20
{
#pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class RequestStartTransactionRequest
    {
        [Newtonsoft.Json.JsonProperty("customData", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public CustomDataType? CustomData { get; set; }

        /// <summary>Number of the EVSE on which to start the transaction. EvseId SHALL be &amp;gt; 0
        /// </summary>
        [Newtonsoft.Json.JsonProperty("evseId", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? EvseId { get; set; }

        [Newtonsoft.Json.JsonProperty("groupIdToken", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public IdTokenType? GroupIdToken { get; set; }

        [Newtonsoft.Json.JsonProperty("idToken", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public IdTokenType IdToken { get; set; } = new IdTokenType();

        /// <summary>Id given by the server to this start request. The Charging Station might return this in the &amp;lt;&amp;lt;transactioneventrequest, TransactionEventRequest&amp;gt;&amp;gt;, letting the server know which transaction was started for this request. Use to start a transaction.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("remoteStartId", Required = Newtonsoft.Json.Required.Always)]
        public int RemoteStartId { get; set; }

        [Newtonsoft.Json.JsonProperty("chargingProfile", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ChargingProfileType? ChargingProfile { get; set; }
    }
}