namespace Application.Features.ChargePoints.Commands.DeleteChargePoints;

public class DeleteChargePointResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}