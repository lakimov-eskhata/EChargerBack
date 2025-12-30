using Application.Common.Interfaces;

namespace Application.Features.ChargePoints.Commands.DeleteChargePoints;

public class DeleteChargePointCommand : IRequest<DeleteChargePointResponse>
{
    public string ChargePointId { get; set; } = string.Empty;
    public bool ForceDelete { get; set; } = false;
}