using System.ComponentModel.DataAnnotations;

public class LoginPersonRequest
{
    [Required(ErrorMessage = "Введите email.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введите пароль.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; }
}
