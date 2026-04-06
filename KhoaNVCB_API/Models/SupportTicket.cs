using System.ComponentModel.DataAnnotations;

namespace KhoaNVCB_API.Models
{
    public class SupportTicket
    {
        [Key]
        public int TicketId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContactInfo { get; set; } = string.Empty;

        [Required]
        public string Question { get; set; } = string.Empty;

        // Trạng thái: "Pending" (Chờ xử lý) hoặc "Resolved" (Đã giải quyết)
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}