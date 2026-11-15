namespace Domain;

public class FinancialExposure
{
    private readonly Dictionary<string, decimal> _exposures = [];
    public const decimal DefaultLimitSymbol = 100_000_000m;
    private decimal _limitSymbol { get; }

    public FinancialExposure(decimal limitSymbol = DefaultLimitSymbol)
    {
        if(limitSymbol <= 0)
        {
            throw new ArgumentException("Limit symbol must be positive.", nameof(limitSymbol));
        }
        _limitSymbol = limitSymbol;
    }

    public Exposure Apply(string symbol, OrderSide side, decimal price, int quantity)
    {
        if(symbol is null || symbol == "")
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }

        decimal orderValue = price * quantity;
        decimal delta = side == OrderSide.Buy ? orderValue : -orderValue;

        //TODO: implementar lock para proteger acesso concorrente ao dicionário _exposures
        decimal before = _exposures.GetValueOrDefault(symbol);
        decimal projected = before + delta;
        var accepted = Math.Abs(projected) <= _limitSymbol;

        if(accepted)
        {
            _exposures[symbol] = projected;
        }
        return new Exposure { 
            Symbol = symbol, 
            OrderValue = orderValue, 
            ExposureBefore = before, 
            ExposureAfter = accepted ? projected : before, 
            Limit = _limitSymbol, 
            Accepted = accepted };
    }

    public decimal GetExposure(string symbol)
    {
        if (symbol is null || symbol == "")
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }
        return _exposures.GetValueOrDefault(symbol);
    }

    public IReadOnlyDictionary<string, decimal> GetAllExposures()
    {
        return new Dictionary<string, decimal>(_exposures);
    }
}