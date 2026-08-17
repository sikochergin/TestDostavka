using System.Text.Json.Serialization;

public sealed class YooKassaPaymentResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("amount")]
    public YooKassaAmount Amount { get; set; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("confirmation")]
    public YooKassaConfirmation? Confirmation { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("paid")]
    public bool Paid { get; set; }

    [JsonPropertyName("refundable")]
    public bool Refundable { get; set; }

    [JsonPropertyName("test")]
    public bool Test { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}