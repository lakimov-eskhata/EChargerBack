using Application.Common.Interfaces;
using Application.Features.ChargePoints.Commands.CreateChargePoints;

namespace Application.Features.ChargePoints.Commands.BulkImportChargePointss;

public class BulkImportChargePointsCommand : IRequest<BulkImportChargePointsResponse>
{
    public List<CreateChargePointCommand> ChargePoints { get; set; } = new();
    public bool OverrideExisting { get; set; } = false;
}