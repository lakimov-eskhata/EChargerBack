using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Services;

public interface ICommandResponseProcessor
{
    Task ProcessResponseAsync(string chargePointId, string message);
}

public class CommandResponseProcessor : ICommandResponseProcessor
{
    private readonly ILogger<CommandResponseProcessor> _logger;
    private readonly IPendingCommandsStorage _pendingCommands;

    public CommandResponseProcessor(
        ILogger<CommandResponseProcessor> logger,
        IPendingCommandsStorage pendingCommands)
    {
        _logger = logger;
        _pendingCommands = pendingCommands;
    }

    public async Task ProcessResponseAsync(string chargePointId, string message)
    {
        try
        {
            // Определяем тип сообщения (OCPP 1.6 или OCPP 2.0)
            if (message.TrimStart().StartsWith("[")) // OCPP 1.6 формат
            {
                await ProcessOCPP16ResponseAsync(chargePointId, message);
            }
            else if (message.Contains("\"jsonrpc\":\"2.0\"")) // OCPP 2.0 JSON-RPC
            {
                await ProcessOCPP20ResponseAsync(chargePointId, message);
            }
            else
            {
                _logger.LogWarning("Unknown message format from {ChargePointId}: {Message}", 
                    chargePointId, message.Substring(0, Math.Min(100, message.Length)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing response from {ChargePointId}", chargePointId);
        }
    }
    
    public async Task ProcessOCPP16ResponseAsync(string chargePointId, string message)
    {
        try
        {
            var json = JsonDocument.Parse(message);
            var messageTypeId = json.RootElement[0].GetInt32();
            var messageId = json.RootElement[1].GetString();

            // Находим ожидающую команду по messageId
            var pendingCommand = await _pendingCommands.GetAsync(messageId);
            if (pendingCommand == null)
            {
                _logger.LogWarning("Received response for unknown command: {MessageId} from {ChargePointId}",
                    messageId, chargePointId);
                return;
            }

            if (messageTypeId == 3) // CallResult
            {
                await ProcessCallResultAsync(chargePointId, pendingCommand, json.RootElement[2]);
            }
            else if (messageTypeId == 4) // CallError
            {
                await ProcessCallErrorAsync(chargePointId, pendingCommand, json.RootElement);
            }

            // Удаляем команду из ожидания
            await _pendingCommands.RemoveAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing response from {ChargePointId}", chargePointId);
        }
    }

    private async Task ProcessCallResultAsync(string chargePointId, PendingCommand pendingCommand, JsonElement result)
    {
        _logger.LogInformation(
            "Command {Action} succeeded for {ChargePointId}: {Result}",
            pendingCommand.Action, chargePointId, result.ToString());

        // Здесь можно обновить состояние системы на основе ответа
        // Например, обновить статус транзакции, коннектора и т.д.

        switch (pendingCommand.Action)
        {
            case "RemoteStartTransaction":
                await ProcessRemoteStartResultAsync(chargePointId, result);
                break;

            case "RemoteStopTransaction":
                await ProcessRemoteStopResultAsync(chargePointId, result);
                break;

            case "Reset":
                await ProcessResetResultAsync(chargePointId, result);
                break;

            case "UnlockConnector":
                await ProcessUnlockConnectorResultAsync(chargePointId, result);
                break;

            // Добавьте обработку других команд...
        }
    }

    private async Task ProcessCallErrorAsync(string chargePointId, PendingCommand pendingCommand, JsonElement error)
    {
        var errorCode = error[2].GetString();
        var errorDescription = error[3].GetString();
        var errorDetails = error[4].ToString();

        _logger.LogWarning(
            "Command {Action} failed for {ChargePointId}: {ErrorCode} - {ErrorDescription}",
            pendingCommand.Action, chargePointId, errorCode, errorDescription);

        // Здесь можно обработать ошибку
        // Например, отправить уведомление, записать в лог ошибок и т.д.
    }

    private async Task ProcessRemoteStartResultAsync(string chargePointId, JsonElement result)
    {
        // Обработка успешного запуска транзакции
        var status = result.GetProperty("status").GetString();

        if (status == "Accepted")
        {
            _logger.LogInformation("Remote start transaction accepted for {ChargePointId}", chargePointId);
            // Можно обновить статус коннектора в базе данных
        }
        else
        {
            _logger.LogWarning("Remote start transaction rejected for {ChargePointId}", chargePointId);
        }
    }

    private async Task ProcessRemoteStopResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();

        if (status == "Accepted")
        {
            _logger.LogInformation("Remote stop transaction accepted for {ChargePointId}", chargePointId);
            // Обновляем статус транзакции в базе данных
        }
    }

    private async Task ProcessResetResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Reset command {Status} for {ChargePointId}", status, chargePointId);
    }

    private async Task ProcessUnlockConnectorResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Unlock connector command {Status} for {ChargePointId}", status, chargePointId);
    }
    
