using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TestDostavka.Models.Enums;

[Table("tbl_request")]
public class Request
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("creationdatetime", TypeName = "timestamp with time zone")]
    public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;

    [Column("person_id")]
    public Guid PersonId { get; set; }
    public Person Person { get; set; }

    [Column("status")]
    public RequestStatus Status { get; set; }

    [Column("product_name")]
    public string ProductName { get; set; }

    [Column("product_url")]
    public string? ProductUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [Column("delivery_date")]
    public DateOnly? DeliveryDate { get; set; }
}