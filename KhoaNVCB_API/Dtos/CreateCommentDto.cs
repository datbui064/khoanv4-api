using System.ComponentModel.DataAnnotations;

namespace KhoaNVCB_API.Dtos
{
    public class CreateCommentDto
    {
        [Required]
        public int PostId { get; set; }
        [Required]
        public int AccountId { get; set; }
        [Required]
        public string Content { get; set; } = null!;
    }
}
