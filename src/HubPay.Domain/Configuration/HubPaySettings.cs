namespace HubPay.Domain.Configuration;

public sealed class HubPaySettings
{
    public const string SectionName = "HubPay";

    public string ConnectionString { get; set; } = "Host=localhost;Database=hubpay;Username=postgres;Password=postgres";
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public string OnnxModelPath { get; set; } = "Models/antifraud.onnx";
    public decimal HubProcessingFeePercent { get; set; } = 0.8m;
    public int IdempotencyTtlHours { get; set; } = 24;
    public int ClearingIntervalSeconds { get; set; } = 30;

    public SibsApiSettings Sibs { get; set; } = new();
    public BizumApiSettings Bizum { get; set; } = new();
    public WeroApiSettings Wero { get; set; } = new();
}

public sealed class SibsApiSettings
{
    public string BaseUrl { get; set; } = "https://api.sibs.pt/sandbox";
    public string ApiKey { get; set; } = "fake-sibs-api-key";
}

public sealed class BizumApiSettings
{
    public string BaseUrl { get; set; } = "https://api.bizum.es/sandbox";
    public string ApiKey { get; set; } = "fake-bizum-api-key";
}

public sealed class WeroApiSettings
{
    public string BaseUrl { get; set; } = "https://api.wero.eu/sandbox";
    public string ApiKey { get; set; } = "fake-wero-api-key";
}
