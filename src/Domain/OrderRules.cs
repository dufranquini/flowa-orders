namespace Domain;


/// <summary>
/// Business rules for orders.
/// </summary>
public static class OrderRules
{
    public static readonly IReadOnlyList<string> AllowedOrderSymbols =
    [
        "PETR4",
        "VALE3",
        "VIIA4"
    ];

    /// <summary>
    /// Should be integer, positive, and less than this value
    /// </summary>
    public const int MaxQuantity = 100;

    /// <summary>
    /// Should be positive, and less than this value
    /// </summary>
    public const decimal MaxPrice = 1000m;
    
    /// <summary>
    /// Should be multiple of this decimal placee
    /// </summary>
    public const decimal PriceDecimalPlaces = 0.01m;
}
