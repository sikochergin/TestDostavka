using System.Text.Json.Serialization;

public sealed class YooKassaConfirmation
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("return_url")]
    public string? ReturnUrl { get; set; }

    [JsonPropertyName("confirmation_url")]
    public string? ConfirmationUrl { get; set; }
}