namespace KhoaNVCB_API.Dtos
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Slug { get; set; }
        public int? ParentId { get; set; }
        public string? ImageUrl { get; set; }
    }
}