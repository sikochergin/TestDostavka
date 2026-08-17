using TestDostavka.Models.Enums;

public class RequestCardModel
{
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }

    public string ProductName { get; set; } = null!;

    public string ProductUrl { get; set; }

    public string? Description { get; set; }

    public string CustomerEmail { get; set; } = null!;

    public RequestStatus Status { get; set; }

    public decimal? OfferedPrice { get; set; }

    public DateTime CreationDateTime { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public bool IsCustomer { get; set; }

    public bool IsManager { get; set; }

    public List<RequestComment> Comments { get; set; }
}