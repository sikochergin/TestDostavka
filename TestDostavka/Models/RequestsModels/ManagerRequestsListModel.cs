using TestDostavka.Models.Enums;
public class ManagerRequestsListModel
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; }
    public string ProductName { get; set; }
    public string ProductUrl { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime DateTime { get; set; }
    public DateOnly? DeliveryDate { get; set; }
}
