namespace KhoaNVCB_API.Dtos // Nhớ đổi namespace thành KhoaNVCB_Client.Models khi copy sang Client nhé
{
    public class RegisterDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
    }
}