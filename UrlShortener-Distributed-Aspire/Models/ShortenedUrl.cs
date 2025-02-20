namespace UrlShortener_Distributed_Aspire.Models
{
    public record ShortenedUrl(string ShortCode, string OriginalUrl, DateTime CreatedAt);
}
