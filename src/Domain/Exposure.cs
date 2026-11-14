namespace Domain;

public record Exposure
{
    public bool Accepted { get; init; }
    public required string Symbol { get; init; }
    public decimal OrderValue { get; init; }
    public decimal ExposureBefore { get; init; }
    public decimal ExposureAfter { get; init; }
    public decimal Limit { get; init; }
    public decimal ProjectedExposure => ExposureBefore + OrderValue;
}
