using System.ComponentModel.DataAnnotations;

namespace EdoSign.Lab_3.Models.ViewModels
{
    public class FileSignViewModel
    {
        [Required(ErrorMessage = "Вкажіть ID файлу")]
        public string? FileId { get; set; }

        [Required(ErrorMessage = "Введіть приватний ключ у форматі PEM")]
        [Display(Name = "Приватний ключ (PEM)")]
        public string? PrivateKeyPem { get; set; }

        public string? SignatureBase64 { get; set; }
    }
}
