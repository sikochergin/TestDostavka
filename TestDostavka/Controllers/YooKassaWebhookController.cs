using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TestDostavka.Models.Enums;

namespace TestDostavka.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/yookassa")]
public sealed class YooKassaWebhookController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IYooKassaService _yooKassaService;

    public YooKassaWebhookController(
        AppDbContext dbContext,
        IYooKassaService yooKassaService,
        ILogger<YooKassaWebhookController> logger)
    {
        _dbContext = dbContext;
        _yooKassaService = yooKassaService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromBody] YooKassaWebhookNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification is null || notification.Object is null)
            return BadRequest();

        if (!string.Equals(notification.Type, "notification", StringComparison.OrdinalIgnoreCase))
            return BadRequest();

        var providerPaymentId = notification.Object.Id;

        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return BadRequest();

        try
        {
            var providerPayment = await _yooKassaService.GetPaymentAsync(providerPaymentId, cancellationToken);

            var payment = await _dbContext.Payments.Include(payment => payment.Request).FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId, cancellationToken);

            if (payment is null)
                return NotFound();

            var amountIsValid = ValidateAmount(payment, providerPayment);

            if (!amountIsValid)
                return BadRequest();

            switch (providerPayment.Status)
            {
                case "succeeded":
                    await HandleSucceededPaymentAsync(payment, providerPayment, cancellationToken);
                    break;

                case "canceled":
                    await HandleCanceledPaymentAsync(payment, providerPayment, cancellationToken);
                    break;

                case "waiting_for_capture":
                    await HandleWaitingForCaptureAsync(
                        payment,
                        providerPayment,
                        cancellationToken);
                    break;

                case "pending":
                    await HandlePendingPaymentAsync(payment, providerPayment, cancellationToken);
                    break;

                default:
                    payment.ProviderStatus =
                        providerPayment.Status;

                    payment.ModificationDateTime = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);

                    break;
            }

            /*
             * ЮKassa считает уведомление подтверждённым,
             * если получает HTTP 200.
             */
            return Ok();
        }
        catch
        {
            return StatusCode( StatusCodes.Status500InternalServerError);
        }
    }

    private async Task HandleSucceededPaymentAsync(Payment payment, YooKassaPaymentResponse providerPayment, CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Succeeded)
            return;

        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderStatus = providerPayment.Status;
        payment.PaidDateTime = DateTime.UtcNow;
        payment.ModificationDateTime = DateTime.UtcNow;
        payment.Request.Status = RequestStatus.Paid;

        await _dbContext.RequestComments.AddAsync(new RequestComment
        {
            IsFromCustomer = true,
            RequestId = payment.RequestId,
            Comment = "",
            TechComment = "Оплатил заявку."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleCanceledPaymentAsync(Payment payment, YooKassaPaymentResponse providerPayment, CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Canceled)
            return;

        payment.Status = PaymentStatus.Canceled;
        payment.ProviderStatus = providerPayment.Status;
        payment.ModificationDateTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleWaitingForCaptureAsync(Payment payment, YooKassaPaymentResponse providerPayment, CancellationToken cancellationToken)
    {
        payment.Status = PaymentStatus.WaitingForCapture;
        payment.ProviderStatus = providerPayment.Status;
        payment.ModificationDateTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandlePendingPaymentAsync(Payment payment, YooKassaPaymentResponse providerPayment, CancellationToken cancellationToken)
    {
        payment.Status = PaymentStatus.Pending;
        payment.ProviderStatus = providerPayment.Status;
        payment.ModificationDateTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool ValidateAmount(Payment payment, YooKassaPaymentResponse providerPayment)
    {
        if (!decimal.TryParse(providerPayment.Amount.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var providerAmount))
            return false;

        var amountMatches = providerAmount == payment.Amount;
        var currencyMatches = string.Equals(providerPayment.Amount.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase);

        return amountMatches && currencyMatches;
    }
}