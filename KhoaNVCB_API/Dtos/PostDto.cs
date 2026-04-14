using System;

namespace KhoaNVCB_API.Dtos
{
    public class PostDto
    {
        // Khớp hoàn toàn với Database
        public int PostId { get; set; }

        public string Title { get; set; } = null!;

        public string? Slug { get; set; }

        public string? Summary { get; set; }

        public string Content { get; set; } = null!;

        public int? CategoryId { get; set; }

        public int? AuthorId { get; set; }

        // Dùng để phân loại: "Video", "Article", "Document"...
        public string? SourceType { get; set; }

        // Nơi chứa Link YouTube hoặc Link Ảnh như chúng ta đã thống nhất
        public string? OriginalUrl { get; set; }
        public int? YearType { get; set; }

        public string? Status { get; set; } = "Published";

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public DateTime? PublishedDate { get; set; }

        // Bổ sung thêm tên Category để hiển thị trên danh sách bài viết cho tiện
        public string? CategoryName { get; set; }

        // Bổ sung tên Tác giả (Giảng viên) để hiển thị
        public string? AuthorName { get; set; }
        public string? ImageUrl { get; set; }
    }
}