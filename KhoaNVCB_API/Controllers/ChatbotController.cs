using Microsoft.AspNetCore.Mvc;
using KhoaNVCB_API.Models;
using KhoaNVCB_API.Services;
using Microsoft.EntityFrameworkCore;
namespace KhoaNVCB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly KhoaNvcbBlogDbContext _context;

        // Bơm cả 2: Não AI (GeminiService) và Database (KhoaNvcbBlogDbContext)
        public ChatbotController(GeminiService geminiService, KhoaNvcbBlogDbContext context)
        {
            _geminiService = geminiService;
            _context = context;
        }

        // ===============================================
        // 1. API NHẬN TIN NHẮN CHAT -> TRẢ VỀ CÂU CỦA AI
        // ===============================================
        [HttpPost("ask")]
        public async Task<IActionResult> AskBot([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Câu hỏi không được để trống.");

            // Gọi não AI xử lý
            var answer = await _geminiService.ChatAsync(request.Message, request.Context);

            return Ok(new { Answer = answer });
        }
        // ===============================================
        // 3. API CHO ADMIN: LẤY DANH SÁCH VÀ ĐÁNH DẤU XONG
        // ===============================================
        [HttpGet("tickets")]
        public async Task<IActionResult> GetAllTickets()
        {
            // Lấy danh sách phiếu hỗ trợ, xếp cái mới nhất lên đầu
            var tickets = await _context.SupportTickets.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return Ok(tickets);
        }

        [HttpPut("ticket/{id}/resolve")]
        public async Task<IActionResult> ResolveTicket(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound("Không tìm thấy phiếu hỗ trợ.");

            ticket.Status = "Resolved"; // Đổi trạng thái thành Đã xử lý
            await _context.SaveChangesAsync();

            return Ok(new { Success = true });
        }
        // ===============================================
        // 2. API LƯU FORM TICKET -> ĐẨY VÀO DATABASE
        // ===============================================
        [HttpPost("ticket")]
        public async Task<IActionResult> SubmitTicket([FromBody] SupportTicket ticket)
        {
            if (!ModelState.IsValid)
                return BadRequest("Dữ liệu không hợp lệ.");

            // Gán ngày giờ hiện tại và trạng thái mặc định
            ticket.CreatedAt = DateTime.Now;
            ticket.Status = "Pending"; // Chờ xử lý

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { Success = true });
        }
    }

    // Class hứng dữ liệu chat từ Frontend gửi lên
    public class ChatRequest
    {
        public string Message { get; set; } = "";

        // Đoạn Tóm tắt bài viết (Nếu khách đang ở trang đọc bài)
        public string Context { get; set; } = "";
    }
}