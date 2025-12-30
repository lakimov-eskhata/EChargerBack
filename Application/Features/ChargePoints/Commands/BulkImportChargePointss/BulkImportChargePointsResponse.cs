namespace Application.Features.ChargePoints.Commands.BulkImportChargePointss;

public class BulkImportChargePointsResponse
{
    public int TotalProcessed { get; set; }
    public int SuccessfullyImported { get; set; }
    public int Failed { get; set; }
    public List<ImportResult> Results { get; set; } = new();
        
    public class ImportResult
    {
        public string ChargePointId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}