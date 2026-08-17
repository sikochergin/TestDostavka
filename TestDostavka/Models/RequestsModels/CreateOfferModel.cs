using System.ComponentModel.DataAnnotations;

public class CreateOfferModel
{
    public Guid RequestId { get; set; }

    public decimal Price { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }
}