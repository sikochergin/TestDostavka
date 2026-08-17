using System.Text.Json.Serialization;

public sealed class YooKassaCreatePaymentRequest
{
    [JsonPropertyName("amount")]
    public YooKassaAmount Amount { get; set; } = null!;

    [JsonPropertyName("capture")]
    public bool Capture { get; set; }

    [JsonPropertyName("confirmation")]
    public YooKassaConfirmation Confirmation { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = [];
}