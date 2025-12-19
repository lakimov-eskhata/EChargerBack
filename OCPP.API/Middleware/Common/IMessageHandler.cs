namespace OCPP.API.Middleware.OCPP16;

public interface IMessageHandler
{
    Task<object> HandleAsync(string chargePointId, object message);
}