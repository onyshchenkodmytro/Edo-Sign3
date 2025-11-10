using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EdoSign.Lab_3.Models.ViewModels
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "Оберіть файл для завантаження")]
        public IFormFile? File { get; set; }

        public string? FileId { get; set; }
    }
}