     private async Task ProcessOCPP20ResponseAsync(string chargePointId, string message)
    {
        try
        {
            var json = JsonDocument.Parse(message);
            var messageId = json.RootElement.GetProperty("id").GetString();
            
            // Находим ожидающую команду
            var pendingCommand = await _pendingCommands.GetAsync(messageId);
            if (pendingCommand == null)
            {
                _logger.LogWarning("Received response for unknown OCPP 2.0 command: {MessageId} from {ChargePointId}", 
                    messageId, chargePointId);
                return;
            }
            
            // Проверяем, есть ли error поле
            if (json.RootElement.TryGetProperty("error", out var errorElement))
            {
                await ProcessOCPP20ErrorAsync(chargePointId, pendingCommand, errorElement);
            }
            else if (json.RootElement.TryGetProperty("result", out var resultElement))
            {
                await ProcessOCPP20ResultAsync(chargePointId, pendingCommand, resultElement);
            }
            
            // Удаляем команду из ожидания
            await _pendingCommands.RemoveAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OCPP 2.0 response from {ChargePointId}", chargePointId);
        }
    }
    
    private async Task ProcessOCPP20ResultAsync(string chargePointId, PendingCommand pendingCommand, JsonElement result)
    {
        _logger.LogInformation(
            "OCPP 2.0 command {Action} succeeded for {ChargePointId}",
            pendingCommand.Action, chargePointId);
        
        // Обработка успешных ответов для разных команд
        switch (pendingCommand.Action)
        {
            case "Reset":
                await ProcessOCPP20ResetResultAsync(chargePointId, result);
                break;
                
            case "UnlockConnector":
                await ProcessOCPP20UnlockConnectorResultAsync(chargePointId, result);
                break;
                
            case "RequestStartTransaction":
                await ProcessOCPP20RequestStartResultAsync(chargePointId, result);
                break;
                
            case "RequestStopTransaction":
                await ProcessOCPP20RequestStopResultAsync(chargePointId, result);
                break;
                
            case "SetChargingProfile":
                await ProcessOCPP20SetChargingProfileResultAsync(chargePointId, result);
                break;
                
            case "ClearChargingProfile":
                await ProcessOCPP20ClearChargingProfileResultAsync(chargePointId, result);
                break;
                
            case "ChangeAvailability":
                await ProcessOCPP20ChangeAvailabilityResultAsync(chargePointId, result);
                break;
                
            case "GetVariables":
                await ProcessOCPP20GetVariablesResultAsync(chargePointId, result);
                break;
                
            case "SetVariables":
                await ProcessOCPP20SetVariablesResultAsync(chargePointId, result);
                break;
                
            // Добавьте обработку других команд OCPP 2.0...
        }
    }
    
    private async Task ProcessOCPP20ErrorAsync(string chargePointId, PendingCommand pendingCommand, JsonElement error)
    {
        var errorCode = error.GetProperty("code").GetInt32();
        var errorMessage = error.GetProperty("message").GetString();
        var errorData = error.TryGetProperty("data", out var dataElement) ? dataElement.ToString() : null;
        
        _logger.LogWarning(
            "OCPP 2.0 command {Action} failed for {ChargePointId}: Code={ErrorCode}, Message={ErrorMessage}, Data={ErrorData}",
            pendingCommand.Action, chargePointId, errorCode, errorMessage, errorData);
        
        // Здесь можно обработать специфичные ошибки OCPP 2.0
        switch (errorCode)
        {
            case -32700: // Parse error
                _logger.LogError("JSON parse error from {ChargePointId}", chargePointId);
                break;
                
            case -32600: // Invalid Request
                _logger.LogError("Invalid request from {ChargePointId}", chargePointId);
                break;
                
            case -32601: // Method not found
                _logger.LogError("Method {Action} not found on {ChargePointId}", 
                    pendingCommand.Action, chargePointId);
                break;
                
            case -32602: // Invalid params
                _logger.LogError("Invalid parameters for {Action} on {ChargePointId}", 
                    pendingCommand.Action, chargePointId);
                break;
                
            case -32603: // Internal error
                _logger.LogError("Internal error on {ChargePointId} for command {Action}", 
                    chargePointId, pendingCommand.Action);
                break;
        }
    }
    
    private async Task ProcessOCPP20ResetResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Reset command {Status} for {ChargePointId}", status, chargePointId);
        
