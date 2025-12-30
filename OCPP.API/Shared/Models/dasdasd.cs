namespace OCPP.API.Shared.Models;

// Общие DTO для всех версий OCPP
    public class ServerCommand
    {
        public string Action { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public object Payload { get; set; } = new object();
        public string ProtocolVersion { get; set; } = "1.6";
    }
    
    public class CommandResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Response { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    // DTO для OCPP 2.0/2.1 команд
    public class GetVariableRequest
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
        public string VariableName { get; set; } = string.Empty;
        public string AttributeType { get; set; } = "Actual";
    }
    
    public class SetVariableRequest
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
        public string VariableName { get; set; } = string.Empty;
        public string AttributeType { get; set; } = "Actual";
        public string AttributeValue { get; set; } = string.Empty;
    }
    
    public class ComponentCriteria
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
    }
    
    public class ComponentVariable
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
        public string VariableName { get; set; } = string.Empty;
    }
    
    public class IdTokenClass
    {
        public string IdToken { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public AdditionalInfo[]? AdditionalInfo { get; set; }
    }
    
    public class AdditionalInfo
    {
        public string AdditionalIdToken { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
    
    // DTO для запросов от контроллеров
    public class ResetRequestDto
    {
        public string Type { get; set; } = "Soft";
    }
    
    public class UnlockConnectorRequestDto
    {
        public int EvseId { get; set; }
    }