namespace Application.Features.ChargePoints.Commands.ChangeChargePointStatuss;

public class ChangeChargePointStatusResponse
{
    public string ChargePointId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}