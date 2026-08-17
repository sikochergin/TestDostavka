using System.Text.Json.Serialization;

public sealed class YooKassaAmount
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;
}