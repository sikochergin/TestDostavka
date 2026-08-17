using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("tbl_request_comment")]
public class RequestComment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("creationdatetime", TypeName = "timestamp with time zone")]
    public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;

    [Column("request_id")]
    public Guid RequestId { get; set; }
    public Request Request { get; set; }

    [Column("is_from_customer")]
    public bool IsFromCustomer { get; set; }

    [Column("сomment")]
    public string Comment { get; set; }

    [Column("tech_comment")]
    public string TechComment { get; set; }
}
