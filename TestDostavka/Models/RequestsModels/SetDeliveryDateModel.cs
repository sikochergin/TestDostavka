using System.ComponentModel.DataAnnotations;

public class SetDeliveryDateModel
{
    public Guid RequestId { get; set; }

    public DateOnly DeliveryDate { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }
}
