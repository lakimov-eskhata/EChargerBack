namespace Domain.Entities.Station;

public class MeterValueEntity
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string? Context { get; set; } // Sample.Periodic, Sample.Clock, Transaction.Begin, Transaction.End, etc.
    public string? Format { get; set; } // Raw, SignedData
    public string? Measurand { get; set; } // Energy.Active.Import.Register, Power.Active.Import, etc.
    public string? Phase { get; set; } // L1, L2, L3, N, L1-N
    public string? Location { get; set; } // Inlet, Outlet, Body, Cable, EV
    public string? Unit { get; set; } // Wh, kWh, varh, kvarh, W, kW, var, kvar, A, V, Celsius, Fahrenheit, K, Percent
        
    public int TransactionId { get; set; }
    public virtual TransactionEntity Transaction { get; set; } = null!;
}