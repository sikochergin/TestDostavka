using System.ComponentModel.DataAnnotations;

public class CreateRequestModel
{
    [Required(ErrorMessage = "Введите название товара.")]
    [MaxLength(300)]
    [Display(Name = "Название товара")]
    public string ProductName { get; set; } = null!;

    [Required(ErrorMessage = "Добавьте ссылку на товар.")]
    [MaxLength(2000)]
    [Display(Name = "Ссылка на товар")]
    public string ProductUrl { get; set; } = null!;

    [MaxLength(3000)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Display(Name = "Количество")]
    public int Quantity { get; set; }
}
