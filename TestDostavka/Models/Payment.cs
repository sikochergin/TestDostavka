using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Payment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("creationdatetime", TypeName = "timestamp with time zone")]
    public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;

    [Column("modificationdatetime", TypeName = "timestamp with time zone")]
    public DateTime ModificationDateTime { get; set; } = DateTime.UtcNow;

    [Column("request_id")]
    public Guid RequestId { get; set; }
    public Request Request { get; set; } = null!;

    [Column("provider_payment_id")]
    public string? ProviderPaymentId { get; set; }

    [Column("idempotency_key")]
    [Required]
    public string IdempotencyKey { get; set; } = null!;

    [Column("amount", TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [Column("currency")]
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "RUB";

    [Column("status")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    [Column("confirmation_url")]
    [MaxLength(2000)]
    public string? ConfirmationUrl { get; set; }

    [Column("provider_status")]
    public string? ProviderStatus { get; set; }

    [Column("error_code")]
    public string? ErrorCode { get; set; }

    [Column("error_description")]
    public string? ErrorDescription { get; set; }

    [Column("paiddatetime", TypeName = "timestamp with time zone")]
    public DateTime? PaidDateTime { get; set; }


    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}