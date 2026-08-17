using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using TestDostavka.Models.Enums;

[Table("tbl_person")]
public class Person
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("creationdatetime", TypeName = "timestamp with time zone")]
    public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(256)]
    [Column("mail")]
    public string Email { get; set; } = null!;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("role")]
    public PersonRole Role { get; set; } = PersonRole.Customer;
}
