using System.ComponentModel.DataAnnotations;
using TestDostavka.Models.Enums;

public class ChangeRequestStatusModel
{
    public Guid RequestId { get; set; }

    public RequestStatus NewStatus { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }
}
