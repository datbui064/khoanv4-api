namespace KhoaNVCB_API.Dtos
{
    public class PostListItemDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public string? Summary { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}