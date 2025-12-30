namespace Application.Features.ChargePoints.Commands.UpdateChargePoints;

public class UpdateChargePointResponse
{
    public string ChargePointId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}