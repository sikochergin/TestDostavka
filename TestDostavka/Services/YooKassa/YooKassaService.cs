using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

public sealed class YooKassaService : IYooKassaService
{
    private readonly HttpClient _httpClient;
    private readonly YooKassaOptions _options;
    private readonly ILogger<YooKassaService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public YooKassaService(
        HttpClient httpClient,
        IOptions<YooKassaOptions> options,
        ILogger<YooKassaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        ValidateOptions(_options);
    }

    public async Task<YooKassaPaymentResponse> CreatePaymentAsync(
        CreateYooKassaPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateCreatePaymentCommand(command);

        var requestModel = new YooKassaCreatePaymentRequest
        {
            Amount = new YooKassaAmount
            {
                Value = command.Amount.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture),

                Currency = command.Currency
            },

            Capture = true,

            Confirmation = new YooKassaConfirmation
            {
                Type = "redirect",
                ReturnUrl = command.ReturnUrl
            },

            Description = command.Description,

            Metadata = new Dictionary<string, string>
            {
                ["request_id"] = command.RequestId.ToString()
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "payments");

        request.Headers.Add(
            "Idempotence-Key",
            command.IdempotencyKey);

        request.Content = JsonContent.Create(
            requestModel,
            options: JsonOptions);

        _logger.LogInformation(
            "Creating YooKassa payment for request {RequestId}, amount {Amount} {Currency}",
            command.RequestId,
            command.Amount,
            command.Currency);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var payment = await ReadResponseAsync<YooKassaPaymentResponse>(
            response,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(payment.Id))
        {
            throw new YooKassaApiException(
                response.StatusCode,
                errorCode: null,
                message: "ЮKassa вернула платёж без идентификатора.");
        }

        if (payment.Confirmation is null ||
            string.IsNullOrWhiteSpace(
                payment.Confirmation.ConfirmationUrl))
        {
            _logger.LogWarning(
                "YooKassa payment {PaymentId} has no confirmation URL. Status: {Status}",
                payment.Id,
                payment.Status);
        }

        return payment;
    }

    public async Task<YooKassaPaymentResponse> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            throw new ArgumentException(
                "Идентификатор платежа не может быть пустым.",
                nameof(paymentId));
        }

        var escapedPaymentId =
            Uri.EscapeDataString(paymentId.Trim());

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"payments/{escapedPaymentId}");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await ReadResponseAsync<YooKassaPaymentResponse>(
            response,
            cancellationToken);
    }

    private async Task<TResponse> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonSerializer.Deserialize<TResponse>(
                    responseBody,
                    JsonOptions);

                if (result is null)
                {
                    throw new YooKassaApiException(
                        response.StatusCode,
                        errorCode: null,
                        message:
                            "ЮKassa вернула пустой или некорректный ответ.",
                        responseBody: responseBody);
                }

                return result;
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to deserialize YooKassa response. Status code: {StatusCode}",
                    response.StatusCode);

                throw new YooKassaApiException(
                    response.StatusCode,
                    errorCode: null,
                    message:
                        "Не удалось прочитать ответ платёжной системы.",
                    responseBody: responseBody);
            }
        }

        YooKassaErrorResponse? error = null;

        try
        {
            error = JsonSerializer.Deserialize<YooKassaErrorResponse>(
                responseBody,
                JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize YooKassa error response. Status code: {StatusCode}",
                response.StatusCode);
        }

        var message =
            error?.Description
            ?? $"ЮKassa вернула HTTP {((int)response.StatusCode)}.";

        _logger.LogError(
            "YooKassa API error. Status: {StatusCode}, Code: {ErrorCode}, Parameter: {Parameter}, Description: {Description}",
            response.StatusCode,
            error?.Code,
            error?.Parameter,
            error?.Description);

        throw new YooKassaApiException(
            response.StatusCode,
            error?.Code,
            message,
            error?.Parameter,
            responseBody);
    }

    private static void ValidateOptions(
        YooKassaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ShopId))
        {
            throw new InvalidOperationException(
                "Не настроен YooKassa:ShopId.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new InvalidOperationException(
                "Не настроен YooKassa:SecretKey.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiUrl))
        {
            throw new InvalidOperationException(
                "Не настроен YooKassa:ApiUrl.");
        }
    }

    private static void ValidateCreatePaymentCommand(
        CreateYooKassaPaymentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command.Amount),
                "Сумма платежа должна быть больше нуля.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            throw new ArgumentException(
                "Валюта платежа не указана.",
                nameof(command.Currency));
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            throw new ArgumentException(
                "Описание платежа не указано.",
                nameof(command.Description));
        }

        if (command.Description.Length > 128)
        {
            throw new ArgumentException(
                "Описание платежа не должно превышать 128 символов.",
                nameof(command.Description));
        }

        if (!Uri.TryCreate(
                command.ReturnUrl,
                UriKind.Absolute,
                out var returnUri) ||
            returnUri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                "ReturnUrl должен быть абсолютным HTTP или HTTPS URL.",
                nameof(command.ReturnUrl));
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            throw new ArgumentException(
                "Ключ идемпотентности не указан.",
                nameof(command.IdempotencyKey));
        }

        if (command.IdempotencyKey.Length > 64)
        {
            throw new ArgumentException(
                "Ключ идемпотентности не должен превышать 64 символа.",
                nameof(command.IdempotencyKey));
        }
    }
}