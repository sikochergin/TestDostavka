public sealed class CreateYooKassaPaymentCommand
{
    public decimal Amount { get; init; }

    public string Currency { get; init; } = "RUB";

    public string Description { get; init; } = null!;

    public string ReturnUrl { get; init; } = null!;

    public Guid RequestId { get; init; }

    public string IdempotencyKey { get; init; } = null!;
}