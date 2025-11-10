using System.ComponentModel.DataAnnotations;

namespace EdoSign.Lab_3.Models.Entities
{
    public class FileEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string? SignatureBase64 { get; set; }

        public string? PublicKeyPem { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
