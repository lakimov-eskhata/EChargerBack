namespace Application.Features.ChargePoints.Commands.CreateChargePoints;

public class CreateChargePointResponse
{
    public int Id { get; set; }
    public string ChargePointId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProtocolVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}