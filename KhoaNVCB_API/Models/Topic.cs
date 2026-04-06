public class Topic
{
    public int TopicId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? FileUrl { get; set; } // Đường dẫn tài liệu chuyên đề
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Status: "Pending" (Chờ duyệt), "Approved" (Đã duyệt), "Rejected" (Từ chối)
    public string Status { get; set; } = "Pending";

    public string? UserId { get; set; } // Người đăng
    public string? AuthorName { get; set; } // Tên tác giả hiển thị
}