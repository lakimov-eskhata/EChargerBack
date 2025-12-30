using System.Text.Json;
using System.Text.Json.Serialization;

namespace OCPP.API.Common;

public class JsonRpcMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
        
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
        
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
        
    [JsonPropertyName("params")]
    public JsonElement Params { get; set; }
}
    
public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
        
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
        
    [JsonPropertyName("result")]
    public object? Result { get; set; }
        
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}
    
public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
        
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
        
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}