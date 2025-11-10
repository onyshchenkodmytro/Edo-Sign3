using System.ComponentModel.DataAnnotations;

namespace EdoSign.Lab_3.Models.ViewModels;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Логін або Email")]
    public string UserNameOrEmail { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = default!;

    [Display(Name = "Запам'ятати мене")]
    public bool RememberMe { get; set; }
}
