public interface IYooKassaService
{
    Task<YooKassaPaymentResponse> CreatePaymentAsync(
        CreateYooKassaPaymentCommand command,
        CancellationToken cancellationToken = default);

    Task<YooKassaPaymentResponse> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken = default);
}