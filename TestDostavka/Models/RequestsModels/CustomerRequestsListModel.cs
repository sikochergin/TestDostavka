using TestDostavka.Models.Enums;

public class CustomerRequestsListModel
{
    public Guid Id { get; set; }
    public string ProductName { get; set; }
    public string ProductUrl { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime DateTime { get; set; }
    public DateOnly? DeliveryDate { get; set; }
}
