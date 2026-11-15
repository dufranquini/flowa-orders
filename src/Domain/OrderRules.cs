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
    public const int MaxQuantity = 100_000;

    /// <summary>
    /// Should be positive, and less than this value
    /// </summary>
    public const decimal MaxPrice = 1_000m;
    
    /// <summary>
    /// Should be multiple of this decimal place
    /// </summary>
    public const decimal PriceDecimalPlaces = 0.01m;

    //TODO: criar métodos de validação de regras de negócio
}
