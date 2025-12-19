

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.API.Middleware.OCPP20.Models
{
#pragma warning disable // Disable all warnings

        /// <summary>Indicates if the Charging Station was able to execute the request.
        /// </summary>
        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
        public enum ClearChargingProfileStatusEnumType
        {
            [System.Runtime.Serialization.EnumMember(Value = @"Accepted")]
            Accepted = 0,

            [System.Runtime.Serialization.EnumMember(Value = @"Unknown")]
            Unknown = 1,
        }

        [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
        public partial class ClearChargingProfileResponse
    {
            [Newtonsoft.Json.JsonProperty("customData", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public CustomDataType? CustomData { get; set; }

            [Newtonsoft.Json.JsonProperty("status", Required = Newtonsoft.Json.Required.Always)]
            [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public ClearChargingProfileStatusEnumType Status { get; set; }

            [Newtonsoft.Json.JsonProperty("statusInfo", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public StatusInfoType? StatusInfo { get; set; }
        }
    }