        if (status == "Accepted")
        {
            // Можно обновить статус станции в базе данных
            // или выполнить другие действия после успешного reset
        }
    }
    
    private Task ProcessOCPP20UnlockConnectorResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Unlock connector command {Status} for {ChargePointId}", status, chargePointId);
        return Task.CompletedTask;
    }
    
    private Task ProcessOCPP20RequestStartResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Request start transaction {Status} for {ChargePointId}", status, chargePointId);
        return Task.CompletedTask;
    }
    
    private Task ProcessOCPP20RequestStopResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Request stop transaction {Status} for {ChargePointId}", status, chargePointId);
        return Task.CompletedTask;
    }
    
    private Task ProcessOCPP20SetChargingProfileResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Set charging profile {Status} for {ChargePointId}", status, chargePointId);
        return Task.CompletedTask;
    }
    
    private Task ProcessOCPP20ClearChargingProfileResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Clear charging profile {Status} for {ChargePointId}", status, chargePointId);
        return Task.CompletedTask;
    }
    
    private Task ProcessOCPP20ChangeAvailabilityResultAsync(string chargePointId, JsonElement result)
    {
        var status = result.GetProperty("status").GetString();
        _logger.LogInformation("Change availability {Status} for {ChargePointId}", status, chargePointId);
        return Task.CompletedTask;
    }
    
    private async Task ProcessOCPP20GetVariablesResultAsync(string chargePointId, JsonElement result)
    {
        var getVariableResult = result.GetProperty("getVariableResult").EnumerateArray();
        
        foreach (var variableResult in getVariableResult)
        {
            var attributeStatus = variableResult.GetProperty("attributeStatus").GetString();
            var component = variableResult.GetProperty("component");
            var variable = variableResult.GetProperty("variable");
            
            _logger.LogDebug(
                "GetVariables result: Component={Component}, Variable={Variable}, Status={Status}",
                component.GetProperty("name").GetString(),
                variable.GetProperty("name").GetString(),
                attributeStatus);
        }
    }
    
    private async Task ProcessOCPP20SetVariablesResultAsync(string chargePointId, JsonElement result)
    {
        var setVariableResult = result.GetProperty("setVariableResult").EnumerateArray();
        
        foreach (var variableResult in setVariableResult)
        {
            var attributeStatus = variableResult.GetProperty("attributeStatus").GetString();
            var component = variableResult.GetProperty("component");
            var variable = variableResult.GetProperty("variable");
            
            _logger.LogDebug(
                "SetVariables result: Component={Component}, Variable={Variable}, Status={Status}",
                component.GetProperty("name").GetString(),
                variable.GetProperty("name").GetString(),
                attributeStatus);
        }
    }
}

public interface IPendingCommandsStorage
{
    Task AddAsync(PendingCommand command);
    Task<PendingCommand?> GetAsync(string messageId);
    Task RemoveAsync(string messageId);
    Task<IEnumerable<PendingCommand>> GetExpiredCommandsAsync(TimeSpan maxAge);
}

public class PendingCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string ChargePointId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public object Payload { get; set; } = new object();
    public DateTime SentAt { get; set; }
    public CommandCallback? Callback { get; set; }
}

public class CommandCallback
{
    public Func<string, Task>? OnSuccess { get; set; }
    public Func<string, string, Task>? OnError { get; set; }
    public Func<Task>? OnTimeout { get; set; }
}

public class InMemoryPendingCommandsStorage : IPendingCommandsStorage
{
    private readonly ConcurrentDictionary<string, PendingCommand> _commands = new();
    private readonly ILogger<InMemoryPendingCommandsStorage> _logger;

    public InMemoryPendingCommandsStorage(ILogger<InMemoryPendingCommandsStorage> logger)
    {
        _logger = logger;

        // Фоновая задача для очистки просроченных команд
        Task.Run(CleanupExpiredCommands);
    }

    public Task AddAsync(PendingCommand command)
    {
        _commands[command.MessageId] = command;
        _logger.LogDebug("Added pending command: {MessageId} for {ChargePointId}",
            command.MessageId, command.ChargePointId);
        return Task.CompletedTask;
    }

    public Task<PendingCommand?> GetAsync(string messageId)
    {
        _commands.TryGetValue(messageId, out var command);
        return Task.FromResult(command);
    }

    public Task RemoveAsync(string messageId)
    {
        _commands.TryRemove(messageId, out _);
        _logger.LogDebug("Removed pending command: {MessageId}", messageId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<PendingCommand>> GetExpiredCommandsAsync(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var expired = _commands.Values
            .Where(c => c.SentAt < cutoff)
            .ToList();

        return Task.FromResult(expired.AsEnumerable());
    }

    private async Task CleanupExpiredCommands()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5));

            try
            {
                var expired = await GetExpiredCommandsAsync(TimeSpan.FromMinutes(10));
                foreach (var command in expired)
                {
                    await RemoveAsync(command.MessageId);
                    _logger.LogWarning(
                        "Removed expired pending command: {MessageId} for {ChargePointId}",
                        command.MessageId, command.ChargePointId);

                    // Вызываем callback timeout если есть
                    command.Callback?.OnTimeout?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired commands");
            }
        }
    }
}