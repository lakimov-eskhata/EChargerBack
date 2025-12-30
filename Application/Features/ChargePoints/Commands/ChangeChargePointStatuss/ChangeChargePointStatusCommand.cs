using Application.Common.Interfaces;

namespace Application.Features.ChargePoints.Commands.ChangeChargePointStatuss;

public class ChangeChargePointStatusCommand : IRequest<ChangeChargePointStatusResponse>
{
    public string ChargePointId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}