using System.Collections.Concurrent;
using Application.Common.Models;
using SimpleR;
using SimpleR.Ocpp;

namespace Application.Common;

public class ChargePointConnectionStorage
{
    private readonly ConcurrentDictionary<string, ChargePointStatus> _sessions = new();

    public void Register(string chargePointId, ChargePointStatus chargePointStatus)
        => _sessions[chargePointId] = chargePointStatus;

    public void Remove(string chargePointId)
        => _sessions.TryRemove(chargePointId, out _);

    public bool TryGet(string chargePointId, out ChargePointStatus? chargePoint)
        => _sessions.TryGetValue(chargePointId, out chargePoint);
    
    
}