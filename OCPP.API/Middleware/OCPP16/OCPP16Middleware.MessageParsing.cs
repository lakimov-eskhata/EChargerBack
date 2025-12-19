using System.Text.Json;

namespace OCPP.API.Middleware.OCPP16;

public partial class OCPP16Middleware
{
    private string DetermineMessageType(string message)
    {
        try
        {
            var json = JsonDocument.Parse(message);
            var messageTypeId = json.RootElement[0].GetInt32();
                
            return messageTypeId switch
            {
                2 => GetActionName(json.RootElement[2]), // Call
                3 => "CallResult",
                4 => "CallError",
                _ => "Unknown"
            };
        }
        catch
        {
            return "Invalid";
        }
    }
        
    private string GetActionName(JsonElement element)
    {
        return element.GetString() ?? "Unknown";
    }
}