namespace Minify.Communication.Responses.Url;

public enum RetrieveOriginalUrlStatus
{
    Found,
    NotFound,
    Expired,
}

public class ResponseRetrieveOriginalUrlJson
{
    public string LongUrl { get; set; } = string.Empty;
    public RetrieveOriginalUrlStatus Status { get; set; }
}
