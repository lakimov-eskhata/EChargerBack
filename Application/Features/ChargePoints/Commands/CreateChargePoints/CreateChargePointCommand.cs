using Application.Common.Interfaces;

namespace Application.Features.ChargePoints.Commands.CreateChargePoints;

// Create Charge Point Command
public class CreateChargePointCommand : IRequest<CreateChargePointResponse>
{
    public string ChargePointId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProtocolVersion { get; set; } = "1.6";
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public int? ConnectorCount { get; set; }
    public int? HeartbeatInterval { get; set; }
    public string? Iccid { get; set; }
    public string? Imsi { get; set; }
    public string? MeterType { get; set; }
    public string? MeterSerialNumber { get; set; }
    public int? CompanyId { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}