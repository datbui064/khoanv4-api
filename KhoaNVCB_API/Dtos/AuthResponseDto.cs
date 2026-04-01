namespace KhoaNVCB_API.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int AccountId { get; set; } // MỚI THÊM TRƯỜNG NÀY
    }
}
