using System.Text.Json.Serialization;

public sealed class YooKassaWebhookNotification
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("event")]
    public string Event { get; set; } = null!;

    [JsonPropertyName("object")]
    public YooKassaPaymentResponse Object { get; set; } = null!;
}