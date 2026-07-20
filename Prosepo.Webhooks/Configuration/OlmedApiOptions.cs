namespace Prosepo.Webhooks.Configuration;

public class OlmedApiOptions
{
    public const string SectionName = "OlmedAuth";

    public string BaseUrl { get; set; } = "https://csm-connector.grupaolmed.pl";
    public string Username { get; set; } = "prospeo";
    public string Password { get; set; } = "";
}