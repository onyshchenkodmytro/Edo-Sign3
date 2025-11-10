using System.ComponentModel.DataAnnotations;

namespace EdoSign.Lab_3.Models.ViewModels
{
    public class FileVerifyViewModel
    {
        [Required(ErrorMessage = "Вкажіть ID файлу")]
        public string? FileId { get; set; }

        [Required(ErrorMessage = "Вкажіть публічний ключ у форматі PEM")]
        [Display(Name = "Публічний ключ (PEM)")]
        public string? PublicKeyPem { get; set; }

        public bool? IsValid { get; set; }
    }
}
