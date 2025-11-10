using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EdoSign.Lab_3.Models;

public class ApplicationUser : IdentityUser
{
    // Username у Identity вже є і має унікальність
    // Email також з унікальністю (див. RequireUniqueEmail в Program.cs)

    [StringLength(500, ErrorMessage = "ПIБ не більше 500 символів")]
    public string? FullName { get; set; }
}
