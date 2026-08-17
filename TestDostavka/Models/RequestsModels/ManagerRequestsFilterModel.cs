using TestDostavka.Models.Enums;

public class ManagerRequestsFilterModel
{
    public List<RequestStatus> Statuses { get; set; } = [];

    public string? Email { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}