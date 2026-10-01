namespace HuitJournal.Api.Configuration;

public sealed class OrcidSettings
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string PublicWebBaseUrl { get; set; } = "";
    public bool UseSandbox { get; set; }
}
