using System.ComponentModel.DataAnnotations;

namespace KhoaNVCB_API.Dtos
{
    public class CreatePostDto
    {
        [Required]
        public string Title { get; set; } = null!;
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        [Required]
        public string Content { get; set; } = null!;
        [Required]
        public int CategoryId { get; set; }
        public string? OriginalUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? SourceType { get; set; }
        public string? Status { get; set; }
        public DateTime? PublishedDate { get; set; }
        public int? YearType { get; set; } // Thêm dòng này
        public string? Data { get; set; } // Thêm dòng này
    }
}
