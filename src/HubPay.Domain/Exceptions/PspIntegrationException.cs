namespace HubPay.Domain.Exceptions;

public sealed class PspIntegrationException : DomainException
{
    public string Scheme { get; }
    public int? HttpStatusCode { get; }
    public string? ResponseBody { get; }

    public PspIntegrationException(string scheme, string message, int? httpStatusCode = null, string? responseBody = null)
        : base(message)
    {
        Scheme = scheme;
        HttpStatusCode = httpStatusCode;
        ResponseBody = responseBody;
    }

    public PspIntegrationException(string scheme, string message, Exception innerException)
        : base(message, innerException)
    {
        Scheme = scheme;
    }
}
