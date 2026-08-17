using System.ComponentModel.DataAnnotations;

public class RejectOfferModel
{
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "Напишите комментарий.")]
    [MaxLength(2000)]
    public string Comment { get; set; } = null!;
}