using System.Threading.Tasks;

namespace OCPP.API.Core.Abstractions;

public interface IMessageHandler
{
    Task<object> HandleAsync(string chargePointId, object message);
}