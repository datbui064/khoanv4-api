using System.ComponentModel.DataAnnotations;

namespace KhoaNVCB_API.Dtos
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
