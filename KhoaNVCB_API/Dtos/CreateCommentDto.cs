
using System.ComponentModel.DataAnnotations;

namespace KhoaNVCB_API.Dtos
{
    public class CreateCommentDto
    {
        [Required]
        public int PostId { get; set; }

        // BỎ DÒNG NÀY: public int AccountId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = null!;

        public string? Website { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung bình luận")]
        public string Content { get; set; } = null!;
    }
}
