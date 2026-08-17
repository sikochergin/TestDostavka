using System.ComponentModel.DataAnnotations;

public class CreatePersonRequest
{
    [Required(ErrorMessage = "Введите email.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введите пароль.")]
    [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Повторите пароль.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "Пароли не совпадают.")]
    public string ConfirmPassword { get; set; } = null!;
}
